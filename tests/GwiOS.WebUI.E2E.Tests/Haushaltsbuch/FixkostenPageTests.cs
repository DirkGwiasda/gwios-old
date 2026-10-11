using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace GwiOS.WebUI.E2E.Tests.Haushaltsbuch;

/// <summary>
/// End-to-end coverage of the Fixkosten tab of the Haushaltsbuch, driven through a real browser against the real
/// GwiOS.WebUI.
/// </summary>
[Collection(GwiOSAppCollection.Name)]
public sealed class FixkostenPageTests(GwiOSAppFixture app) : IAsyncLifetime
{
    private IBrowserContext _context = default!;
    private IPage _page = default!;

    public async ValueTask InitializeAsync()
    {
        _context = await app.Browser!.NewContextAsync();
        _page = await _context.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
        => await _context.DisposeAsync();

    [Fact]
    public async Task ShowsTheHeadingAndMonthlyEquivalentPanel()
    {
        await app.OpenAsync(_page, "/haushaltsbuch/fixkosten");

        await Expect(_page.Locator("h1")).ToHaveTextAsync("Haushaltsbuch");
        await Expect(_page.GetByText("Monatliches Äquivalent (aktiv)")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CreatesAndDeletesARecurringExpenseItem()
    {
        await using TestDataSeeder seeder = new();
        Category category = await seeder.CreateExpenseCategoryAsync();
        string name = TestDataSeeder.CreateUniqueName("e2e-fixkosten");

        await app.OpenAsync(_page, "/haushaltsbuch/fixkosten");
        await _page.GetByRole(AriaRole.Button, new() { Name = "+ Neuer wiederkehrender Posten" }).ClickAsync();
        await _page.Locator("#fix-edit-name").FillAsync(name);
        await _page.Locator("#fix-edit-category").SelectOptionAsync(category.Id.ToString());
        await _page.Locator("#fix-edit-amount").FillAsync("19.99");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Speichern" }).ClickAsync();

        ILocator row = _page.Locator("tr", new PageLocatorOptions { HasText = name });
        await Expect(row).ToBeVisibleAsync();
        await Expect(row).ToContainTextAsync("19,99");

        // The delete button asks for a second click to confirm; its title changes once clicked, so it is located
        // by position, not by its (changing) title.
        ILocator deleteButton = row.Locator(".gwios-table-actions button").Last;
        await deleteButton.ClickAsync();
        await deleteButton.ClickAsync();

        await Expect(row).Not.ToBeVisibleAsync();
    }

    [Fact]
    public async Task ContinuesARecurringItemWithAChangedAmountFromTheGivenDay()
    {
        await using TestDataSeeder seeder = new();
        IRecurringItemManager recurringItemManager = seeder.Services.GetRequiredService<IRecurringItemManager>();
        Category category = await seeder.CreateExpenseCategoryAsync();
        RecurringItem item = await recurringItemManager.CreateRecurringItemAsync(new RecurringItem
        {
            Name = TestDataSeeder.CreateUniqueName("e2e-fortfuehren"),
            Type = CategoryType.Expense,
            CategoryId = category.Id,
            Amount = 50m,
            Interval = RecurringInterval.Monthly
        }, TestContext.Current.CancellationToken);
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        DateOnly effectiveDate = new DateOnly(today.Year, today.Month, 1).AddMonths(1);

        try
        {
            await app.OpenAsync(_page, "/haushaltsbuch/fixkosten");
            ILocator rows = _page.Locator("tr", new PageLocatorOptions { HasText = item.Name });
            await rows.GetByTitle("Bearbeiten").ClickAsync();
            await _page.Locator("#fix-edit-amount").FillAsync("60");
            await _page.Locator("#fix-edit-continue-date").FillAsync(effectiveDate.ToString("yyyy-MM-dd"));
            await _page.GetByRole(AriaRole.Button, new() { Name = "Fortführen" }).ClickAsync();

            await Expect(rows).ToHaveCountAsync(2);
            await Expect(rows.Filter(new() { HasText = "50,00" }))
                .ToContainTextAsync($"bis {effectiveDate.AddDays(-1):dd.MM.yyyy}");
            await Expect(rows.Filter(new() { HasText = "60,00" })).ToContainTextAsync($"ab {effectiveDate:dd.MM.yyyy}");
        }
        finally
        {
            await DeleteWithSuccessorsAsync(recurringItemManager, item);
        }
    }

    [Fact]
    public async Task BackdatedContinuationAdjustsTheBookingsGeneratedFromTheRecurringItem()
    {
        await using TestDataSeeder seeder = new();
        IRecurringItemManager recurringItemManager = seeder.Services.GetRequiredService<IRecurringItemManager>();
        IBookingManager bookingManager = seeder.Services.GetRequiredService<IBookingManager>();
        Category category = await seeder.CreateExpenseCategoryAsync();
        RecurringItem item = await recurringItemManager.CreateRecurringItemAsync(new RecurringItem
        {
            Name = TestDataSeeder.CreateUniqueName("e2e-rueckwirkend"),
            Type = CategoryType.Expense,
            CategoryId = category.Id,
            Amount = 50m,
            Interval = RecurringInterval.Monthly
        }, TestContext.Current.CancellationToken);
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        DateOnly effectiveDate = new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
        Booking booking = await bookingManager.CreateBookingAsync(new Booking
        {
            Date = effectiveDate,
            Type = CategoryType.Expense,
            CategoryId = category.Id,
            Amount = 50m,
            Status = BookingStatus.Booked,
            RecurringItemId = item.Id
        }, TestContext.Current.CancellationToken);

        try
        {
            await app.OpenAsync(_page, "/haushaltsbuch/fixkosten");
            ILocator rows = _page.Locator("tr", new PageLocatorOptions { HasText = item.Name });
            await rows.GetByTitle("Bearbeiten").ClickAsync();
            await _page.Locator("#fix-edit-amount").FillAsync("60");
            await _page.Locator("#fix-edit-continue-date").FillAsync(effectiveDate.ToString("yyyy-MM-dd"));
            await _page.GetByRole(AriaRole.Button, new() { Name = "Fortführen" }).ClickAsync();

            await Expect(_page.GetByText("1 Buchung angepasst")).ToBeVisibleAsync();
            List<Booking> bookings = await bookingManager.GetBookingsInRangeAsync(
                effectiveDate,
                effectiveDate,
                TestContext.Current.CancellationToken);
            Booking adjusted = bookings.Single(candidate => candidate.Id == booking.Id);
            Assert.Equal(60m, adjusted.Amount);
            Assert.NotEqual(item.Id, adjusted.RecurringItemId);
        }
        finally
        {
            await bookingManager.DeleteBookingAsync(booking.Id, TestContext.Current.CancellationToken);
            await DeleteWithSuccessorsAsync(recurringItemManager, item);
        }
    }

    [Fact]
    public async Task AsksBeforeMovingBookingsToTheNewDueDayAndMovesThemWhenConfirmed()
    {
        await using TestDataSeeder seeder = new();
        IRecurringItemManager recurringItemManager = seeder.Services.GetRequiredService<IRecurringItemManager>();
        IBookingManager bookingManager = seeder.Services.GetRequiredService<IBookingManager>();
        Category category = await seeder.CreateExpenseCategoryAsync();
        RecurringItem item = await recurringItemManager.CreateRecurringItemAsync(new RecurringItem
        {
            Name = TestDataSeeder.CreateUniqueName("e2e-verschieben"),
            Type = CategoryType.Expense,
            CategoryId = category.Id,
            Amount = 50m,
            Interval = RecurringInterval.Monthly
        }, TestContext.Current.CancellationToken);
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        DateOnly effectiveDate = new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
        Booking booking = await bookingManager.CreateBookingAsync(new Booking
        {
            Date = effectiveDate,
            Type = CategoryType.Expense,
            CategoryId = category.Id,
            Amount = 50m,
            Status = BookingStatus.Booked,
            RecurringItemId = item.Id
        }, TestContext.Current.CancellationToken);

        try
        {
            await app.OpenAsync(_page, "/haushaltsbuch/fixkosten");
            ILocator rows = _page.Locator("tr", new PageLocatorOptions { HasText = item.Name });
            await rows.GetByTitle("Bearbeiten").ClickAsync();
            await _page.Locator("#fix-edit-due-day").FillAsync("15");
            await _page.Locator("#fix-edit-continue-date").FillAsync(effectiveDate.ToString("yyyy-MM-dd"));
            await _page.GetByRole(AriaRole.Button, new() { Name = "Fortführen" }).ClickAsync();

            ILocator dialog = _page.GetByRole(AriaRole.Dialog);
            await Expect(dialog).ToContainTextAsync("Buchungen an neuen Rhythmus anpassen?");
            await Expect(dialog).ToContainTextAsync($"→ {effectiveDate.AddDays(14):dd.MM.yyyy}");
            await dialog.GetByRole(AriaRole.Button, new() { Name = "Anpassen" }).ClickAsync();

            await Expect(_page.GetByText("1 Buchung verschoben")).ToBeVisibleAsync();
            List<Booking> bookings = await bookingManager.GetBookingsInRangeAsync(
                effectiveDate,
                effectiveDate.AddMonths(1).AddDays(-1),
                TestContext.Current.CancellationToken);
            Assert.Equal(effectiveDate.AddDays(14), bookings.Single(candidate => candidate.Id == booking.Id).Date);
        }
        finally
        {
            await bookingManager.DeleteBookingAsync(booking.Id, TestContext.Current.CancellationToken);
            await DeleteWithSuccessorsAsync(recurringItemManager, item);
        }
    }

    // The successor goes first: deleting it reopens the predecessor, which is then deleted as well.
    private static async Task DeleteWithSuccessorsAsync(IRecurringItemManager recurringItemManager, RecurringItem item)
    {
        List<RecurringItem> items = await recurringItemManager.GetAllRecurringItemsAsync();
        foreach (RecurringItem successor in items.Where(candidate => candidate.PredecessorId == item.Id))
        {
            await recurringItemManager.DeleteRecurringItemAsync(successor.Id);
        }

        await recurringItemManager.DeleteRecurringItemAsync(item.Id);
    }
}
