using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace GwiOS.WebUI.E2E.Tests.Haushaltsbuch;

/// <summary>
/// End-to-end coverage of the Kategorien tab of the Haushaltsbuch, driven through a real browser against the real
/// GwiOS.WebUI.
/// </summary>
[Collection(GwiOSAppCollection.Name)]
public sealed class KategorienPageTests(GwiOSAppFixture app) : IAsyncLifetime
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
    public async Task ShowsTheIncomeAndExpenseTrees()
    {
        await app.OpenAsync(_page, "/haushaltsbuch/kategorien");

        await Expect(_page.Locator("h1")).ToHaveTextAsync("Haushaltsbuch");
        await Expect(_page.GetByText("Einnahmen", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(_page.GetByText("Ausgaben", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CreatesAndDeletesAnExpenseRootCategory()
    {
        string name = TestDataSeeder.CreateUniqueName("e2e-category");
        await app.OpenAsync(_page, "/haushaltsbuch/kategorien");

        ILocator expenseSection = _page.Locator("section", new PageLocatorOptions { HasText = "Ausgaben" });
        await expenseSection.GetByRole(AriaRole.Button, new() { Name = "+ Hauptkategorie" }).ClickAsync();
        await _page.Locator("#category-edit-name").FillAsync(name);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Speichern" }).ClickAsync();

        ILocator categoryRow = _page.Locator(".gwios-category-row", new PageLocatorOptions { HasText = name });
        await Expect(categoryRow).ToBeVisibleAsync();

        // The delete button asks for a second click to confirm; its title changes once clicked, so it is located
        // by position, not by its (changing) title.
        ILocator deleteButton = categoryRow.Locator(".gwios-category-actions button").Last;
        await deleteButton.ClickAsync();
        await deleteButton.ClickAsync();

        await Expect(categoryRow).Not.ToBeVisibleAsync();
    }

    [Fact]
    public async Task MarksAndUnmarksACategoryAsFavorite()
    {
        await using TestDataSeeder seeder = new();
        Category category = await seeder.CreateExpenseCategoryAsync();
        await app.OpenAsync(_page, "/haushaltsbuch/kategorien");
        ILocator star = _page.Locator(".gwios-category-row", new PageLocatorOptions { HasText = category.Name })
            .Locator(".gwios-category-favorite");

        await star.ClickAsync();

        await Expect(star).ToHaveTextAsync("★");
        Assert.True(await IsStoredAsFavoriteAsync(seeder, category));

        await star.ClickAsync();

        await Expect(star).ToHaveTextAsync("☆");
        Assert.False(await IsStoredAsFavoriteAsync(seeder, category));
    }

    [Fact]
    public async Task RenamesAndDeletesAFavoriteInTheFavoritesCard()
    {
        await using TestDataSeeder seeder = new();
        Category category = await seeder.CreateExpenseCategoryAsync();
        BookingTemplate template = await seeder.Services.GetRequiredService<IBookingTemplateManager>()
            .CreateBookingTemplateAsync(new BookingTemplate
            {
                Description = TestDataSeeder.CreateUniqueName("e2e-template"), CategoryId = category.Id
            });
        string newName = $"{template.Description}-neu";
        await app.OpenAsync(_page, "/haushaltsbuch/kategorien");
        ILocator row = _page.Locator(".gwios-booking-template-row", new PageLocatorOptions { HasText = template.Description });

        await row.GetByRole(AriaRole.Button, new() { Name = "Umbenennen" }).ClickAsync();
        await row.GetByRole(AriaRole.Textbox, new() { Name = "Neuer Name" }).FillAsync(newName);
        await row.GetByRole(AriaRole.Textbox, new() { Name = "Neuer Name" }).PressAsync("Enter");

        ILocator renamedRow = _page.Locator(".gwios-booking-template-row", new PageLocatorOptions { HasText = newName });
        await Expect(renamedRow).ToContainTextAsync(category.Name);

        // The delete button asks for a second click to confirm.
        ILocator deleteButton = renamedRow.Locator(".gwios-booking-template-actions button").Last;
        await deleteButton.ClickAsync();
        await deleteButton.ClickAsync();

        await Expect(renamedRow).ToHaveCountAsync(0);
    }

    private static async Task<bool> IsStoredAsFavoriteAsync(TestDataSeeder seeder, Category category)
        => (await seeder.Services.GetRequiredService<ICategoryManager>().GetAllCategoriesAsync())
            .Single(stored => stored.Id == category.Id).IsFavorite;
}
