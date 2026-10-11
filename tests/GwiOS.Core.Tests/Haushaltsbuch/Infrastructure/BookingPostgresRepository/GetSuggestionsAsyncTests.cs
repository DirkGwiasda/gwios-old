using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts.Models;
using GwiOS.Core.Tests.TestInfrastructure;

namespace GwiOS.Core.Tests.Haushaltsbuch.Infrastructure.BookingPostgresRepository;

/// <summary>
/// Covers <c>BookingPostgresRepository.GetSuggestionsAsync</c> against the PostgreSQL test database. The database may
/// contain bookings of other tests, so the tests only look at the suggestions for the categories they created
/// themselves.
/// </summary>
[Collection(PostgresTestDatabaseCollection.Name)]
public sealed class GetSuggestionsAsyncTests(PostgresTestDatabase database) : IAsyncLifetime
{
    private readonly IBookingRepository _bookingRepository = database.BookingRepository;
    private readonly TestBookings _testBookings = new(database.CategoryRepository, database.BookingRepository);

    [Fact]
    public async Task GroupsBookingsByTrimmedDescriptionAndCategoryWithCountAndLastDate()
    {
        Booking first = await _testBookings.CreateAsync(new DateOnly(2026, 9, 3), TestContext.Current.CancellationToken);
        first.Description = "Edeka";
        Booking second = _testBookings.CreateForCategory(first.CategoryId, new DateOnly(2026, 9, 20));
        second.Description = " Edeka ";
        await _bookingRepository.InsertAsync(first, TestContext.Current.CancellationToken);
        await _bookingRepository.InsertAsync(second, TestContext.Current.CancellationToken);

        List<BookingSuggestion> suggestions =
            await _bookingRepository.GetSuggestionsAsync(TestContext.Current.CancellationToken);

        BookingSuggestion suggestion = Assert.Single(suggestions, candidate => candidate.CategoryId == first.CategoryId);
        Assert.Equal("Edeka", suggestion.Description);
        Assert.Equal(2, suggestion.UsageCount);
        Assert.Equal(new DateOnly(2026, 9, 20), suggestion.LastUsedOn);
    }

    [Fact]
    public async Task LeavesOutBookingsWithoutDescription()
    {
        Booking withoutDescription =
            await _testBookings.CreateAsync(new DateOnly(2026, 9, 3), TestContext.Current.CancellationToken);
        Booking blankDescription = _testBookings.CreateForCategory(withoutDescription.CategoryId, new DateOnly(2026, 9, 4));
        blankDescription.Description = "  ";
        await _bookingRepository.InsertAsync(withoutDescription, TestContext.Current.CancellationToken);
        await _bookingRepository.InsertAsync(blankDescription, TestContext.Current.CancellationToken);

        List<BookingSuggestion> suggestions =
            await _bookingRepository.GetSuggestionsAsync(TestContext.Current.CancellationToken);

        Assert.DoesNotContain(suggestions, suggestion => suggestion.CategoryId == withoutDescription.CategoryId);
    }

    [Fact]
    public async Task OrdersByUsageCountThenByLastUse()
    {
        Booking rare = await _testBookings.CreateAsync(new DateOnly(2026, 9, 25), TestContext.Current.CancellationToken);
        rare.Description = "Selten";
        Booking frequent = _testBookings.CreateForCategory(rare.CategoryId, new DateOnly(2026, 9, 1));
        frequent.Description = "Oft";
        Booking frequentAgain = _testBookings.CreateForCategory(rare.CategoryId, new DateOnly(2026, 9, 2));
        frequentAgain.Description = "Oft";
        Booking recent = _testBookings.CreateForCategory(rare.CategoryId, new DateOnly(2026, 9, 28));
        recent.Description = "Neu";
        foreach (Booking booking in new[] { rare, frequent, frequentAgain, recent })
        {
            await _bookingRepository.InsertAsync(booking, TestContext.Current.CancellationToken);
        }

        List<BookingSuggestion> suggestions =
            await _bookingRepository.GetSuggestionsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            ["Oft", "Neu", "Selten"],
            suggestions.Where(suggestion => suggestion.CategoryId == rare.CategoryId).Select(suggestion => suggestion.Description));
    }

    public ValueTask InitializeAsync()
        => ValueTask.CompletedTask;

    public ValueTask DisposeAsync()
        => _testBookings.DisposeAsync();
}
