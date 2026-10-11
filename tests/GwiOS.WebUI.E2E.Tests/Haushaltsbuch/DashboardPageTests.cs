using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts;
using System.Globalization;
using System.Text.RegularExpressions;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.RecurringBookingGeneration.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace GwiOS.WebUI.E2E.Tests.Haushaltsbuch;

/// <summary>
/// End-to-end coverage of the Dashboard tab of the Haushaltsbuch, driven through a real browser against the real
/// GwiOS.WebUI.
/// </summary>
[Collection(GwiOSAppCollection.Name)]
public sealed class DashboardPageTests(GwiOSAppFixture app) : IAsyncLifetime
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
    public async Task ShowsTheHeadingAndKpiTiles()
    {
        await app.OpenAsync(_page, "/haushaltsbuch/dashboard");

        await Expect(_page.Locator("h1")).ToHaveTextAsync("Haushaltsbuch");
        await Expect(_page.GetByText("Gesamteinnahmen")).ToBeVisibleAsync();
        await Expect(_page.GetByText("Gesamtausgaben")).ToBeVisibleAsync();
        await Expect(_page.GetByText("Verfügbarer Saldo")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CreatesANewBookingAndShowsItInTheTable()
    {
        await using TestDataSeeder seeder = new();
        Category category = await seeder.CreateExpenseCategoryAsync();
        string description = TestDataSeeder.CreateUniqueName("e2e-booking");

        await app.OpenAsync(_page, "/haushaltsbuch/dashboard");
        ILocator dialog = await OpenNewBookingDialogAsync();
        await dialog.Locator("#booking-edit-amount").FillAsync("42.50");
        await PickCategoryBySearchAsync(dialog, category);
        await FillDescriptionAsync(dialog, description);
        await ClickSaveAsync(dialog);

        ILocator row = _page.Locator("tr", new PageLocatorOptions { HasText = description });
        await Expect(row).ToBeVisibleAsync();
        await Expect(row).ToContainTextAsync("42,50");

        await CleanUpBookingAsync(seeder, description);
    }

    [Fact]
    public async Task AFavoriteFillsDescriptionAndCategoryAndTheAmountMayBeACalculation()
    {
        await using TestDataSeeder seeder = new();
        Category category = await seeder.CreateExpenseCategoryAsync();
        BookingTemplate template = await CreateBookingTemplateAsync(seeder, category);

        await app.OpenAsync(_page, "/haushaltsbuch/dashboard");
        ILocator dialog = await OpenNewBookingDialogAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = $"★{template.Description}" }).ClickAsync();

        await Expect(dialog.Locator("#booking-edit-description")).ToHaveValueAsync(template.Description);
        await Expect(dialog.Locator(".gwios-category-picker-selected")).ToContainTextAsync(category.Name);
        await Expect(dialog.Locator("#booking-edit-amount")).ToBeFocusedAsync();

        await dialog.Locator("#booking-edit-amount").FillAsync("12,50+3,20");
        await Expect(dialog.Locator(".gwios-amount-box-result")).ToHaveTextAsync("= 15,70 €");
        await ClickSaveAsync(dialog);

        ILocator row = _page.Locator("tr", new PageLocatorOptions { HasText = template.Description });
        await Expect(row).ToBeVisibleAsync();
        await Expect(row).ToContainTextAsync("15,70");

        await CleanUpBookingAsync(seeder, template.Description);
    }

    [Fact]
    public async Task ThePinnedFavoriteCategoryCanBePickedWithOneTap()
    {
        await using TestDataSeeder seeder = new();
        Category category = await seeder.CreateExpenseCategoryAsync();
        category.IsFavorite = true;
        await seeder.Services.GetRequiredService<ICategoryManager>()
            .UpdateCategoryAsync(category, TestContext.Current.CancellationToken);

        await app.OpenAsync(_page, "/haushaltsbuch/dashboard");
        ILocator dialog = await OpenNewBookingDialogAsync();
        await dialog.Locator(".gwios-category-picker .gwios-chip", new() { HasText = category.Name }).ClickAsync();

        await Expect(dialog.Locator(".gwios-category-picker-selected")).ToContainTextAsync(category.Name);
    }

    [Fact]
    public async Task TheUndoBarTakesBackTheBookingJustSaved()
    {
        await using TestDataSeeder seeder = new();
        Category category = await seeder.CreateExpenseCategoryAsync();
        string description = TestDataSeeder.CreateUniqueName("e2e-undo");

        await app.OpenAsync(_page, "/haushaltsbuch/dashboard");
        ILocator dialog = await OpenNewBookingDialogAsync();
        await dialog.Locator("#booking-edit-amount").FillAsync("9,99");
        await PickCategoryBySearchAsync(dialog, category);
        await FillDescriptionAsync(dialog, description);
        await ClickSaveAsync(dialog);

        ILocator row = _page.Locator("tr", new PageLocatorOptions { HasText = description });
        await Expect(row).ToBeVisibleAsync();
        ILocator undoBar = _page.Locator(".gwios-undo-bar");
        await Expect(undoBar).ToContainTextAsync($"{description} · −9,99 € gebucht");
        await undoBar.GetByRole(AriaRole.Button, new() { Name = "Rückgängig" }).ClickAsync();

        await Expect(undoBar).ToContainTextAsync("Buchung zurückgenommen");
        await Expect(row).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task SaveAndContinueKeepsTheDialogOpenWithAnEmptyFormAndTheSameDate()
    {
        await using TestDataSeeder seeder = new();
        Category category = await seeder.CreateExpenseCategoryAsync();
        string description = TestDataSeeder.CreateUniqueName("e2e-next");

        await app.OpenAsync(_page, "/haushaltsbuch/dashboard");
        ILocator dialog = await OpenNewBookingDialogAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Gestern", Exact = true }).ClickAsync();
        await dialog.Locator("#booking-edit-amount").FillAsync("3,10");
        await PickCategoryBySearchAsync(dialog, category);
        await FillDescriptionAsync(dialog, description);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Speichern & weitere" }).ClickAsync();

        await Expect(dialog.Locator(".gwios-booking-flash")).ToContainTextAsync("bereit für die nächste");
        await Expect(dialog.Locator("#booking-edit-amount")).ToHaveValueAsync(string.Empty);
        await Expect(dialog.Locator("#booking-edit-description")).ToHaveValueAsync(string.Empty);
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Gestern", Exact = true }))
            .ToHaveAttributeAsync("aria-pressed", "true");

        await CleanUpBookingAsync(seeder, description);
    }

    [Fact]
    public async Task TheStarInTheDialogPinsDescriptionAndCategoryAsFavorite()
    {
        await using TestDataSeeder seeder = new();
        Category category = await seeder.CreateExpenseCategoryAsync();
        string description = TestDataSeeder.CreateUniqueName("e2e-favorite");

        await app.OpenAsync(_page, "/haushaltsbuch/dashboard");
        ILocator dialog = await OpenNewBookingDialogAsync();
        await PickCategoryBySearchAsync(dialog, category);
        await FillDescriptionAsync(dialog, description);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "☆ Als Favorit speichern" }).ClickAsync();

        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "★ Favorit" })).ToBeVisibleAsync();
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = $"★{description}" })).ToBeVisibleAsync();
        List<BookingTemplate> templates = await seeder.Services.GetRequiredService<IBookingTemplateManager>()
            .GetAllBookingTemplatesAsync(TestContext.Current.CancellationToken);
        Assert.Contains(
            templates,
            template => (template.Description == description) && (template.CategoryId == category.Id));
    }

    [Fact]
    public async Task GenerateFixBookingsTakesOverOnlyTheRecurringItemsLeftSelectedInTheDialog()
    {
        await using TestDataSeeder seeder = new();
        Category category = await seeder.CreateExpenseCategoryAsync();
        RecurringItem keptItem = await CreateMonthlyRecurringItemAsync(seeder, category);
        RecurringItem deselectedItem = await CreateMonthlyRecurringItemAsync(seeder, category);
        await MarkCurrentMonthAsReviewedAsync(seeder);

        try
        {
            await app.OpenAsync(_page, "/haushaltsbuch/dashboard");
            ILocator dialog = await OpenFixBookingSelectionAsync();
            await Expect(dialog.GetByRole(AriaRole.Checkbox, new() { Name = keptItem.Name })).ToBeCheckedAsync();
            await Expect(dialog.GetByRole(AriaRole.Checkbox, new() { Name = deselectedItem.Name })).ToBeCheckedAsync();
            await UncheckAllProposalsExceptAsync(dialog, keptItem.Name);
            await dialog.GetByRole(AriaRole.Button, new() { Name = "Übernehmen" }).ClickAsync();

            await Expect(dialog).ToBeHiddenAsync();
            ILocator keptRow = _page.Locator("tr", new PageLocatorOptions { HasText = keptItem.Name });
            await Expect(keptRow).ToBeVisibleAsync();
            await Expect(keptRow.Locator(".gwios-recurring-item-label")).ToHaveTextAsync(new Regex(Regex.Escape(keptItem.Name)));
            await Expect(_page.Locator("tr", new PageLocatorOptions { HasText = deselectedItem.Name })).ToHaveCountAsync(0);
        }
        finally
        {
            await CleanUpRecurringItemsAsync(seeder, keptItem, deselectedItem);
        }
    }

    [Fact]
    public async Task GenerateFixBookingsCreatesNothingWhenTheDialogIsCancelled()
    {
        await using TestDataSeeder seeder = new();
        Category category = await seeder.CreateExpenseCategoryAsync();
        RecurringItem item = await CreateMonthlyRecurringItemAsync(seeder, category);
        await MarkCurrentMonthAsReviewedAsync(seeder);

        try
        {
            await app.OpenAsync(_page, "/haushaltsbuch/dashboard");
            ILocator dialog = await OpenFixBookingSelectionAsync();
            await dialog.GetByRole(AriaRole.Button, new() { Name = "Abbrechen" }).ClickAsync();

            await Expect(dialog).ToBeHiddenAsync();
            await Expect(_page.Locator("tr", new PageLocatorOptions { HasText = item.Name })).ToHaveCountAsync(0);
        }
        finally
        {
            await CleanUpRecurringItemsAsync(seeder, item);
        }
    }

    [Fact]
    public async Task AvailableBalanceCountsRecurringItemsAndBookedOtherBookingsButNotPendingOnes()
    {
        await using TestDataSeeder seeder = new();
        Category category = await seeder.CreateExpenseCategoryAsync();
        await MarkCurrentMonthAsReviewedAsync(seeder);
        await app.OpenAsync(_page, "/haushaltsbuch/dashboard");
        decimal balanceBefore = await ReadAvailableBalanceAsync();

        RecurringItem item = await CreateMonthlyRecurringItemAsync(seeder, category);
        string bookedDescription = await CreateTodaysExpenseAsync(seeder, category, 20m, BookingStatus.Booked);
        string pendingDescription = await CreateTodaysExpenseAsync(seeder, category, 300m, BookingStatus.Pending);

        try
        {
            await app.OpenAsync(_page, "/haushaltsbuch/dashboard");

            await Expect(AvailableBalanceValue()).ToHaveTextAsync(FormatSignedAmount(balanceBefore - item.Amount - 20m));
        }
        finally
        {
            await CleanUpBookingAsync(seeder, bookedDescription);
            await CleanUpBookingAsync(seeder, pendingDescription);
            await CleanUpRecurringItemsAsync(seeder, item);
        }
    }

    [Fact]
    public async Task ANewBookingStartsWithTheDateEnteredLastInTheSession()
    {
        await using TestDataSeeder seeder = new();
        Category category = await seeder.CreateExpenseCategoryAsync();
        string description = TestDataSeeder.CreateUniqueName("e2e-recent-date");
        DateOnly olderDate = DateOnly.FromDateTime(DateTime.Now).AddDays(-6);

        await app.OpenAsync(_page, "/haushaltsbuch/dashboard");
        ILocator dialog = await OpenNewBookingDialogAsync();
        await dialog.Locator("#booking-edit-amount").FillAsync("500");
        await PickCategoryBySearchAsync(dialog, category);
        await FillDescriptionAsync(dialog, description);
        await dialog.Locator("input[type='date']").FillAsync(olderDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        await ClickSaveAsync(dialog);
        await Expect(dialog).ToBeHiddenAsync();

        dialog = await OpenNewBookingDialogAsync();

        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = $"Zuletzt {olderDate:dd.MM.}" }))
            .ToHaveAttributeAsync("aria-pressed", "true");

        await CleanUpBookingAsync(seeder, description);
    }

    [Fact]
    public async Task TheAmountTakesTheLastTwoOfThreeOrMoreDigitsAsCentsWhileTyping()
    {
        await app.OpenAsync(_page, "/haushaltsbuch/dashboard");
        ILocator dialog = await OpenNewBookingDialogAsync();
        ILocator amount = dialog.Locator("#booking-edit-amount");

        await amount.PressSequentiallyAsync("125");
        await Expect(amount).ToHaveValueAsync("1,25");
        await amount.PressSequentiallyAsync("0");
        await Expect(amount).ToHaveValueAsync("12,50");

        // A typed comma is dropped; it must not stay visible although the value does not change.
        await amount.FillAsync(string.Empty);
        await amount.PressSequentiallyAsync("12,");
        await Expect(amount).ToHaveValueAsync("12");
    }

    [Fact]
    public async Task TabLeadsFromTheAmountStraightToTheDescription()
    {
        await app.OpenAsync(_page, "/haushaltsbuch/dashboard");
        ILocator dialog = await OpenNewBookingDialogAsync();
        await Expect(dialog.Locator("#booking-edit-amount")).ToBeFocusedAsync();

        await _page.Keyboard.PressAsync("Tab");

        await Expect(dialog.Locator("#booking-edit-description")).ToBeFocusedAsync();
    }

    [Fact]
    public async Task TheDescriptionSuggestsEarlierBookingsAndTakesOverTheirCategory()
    {
        await using TestDataSeeder seeder = new();
        Category category = await seeder.CreateExpenseCategoryAsync();
        string description = await CreateTodaysExpenseAsync(seeder, category, 4.2m, BookingStatus.Booked);

        await app.OpenAsync(_page, "/haushaltsbuch/dashboard");
        ILocator dialog = await OpenNewBookingDialogAsync();
        await dialog.Locator("#booking-edit-description").PressSequentiallyAsync(description[..^4]);
        ILocator option = dialog.GetByRole(AriaRole.Option, new() { Name = description });
        await Expect(option).ToContainTextAsync(category.Name);
        await option.ClickAsync();

        await Expect(dialog.Locator("#booking-edit-description")).ToHaveValueAsync(description);
        await Expect(dialog.Locator(".gwios-category-picker-selected")).ToContainTextAsync(category.Name);
        await Expect(dialog.Locator("#booking-edit-amount")).ToBeFocusedAsync();
        await Expect(dialog.Locator("#booking-edit-amount")).ToHaveValueAsync(string.Empty);

        await CleanUpBookingAsync(seeder, description);
    }

    [Fact]
    public async Task TheMonthOverviewShowsTheBookedCategoriesAndCanBeDownloadedAsPdf()
    {
        await using TestDataSeeder seeder = new();
        Category category = await seeder.CreateExpenseCategoryAsync();
        string description = await CreateTodaysExpenseAsync(seeder, category, 7.77m, BookingStatus.Booked);

        await app.OpenAsync(_page, "/haushaltsbuch/dashboard");
        IDownload download = await _page.RunAndWaitForDownloadAsync(
            () => _page.GetByRole(AriaRole.Link, new() { Name = "Monatsübersicht drucken" }).ClickAsync());
        Assert.Equal(
            $"monatsuebersicht-{DateTime.Now.ToString("yyyy-MM", CultureInfo.InvariantCulture)}.pdf",
            download.SuggestedFilename);

        await _page.GetByRole(AriaRole.Button, new() { Name = "Monatsübersicht anzeigen" }).ClickAsync();

        ILocator dialog = _page.GetByRole(AriaRole.Dialog, new() { Name = "Monatsübersicht" });
        await Expect(dialog.GetByText("Einnahmen", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(dialog.Locator(".gwios-month-overview-row", new() { HasText = category.Name })).ToContainTextAsync("7,77 €");

        await CleanUpBookingAsync(seeder, description);
    }

    [Fact]
    public async Task TheFilterAboveTheListFindsBookingsByDescriptionOrAmount()
    {
        await using TestDataSeeder seeder = new();
        Category category = await seeder.CreateExpenseCategoryAsync();
        string firstDescription = await CreateTodaysExpenseAsync(seeder, category, 8765.43m, BookingStatus.Booked);
        string secondDescription = await CreateTodaysExpenseAsync(seeder, category, 1.11m, BookingStatus.Booked);

        await app.OpenAsync(_page, "/haushaltsbuch/dashboard");
        ILocator filter = _page.GetByRole(AriaRole.Searchbox, new() { Name = "Buchungen filtern" });
        ILocator firstRow = _page.Locator("tr", new PageLocatorOptions { HasText = firstDescription });
        ILocator secondRow = _page.Locator("tr", new PageLocatorOptions { HasText = secondDescription });

        await filter.FillAsync(secondDescription.ToUpperInvariant());
        await Expect(secondRow).ToBeVisibleAsync();
        await Expect(firstRow).ToHaveCountAsync(0);

        await filter.FillAsync("8765,4");
        await Expect(firstRow).ToBeVisibleAsync();
        await Expect(secondRow).ToHaveCountAsync(0);

        await filter.FillAsync(string.Empty);
        await Expect(firstRow).ToBeVisibleAsync();
        await Expect(secondRow).ToBeVisibleAsync();

        await CleanUpBookingAsync(seeder, firstDescription);
        await CleanUpBookingAsync(seeder, secondDescription);
    }

    private async Task<ILocator> OpenNewBookingDialogAsync()
    {
        await _page.GetByRole(AriaRole.Button, new() { Name = "+ Neue Buchung" }).ClickAsync();
        // By name, because the category picker opens a second dialog inside this one.
        ILocator dialog = _page.GetByRole(AriaRole.Dialog, new() { Name = "Neue Buchung" });
        await Expect(dialog).ToBeVisibleAsync();
        return dialog;
    }

    // Filters by the full name, so that exactly the seeded category is left to click.
    private static async Task PickCategoryBySearchAsync(ILocator dialog, Category category)
    {
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Kategorie suchen" }).ClickAsync();
        ILocator treeDialog = dialog.Page.GetByRole(AriaRole.Dialog, new() { Name = "Kategorie wählen" });
        await Expect(treeDialog.GetByRole(AriaRole.Textbox, new() { Name = "Kategorien filtern" })).ToBeFocusedAsync();
        await treeDialog.GetByRole(AriaRole.Textbox, new() { Name = "Kategorien filtern" }).FillAsync(category.Name);
        await treeDialog.GetByRole(AriaRole.Button, new() { Name = category.Name, Exact = true }).ClickAsync();
        await Expect(treeDialog).ToBeHiddenAsync();
    }

    // Tab leaves the field, which closes the list of suggestions, so it cannot cover the next control to click.
    private static async Task FillDescriptionAsync(ILocator dialog, string description)
    {
        await dialog.Locator("#booking-edit-description").FillAsync(description);
        await dialog.Locator("#booking-edit-description").PressAsync("Tab");
    }

    // Exact, because "Speichern & weitere" would match as well.
    private static async Task ClickSaveAsync(ILocator dialog)
        => await dialog.Page.GetByRole(AriaRole.Button, new() { Name = "Speichern", Exact = true }).ClickAsync();

    // The template is deleted together with the seeded category, so it needs no cleanup of its own.
    private static async Task<BookingTemplate> CreateBookingTemplateAsync(TestDataSeeder seeder, Category category)
        => await seeder.Services.GetRequiredService<IBookingTemplateManager>().CreateBookingTemplateAsync(
            new BookingTemplate
            {
                Description = TestDataSeeder.CreateUniqueName("e2e-template"), CategoryId = category.Id
            });

    private ILocator AvailableBalanceValue()
        => _page.Locator(".gwios-kpi-tile", new PageLocatorOptions { HasText = "Verfügbarer Saldo" })
            .Locator(".gwios-kpi-tile-value");

    private async Task<decimal> ReadAvailableBalanceAsync()
    {
        string text = await AvailableBalanceValue().InnerTextAsync();
        decimal amount = decimal.Parse(
            text.TrimStart('+', '–', ' '),
            NumberStyles.Currency,
            CultureInfo.GetCultureInfo("de-DE"));
        return text.StartsWith('–') ? -amount : amount;
    }

    // Mirrors the formatting of the Dashboard page.
    private static string FormatSignedAmount(decimal value)
        => (value >= 0 ? "+ " : "– ") + Math.Abs(value).ToString("C", CultureInfo.GetCultureInfo("de-DE"));

    private static async Task<string> CreateTodaysExpenseAsync(
        TestDataSeeder seeder,
        Category category,
        decimal amount,
        BookingStatus status)
    {
        IBookingManager bookingManager = seeder.Services.GetRequiredService<IBookingManager>();
        string description = TestDataSeeder.CreateUniqueName("e2e-balance");
        await bookingManager.CreateBookingAsync(new Booking
        {
            Date = DateOnly.FromDateTime(DateTime.Now),
            Type = CategoryType.Expense,
            CategoryId = category.Id,
            Description = description,
            Amount = amount,
            Status = status
        });
        return description;
    }

    private async Task<ILocator> OpenFixBookingSelectionAsync()
    {
        Regex generateButtonName = new("^Fixbuchungen für .+ generieren");
        await _page.GetByRole(AriaRole.Button, new() { NameRegex = generateButtonName }).ClickAsync();
        ILocator dialog = _page.GetByRole(AriaRole.Dialog);
        await Expect(dialog).ToBeVisibleAsync();
        return dialog;
    }

    // Other recurring items may exist in the shared test database; leaving only the own one selected keeps the test
    // from creating bookings it would not clean up.
    private static async Task UncheckAllProposalsExceptAsync(ILocator dialog, string keptName)
    {
        foreach (ILocator checkbox in await dialog.GetByRole(AriaRole.Checkbox).AllAsync())
        {
            string label = await checkbox.Locator("xpath=..").InnerTextAsync();
            if (!label.Contains(keptName))
            {
                await checkbox.UncheckAsync();
            }
        }
    }

    private static async Task<RecurringItem> CreateMonthlyRecurringItemAsync(TestDataSeeder seeder, Category category)
    {
        IRecurringItemManager recurringItemManager = seeder.Services.GetRequiredService<IRecurringItemManager>();
        RecurringItem item = new()
        {
            Name = TestDataSeeder.CreateUniqueName("e2e-recurring"),
            Type = CategoryType.Expense,
            CategoryId = category.Id,
            Amount = 12.34m,
            Interval = RecurringInterval.Monthly
        };
        return await recurringItemManager.CreateRecurringItemAsync(item);
    }

    // Without this, the automatic month review dialog would open instead and cover the button under test.
    private static async Task MarkCurrentMonthAsReviewedAsync(TestDataSeeder seeder)
    {
        IRecurringBookingGenerationWorkflow workflow =
            seeder.Services.GetRequiredService<IRecurringBookingGenerationWorkflow>();
        await workflow.ConfirmAsync(DateOnly.FromDateTime(DateTime.Now), []);
    }

    private static async Task CleanUpRecurringItemsAsync(TestDataSeeder seeder, params RecurringItem[] items)
    {
        IBookingManager bookingManager = seeder.Services.GetRequiredService<IBookingManager>();
        IRecurringItemManager recurringItemManager = seeder.Services.GetRequiredService<IRecurringItemManager>();
        HashSet<Guid> itemIds = [.. items.Select(item => item.Id)];
        List<Booking> bookings = await bookingManager.GetBookingsInRangeAsync(DateOnly.MinValue, DateOnly.MaxValue);
        foreach (Booking booking in bookings.Where(booking => (booking.RecurringItemId is Guid id) && itemIds.Contains(id)))
        {
            await bookingManager.DeleteBookingAsync(booking.Id);
        }

        foreach (RecurringItem item in items)
        {
            await recurringItemManager.DeleteRecurringItemAsync(item.Id);
        }
    }

    private static async Task CleanUpBookingAsync(TestDataSeeder seeder, string description)
    {
        IBookingManager bookingManager = seeder.Services.GetRequiredService<IBookingManager>();
        List<Booking> bookings = await bookingManager.GetBookingsInRangeAsync(DateOnly.MinValue, DateOnly.MaxValue);
        Booking booking = bookings.Single(candidate => candidate.Description == description);
        await bookingManager.DeleteBookingAsync(booking.Id);
    }
}
