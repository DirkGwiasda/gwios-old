using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;

namespace GwiOS.WebUI.BookingFilters.Contracts;

/// <summary>
/// Filters a list of bookings by a text the user types above it.
/// </summary>
public interface IBookingFilter
{
    /// <summary>
    /// Returns the bookings that match the given text, in their original order. A text that is a number (digits with
    /// at most one comma or dot, e.g. <c>47</c> or <c>47,8</c>) matches bookings whose amount, written like
    /// <c>47,83</c>, starts with it. Any other text matches bookings whose category path (e.g.
    /// <c>Haushalt › Lebensmittel</c>) or description contains it, ignoring case; a booking generated from a
    /// recurring item also matches by the name of that item, which the list shows as its description. An empty or
    /// blank text returns all bookings.
    /// </summary>
    /// <param name="bookings">The bookings to filter.</param>
    /// <param name="categories">All categories, to resolve the category paths.</param>
    /// <param name="recurringItems">All recurring items, to resolve their names.</param>
    /// <param name="text">The text the user typed.</param>
    List<Booking> Filter(
        IReadOnlyList<Booking> bookings,
        IReadOnlyList<Category> categories,
        IReadOnlyList<RecurringItem> recurringItems,
        string text);
}
