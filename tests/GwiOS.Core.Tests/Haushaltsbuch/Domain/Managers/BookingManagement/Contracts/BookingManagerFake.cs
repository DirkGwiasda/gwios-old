using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts.Models;

namespace GwiOS.Core.Tests.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts;

/// <summary>
/// Stands in for <see cref="IBookingManager"/> for the workflow tests. It keeps bookings in memory and does not
/// validate.
/// </summary>
public sealed class BookingManagerFake : IBookingManager
{
    private readonly List<Booking> _bookings = [];

    /// <summary>
    /// All bookings created so far, in call order.
    /// </summary>
    public List<Booking> CreatedBookings { get; } = [];

    /// <summary>
    /// The IDs of all bookings deleted so far, in call order.
    /// </summary>
    public List<Guid> DeletedBookingIds { get; } = [];

    /// <summary>
    /// Stores a booking directly, as if it had been created before the test.
    /// </summary>
    public void Add(Booking booking)
        => _bookings.Add(booking);

    public Task<Booking> CreateBookingAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        _bookings.Add(booking);
        CreatedBookings.Add(booking);
        return Task.FromResult(booking);
    }

    public Task UpdateBookingAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        int index = _bookings.FindIndex(stored => stored.Id == booking.Id);
        _bookings[index] = booking;
        return Task.CompletedTask;
    }

    public Task<List<Booking>> GetBookingsInRangeAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_bookings
            .Where(booking => (booking.Date >= fromDate) && (booking.Date <= toDate))
            .OrderByDescending(booking => booking.Date)
            .ThenBy(booking => booking.Id)
            .ToList());

    public Task<List<BookingSuggestion>> GetBookingSuggestionsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_bookings
            .Where(booking => !string.IsNullOrWhiteSpace(booking.Description))
            .GroupBy(booking => (Description: booking.Description!.Trim(), booking.CategoryId))
            .Select(group => new BookingSuggestion
            {
                Description = group.Key.Description,
                CategoryId = group.Key.CategoryId,
                UsageCount = group.Count(),
                LastUsedOn = group.Max(booking => booking.Date)
            })
            .OrderByDescending(suggestion => suggestion.UsageCount)
            .ThenByDescending(suggestion => suggestion.LastUsedOn)
            .ThenBy(suggestion => suggestion.Description, StringComparer.Ordinal)
            .ToList());

    public Task DeleteBookingAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        DeletedBookingIds.Add(bookingId);
        _bookings.RemoveAll(booking => booking.Id == bookingId);
        return Task.CompletedTask;
    }
}
