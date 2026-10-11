using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Tests.Haushaltsbuch.Infrastructure.CategoryPostgresRepository;

namespace GwiOS.Core.Tests.Haushaltsbuch.Infrastructure.BookingTemplatePostgresRepository;

/// <summary>
/// Creates booking templates for the tests against the PostgreSQL test database, each with its own stored category
/// (the foreign key requires one), and deletes every template and category it created when it is disposed. The
/// database may contain templates of other tests, so tests only look at the templates they created themselves.
/// </summary>
public sealed class TestBookingTemplates(
    IBookingTemplateRepository bookingTemplateRepository,
    ICategoryRepository categoryRepository) : IAsyncDisposable
{
    private readonly IBookingTemplateRepository _bookingTemplateRepository = bookingTemplateRepository;
    private readonly TestCategories _testCategories = new(categoryRepository);
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly List<Guid> _bookingTemplateIds = [];

    /// <summary>
    /// Stores a new root category for templates to point to and returns it. It is deleted on dispose.
    /// </summary>
    public async Task<Category> CreateStoredCategoryAsync()
    {
        Category category = _testCategories.Create();
        await _categoryRepository.InsertAsync(category);
        return category;
    }

    /// <summary>
    /// Returns a new, not yet stored, template with a unique description at the given position, pointing to the given
    /// stored category.
    /// </summary>
    public BookingTemplate Create(Category category, int position = 0)
    {
        BookingTemplate bookingTemplate = new()
        {
            Description = _testCategories.CreateUniqueName(), CategoryId = category.Id, Position = position
        };
        _bookingTemplateIds.Add(bookingTemplate.Id);
        return bookingTemplate;
    }

    /// <summary>
    /// Returns the stored template with the given ID, or <c>null</c> if no such template is stored.
    /// </summary>
    public async Task<BookingTemplate?> FindStoredAsync(Guid bookingTemplateId)
        => (await _bookingTemplateRepository.GetAllAsync())
            .SingleOrDefault(bookingTemplate => bookingTemplate.Id == bookingTemplateId);

    public async ValueTask DisposeAsync()
    {
        foreach (Guid bookingTemplateId in _bookingTemplateIds)
        {
            await _bookingTemplateRepository.DeleteAsync(bookingTemplateId);
        }

        await _testCategories.DisposeAsync();
    }
}
