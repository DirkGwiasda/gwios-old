using GwiOS.Core.CrossCutting.Logging.Domain.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;
using GwiOS.WebUI.Tests.TestInfrastructure;
using BookingFilterUnderTest = GwiOS.WebUI.BookingFilters.BookingFilter;

namespace GwiOS.WebUI.Tests.BookingFilters.BookingFilter;

/// <summary>
/// Covers <c>BookingFilter.Filter</c> over a few bookings: "Edeka" (47,83 € in Haushalt › Lebensmittel), "Tanken"
/// (12,00 € in Mobilität), one without description (470,00 € in Mobilität) and one generated from the recurring item
/// "Miete" (950,00 € in Wohnen).
/// </summary>
public sealed class FilterTests
{
    private static readonly Category Household = new() { Name = "Haushalt", Type = CategoryType.Expense };
    private static readonly Category Groceries =
        new() { Name = "Lebensmittel", Type = CategoryType.Expense, ParentId = Household.Id };
    private static readonly Category Mobility = new() { Name = "Mobilität", Type = CategoryType.Expense };
    private static readonly Category Housing = new() { Name = "Wohnen", Type = CategoryType.Expense };
    private static readonly RecurringItem Rent = CreateRecurringItem("Miete");

    private readonly LoggerFake<BookingFilterUnderTest> _logger = new();
    private readonly BookingFilterUnderTest _filter;
    private readonly Booking _edeka = CreateBooking(Groceries, 47.83m, "Edeka");
    private readonly Booking _fuel = CreateBooking(Mobility, 12m, "Tanken");
    private readonly Booking _withoutDescription = CreateBooking(Mobility, 470m, null);
    private readonly Booking _rent = CreateBooking(Housing, 950m, null, Rent.Id);

    public FilterTests()
    {
        _filter = new BookingFilterUnderTest(_logger);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ReturnsAllBookings_WhenTheTextIsBlank(string text)
    {
        Assert.Equal([_edeka, _fuel, _withoutDescription, _rent], Filter(text));
    }

    [Theory]
    [InlineData("edeka")]
    [InlineData("DEK")]
    public void FindsBookingsByTheirDescription_IgnoringCase(string text)
    {
        Assert.Equal([_edeka], Filter(text));
    }

    [Fact]
    public void FindsBookingsByTheirCategory()
    {
        Assert.Equal([_fuel, _withoutDescription], Filter("mobil"));
    }

    [Fact]
    public void FindsSubCategoryBookingsByTheNameOfTheParentCategory()
    {
        Assert.Equal([_edeka], Filter("haushalt"));
    }

    [Fact]
    public void FindsBookingsOfARecurringItemByItsName()
    {
        Assert.Equal([_rent], Filter("miet"));
    }

    [Fact]
    public void CombinesCategoryAndDescriptionWithOr()
    {
        Booking groceriesWithMobilityDescription = CreateBooking(Groceries, 5m, "Mobilitätsticket");

        List<Booking> result = _filter.Filter(
            [_edeka, groceriesWithMobilityDescription, _fuel], [Household, Groceries, Mobility, Housing], [Rent], "mobil");

        Assert.Equal([groceriesWithMobilityDescription, _fuel], result);
    }

    [Fact]
    public void FindsBookingsWhoseAmountStartsWithANumber()
    {
        Assert.Equal([_edeka, _withoutDescription], Filter("47"));
    }

    [Theory]
    [InlineData("47,8")]
    [InlineData("47.83")]
    public void TakesACommaOrADotInANumberAsDecimalSeparator(string text)
    {
        Assert.Equal([_edeka], Filter(text));
    }

    [Fact]
    public void SearchesOnlyTheAmount_WhenTheTextIsANumber()
    {
        Booking numberInDescription = CreateBooking(Groceries, 3m, "Kasse 12");

        List<Booking> result = _filter.Filter(
            [_fuel, numberInDescription], [Household, Groceries, Mobility, Housing], [Rent], "12");

        Assert.Equal([_fuel], result);
    }

    [Fact]
    public void FindsNothing_WhenNothingMatches()
    {
        Assert.Empty(Filter("xyz"));
    }

    [Fact]
    public void LogsTheKindAndTheCountsButNotTheText()
    {
        Filter("edeka");

        LogEntry logEntry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Debug, logEntry.LogLevel);
        Assert.Equal("Bookings filtered.", logEntry.Message);
        Assert.Equal("Text", logEntry.ContextData["FilterKind"]);
        Assert.Equal("4", logEntry.ContextData["BookingCount"]);
        Assert.Equal("1", logEntry.ContextData["MatchCount"]);
        Assert.DoesNotContain(logEntry.ContextData.Values, value => value.Contains("edeka", StringComparison.OrdinalIgnoreCase));
    }

    private List<Booking> Filter(string text)
        => _filter.Filter([_edeka, _fuel, _withoutDescription, _rent], [Household, Groceries, Mobility, Housing], [Rent], text);

    private static Booking CreateBooking(Category category, decimal amount, string? description, Guid? recurringItemId = null)
        => new()
        {
            Date = new DateOnly(2026, 10, 3),
            Type = CategoryType.Expense,
            CategoryId = category.Id,
            Description = description,
            Amount = amount,
            Status = BookingStatus.Booked,
            RecurringItemId = recurringItemId
        };

    private static RecurringItem CreateRecurringItem(string name)
        => new()
        {
            Name = name,
            Type = CategoryType.Expense,
            CategoryId = Housing.Id,
            Amount = 950m,
            Interval = RecurringInterval.Monthly
        };
}
