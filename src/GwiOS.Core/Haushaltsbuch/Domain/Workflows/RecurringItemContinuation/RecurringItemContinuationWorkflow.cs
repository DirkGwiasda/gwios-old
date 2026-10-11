using GwiOS.Core.CrossCutting.Logging.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.RecurringItemContinuation.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.RecurringItemContinuation.Contracts.Models;

namespace GwiOS.Core.Haushaltsbuch.Domain.Workflows.RecurringItemContinuation;

internal sealed class RecurringItemContinuationWorkflow(
    IRecurringItemManager recurringItemManager,
    IBookingManager bookingManager,
    TimeProvider timeProvider,
    ILogger<RecurringItemContinuationWorkflow> logger) : IRecurringItemContinuationWorkflow
{
    // Twelve months including the current one; older bookings stay as they were booked.
    private const int AdjustableMonthsBeforeCurrent = 11;

    private readonly IRecurringItemManager _recurringItemManager = recurringItemManager;
    private readonly IBookingManager _bookingManager = bookingManager;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<RecurringItemContinuationWorkflow> _logger = logger;

    public async Task<List<BookingRescheduling>> GetBookingReschedulingsAsync(
        Guid predecessorId,
        DateOnly effectiveDate,
        RecurringItem successor,
        CancellationToken cancellationToken = default)
    {
        List<Booking> bookings = await GetAdjustableBookingsAsync(predecessorId, effectiveDate, cancellationToken);
        return PlanReschedulings(bookings, successor, effectiveDate);
    }

    public async Task<RecurringItemContinuationResult> ContinueAsync(
        Guid predecessorId,
        DateOnly effectiveDate,
        RecurringItem successor,
        BookingDateHandling bookingDateHandling,
        CancellationToken cancellationToken = default)
    {
        List<Booking> bookings = await GetAdjustableBookingsAsync(predecessorId, effectiveDate, cancellationToken);
        Dictionary<Guid, BookingRescheduling> reschedulingsByBookingId = bookingDateHandling == BookingDateHandling.FollowSchedule
            ? PlanReschedulings(bookings, successor, effectiveDate).ToDictionary(rescheduling => rescheduling.Booking.Id)
            : [];

        RecurringItem storedSuccessor = await _recurringItemManager.ContinueRecurringItemAsync(
            predecessorId,
            effectiveDate,
            successor,
            cancellationToken);
        RecurringItemContinuationResult result =
            await AdjustBookingsAsync(bookings, storedSuccessor, reschedulingsByBookingId, cancellationToken);

        _logger.LogInformation(
            "Bookings adjusted to continued recurring item.",
            new Dictionary<string, string>
            {
                ["RecurringItemId"] = storedSuccessor.Id.ToString(),
                ["PredecessorId"] = predecessorId.ToString(),
                ["AdjustedBookingCount"] = result.AdjustedBookingCount.ToString(),
                ["MovedBookingCount"] = result.MovedBookingCount.ToString(),
                ["DeletedBookingCount"] = result.DeletedBookingCount.ToString()
            });
        return result;
    }

    private async Task<RecurringItemContinuationResult> AdjustBookingsAsync(
        List<Booking> bookings,
        RecurringItem successor,
        Dictionary<Guid, BookingRescheduling> reschedulingsByBookingId,
        CancellationToken cancellationToken)
    {
        int adjustedCount = 0;
        int movedCount = 0;
        int deletedCount = 0;
        foreach (Booking booking in bookings)
        {
            reschedulingsByBookingId.TryGetValue(booking.Id, out BookingRescheduling? rescheduling);
            if ((rescheduling is not null) && (rescheduling.NewDate is null))
            {
                await _bookingManager.DeleteBookingAsync(booking.Id, cancellationToken);
                deletedCount++;
                continue;
            }

            DateOnly date = rescheduling?.NewDate ?? booking.Date;
            await _bookingManager.UpdateBookingAsync(CreateAdjustedBooking(booking, successor, date), cancellationToken);
            adjustedCount++;
            movedCount += rescheduling is null ? 0 : 1;
        }

        return new RecurringItemContinuationResult
        {
            Successor = successor,
            AdjustedBookingCount = adjustedCount,
            MovedBookingCount = movedCount,
            DeletedBookingCount = deletedCount
        };
    }

    private async Task<List<Booking>> GetAdjustableBookingsAsync(
        Guid predecessorId,
        DateOnly effectiveDate,
        CancellationToken cancellationToken)
    {
        DateOnly earliestDate = GetEarliestAdjustableDate();
        DateOnly fromDate = effectiveDate > earliestDate ? effectiveDate : earliestDate;
        List<Booking> bookings = await _bookingManager.GetBookingsInRangeAsync(fromDate, DateOnly.MaxValue, cancellationToken);
        return [.. bookings.Where(booking => booking.RecurringItemId == predecessorId)];
    }

    private DateOnly GetEarliestAdjustableDate()
    {
        DateOnly today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        return new DateOnly(today.Year, today.Month, 1).AddMonths(-AdjustableMonthsBeforeCurrent);
    }

    private List<BookingRescheduling> PlanReschedulings(
        List<Booking> bookings,
        RecurringItem successor,
        DateOnly effectiveDate)
    {
        RecurringItem successorInEffect = CopyInEffectFrom(successor, effectiveDate);
        return [.. bookings
            .GroupBy(booking => new DateOnly(booking.Date.Year, booking.Date.Month, 1))
            .SelectMany(month => PlanMonthReschedulings([.. month], _recurringItemManager.GetDueDates(successorInEffect, month.Key)))
            .OrderBy(rescheduling => rescheduling.Booking.Date)];
    }

    // A booking already on a due date keeps it; the others take the remaining due dates in date order, and bookings
    // left over have no due date to go to.
    private static List<BookingRescheduling> PlanMonthReschedulings(
        List<Booking> monthBookings,
        IReadOnlyList<DateOnly> dueDates)
    {
        List<DateOnly> freeDueDates = [.. dueDates];
        List<Booking> misplacedBookings = [];
        foreach (Booking booking in monthBookings.OrderBy(booking => booking.Date))
        {
            if (!freeDueDates.Remove(booking.Date))
            {
                misplacedBookings.Add(booking);
            }
        }

        return [.. misplacedBookings.Select((booking, index) => new BookingRescheduling
        {
            Booking = booking,
            NewDate = index < freeDueDates.Count ? freeDueDates[index] : null
        })];
    }

    private static RecurringItem CopyInEffectFrom(RecurringItem recurringItem, DateOnly effectiveDate)
        => new()
        {
            Id = recurringItem.Id,
            Name = recurringItem.Name,
            Type = recurringItem.Type,
            CategoryId = recurringItem.CategoryId,
            Amount = recurringItem.Amount,
            Interval = recurringItem.Interval,
            DueDay = recurringItem.DueDay,
            StartMonth = recurringItem.StartMonth,
            Active = recurringItem.Active,
            ValidFrom = effectiveDate
        };

    private static Booking CreateAdjustedBooking(Booking booking, RecurringItem successor, DateOnly date)
        => new()
        {
            Id = booking.Id,
            Date = date,
            Type = successor.Type,
            CategoryId = successor.CategoryId,
            Description = booking.Description,
            Amount = successor.Amount,
            Status = booking.Status,
            RecurringItemId = successor.Id
        };
}
