using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;

namespace GwiOS.Core.Tests.Haushaltsbuch.Domain.Managers.BookingManagement.BookingManager;

/// <summary>
/// Covers <c>BookingManager.GetBookingSuggestionsAsync</c>.
/// </summary>
public sealed class GetBookingSuggestionsAsyncTests
{
    private static readonly Guid GroceriesId = Guid.CreateVersion7();

    private readonly BookingManagerProbe _probe = new();

    [Fact]
    public async Task ReturnsTheSuggestionsOfTheRepository()
    {
        await InsertAsync("Edeka", new DateOnly(2026, 10, 3));
        await InsertAsync("Edeka ", new DateOnly(2026, 10, 5));

        List<BookingSuggestion> suggestions =
            await _probe.BookingManager.GetBookingSuggestionsAsync(TestContext.Current.CancellationToken);

        BookingSuggestion suggestion = Assert.Single(suggestions);
        Assert.Equal("Edeka", suggestion.Description);
        Assert.Equal(GroceriesId, suggestion.CategoryId);
        Assert.Equal(2, suggestion.UsageCount);
        Assert.Equal(new DateOnly(2026, 10, 5), suggestion.LastUsedOn);
    }

    [Fact]
    public async Task ReturnsAnEmptyList_WhenNoBookingHasADescription()
    {
        await InsertAsync(null, new DateOnly(2026, 10, 3));

        List<BookingSuggestion> suggestions =
            await _probe.BookingManager.GetBookingSuggestionsAsync(TestContext.Current.CancellationToken);

        Assert.Empty(suggestions);
    }

    private async Task InsertAsync(string? description, DateOnly date)
        => await _probe.BookingRepository.InsertAsync(
            new Booking
            {
                Date = date, Type = CategoryType.Expense, CategoryId = GroceriesId, Description = description,
                Amount = 47.83m, Status = BookingStatus.Booked
            },
            TestContext.Current.CancellationToken);
}
