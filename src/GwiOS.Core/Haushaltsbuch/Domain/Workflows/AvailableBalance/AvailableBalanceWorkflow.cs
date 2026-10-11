using GwiOS.Core.CrossCutting.Logging.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.AvailableBalance.Contracts;

namespace GwiOS.Core.Haushaltsbuch.Domain.Workflows.AvailableBalance;

internal sealed class AvailableBalanceWorkflow(
    IRecurringItemManager recurringItemManager,
    IBookingManager bookingManager,
    ILogger<AvailableBalanceWorkflow> logger) : IAvailableBalanceWorkflow
{
    private readonly IRecurringItemManager _recurringItemManager = recurringItemManager;
    private readonly IBookingManager _bookingManager = bookingManager;
    private readonly ILogger<AvailableBalanceWorkflow> _logger = logger;

    public async Task<decimal> GetAvailableBalanceAsync(DateOnly month, CancellationToken cancellationToken = default)
    {
        DateOnly firstDay = new(month.Year, month.Month, 1);
        DateOnly lastDay = new(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
        List<RecurringItem> recurringItems = await _recurringItemManager.GetAllRecurringItemsAsync(cancellationToken);
        List<Booking> bookings = await _bookingManager.GetBookingsInRangeAsync(firstDay, lastDay, cancellationToken);

        decimal balance = SumRecurringItems(recurringItems, firstDay, CategoryType.Income)
            + SumOtherBookedBookings(bookings, CategoryType.Income)
            - SumRecurringItems(recurringItems, firstDay, CategoryType.Expense)
            - SumOtherBookedBookings(bookings, CategoryType.Expense);

        _logger.LogDebug(
            "Available balance calculated.",
            new Dictionary<string, string> { ["Month"] = firstDay.ToString("O") });
        return balance;
    }

    private decimal SumRecurringItems(List<RecurringItem> recurringItems, DateOnly month, CategoryType type)
        => recurringItems
            .Where(item => item.Active && (item.Type == type))
            .Sum(item => item.Amount * _recurringItemManager.GetDueDates(item, month).Count);

    // Bookings generated from a recurring item are left out: the recurring item itself is already counted.
    private static decimal SumOtherBookedBookings(List<Booking> bookings, CategoryType type)
        => bookings
            .Where(booking => (booking.Type == type)
                && (booking.Status == BookingStatus.Booked)
                && (booking.RecurringItemId is null))
            .Sum(booking => booking.Amount);
}
