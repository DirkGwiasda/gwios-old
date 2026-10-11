namespace GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts.Models;

/// <summary>
/// A combination of description and category used in earlier bookings, offered while the user types the
/// description of a new booking. It carries no amount, since amounts change from booking to booking.
/// </summary>
public class BookingSuggestion
{
    /// <summary>
    /// The description, without leading or trailing white space.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The ID of the category the description was booked under.
    /// </summary>
    public required Guid CategoryId { get; init; }

    /// <summary>
    /// How many bookings use this combination of description and category; at least 1.
    /// </summary>
    public required int UsageCount { get; init; }

    /// <summary>
    /// The date of the latest booking with this combination.
    /// </summary>
    public required DateOnly LastUsedOn { get; init; }
}
