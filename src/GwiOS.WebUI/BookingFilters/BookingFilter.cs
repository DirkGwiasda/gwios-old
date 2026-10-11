using System.Globalization;
using System.Text.RegularExpressions;
using GwiOS.Core.CrossCutting.Logging.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;
using GwiOS.WebUI.BookingFilters.Contracts;

namespace GwiOS.WebUI.BookingFilters;

internal sealed partial class BookingFilter(ILogger<BookingFilter> logger) : IBookingFilter
{
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    // Categories nest at most 5 levels deep; the limit only protects against a cycle in inconsistent data.
    private const int MaxDepth = 8;

    private readonly ILogger<BookingFilter> _logger = logger;

    public List<Booking> Filter(
        IReadOnlyList<Booking> bookings,
        IReadOnlyList<Category> categories,
        IReadOnlyList<RecurringItem> recurringItems,
        string text)
    {
        string query = text.Trim();
        if (query.Length == 0)
        {
            return [.. bookings];
        }

        bool isAmountQuery = NumberRegex().IsMatch(query);
        Func<Booking, bool> matches = isAmountQuery
            ? CreateAmountMatcher(query)
            : CreateTextMatcher(query, categories, recurringItems);
        List<Booking> result = [.. bookings.Where(matches)];
        // The text itself is not logged; it may contain names or amounts.
        _logger.LogDebug(
            "Bookings filtered.",
            new Dictionary<string, string>
            {
                ["FilterKind"] = isAmountQuery ? "Amount" : "Text",
                ["BookingCount"] = bookings.Count.ToString(CultureInfo.InvariantCulture),
                ["MatchCount"] = result.Count.ToString(CultureInfo.InvariantCulture)
            });
        return result;
    }

    // "47" finds 47,83 and 470,00; a dot counts as comma, so "47.8" finds 47,83 as well.
    private static Func<Booking, bool> CreateAmountMatcher(string query)
    {
        string prefix = query.Replace('.', ',');
        return booking => booking.Amount.ToString("0.00", GermanCulture).StartsWith(prefix, StringComparison.Ordinal);
    }

    private static Func<Booking, bool> CreateTextMatcher(
        string query, IReadOnlyList<Category> categories, IReadOnlyList<RecurringItem> recurringItems)
    {
        Dictionary<Guid, Category> categoriesById = categories.ToDictionary(category => category.Id);
        Dictionary<Guid, string> recurringItemNames = recurringItems.ToDictionary(item => item.Id, item => item.Name);
        return booking =>
            Contains(BuildCategoryPath(booking.CategoryId, categoriesById), query)
            || Contains(booking.Description, query)
            || ((booking.RecurringItemId is Guid recurringItemId)
                && Contains(recurringItemNames.GetValueOrDefault(recurringItemId), query));
    }

    private static bool Contains(string? value, string query)
        => (value is not null) && value.Contains(query, StringComparison.CurrentCultureIgnoreCase);

    // Root first, e.g. "Haushalt › Lebensmittel", as the list shows it.
    private static string BuildCategoryPath(Guid categoryId, Dictionary<Guid, Category> categoriesById)
    {
        List<string> names = [];
        Guid? currentId = categoryId;
        while ((currentId is Guid id) && categoriesById.TryGetValue(id, out Category? category) && (names.Count < MaxDepth))
        {
            names.Insert(0, category.Name);
            currentId = category.ParentId;
        }

        return string.Join(" › ", names);
    }

    [GeneratedRegex(@"^\d+([.,]\d*)?$")]
    private static partial Regex NumberRegex();
}
