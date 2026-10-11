using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Tests.TestInfrastructure;

namespace GwiOS.Core.Tests.Haushaltsbuch.Infrastructure.BookingTemplatePostgresRepository;

/// <summary>
/// Covers <c>BookingTemplatePostgresRepository.DeleteAsync</c> against the PostgreSQL test database, and that a
/// template is deleted together with its category.
/// </summary>
[Collection(PostgresTestDatabaseCollection.Name)]
public sealed class DeleteAsyncTests(PostgresTestDatabase database) : IAsyncLifetime
{
    private readonly IBookingTemplateRepository _bookingTemplateRepository = database.BookingTemplateRepository;
    private readonly ICategoryRepository _categoryRepository = database.CategoryRepository;
    private readonly TestBookingTemplates _testBookingTemplates =
        new(database.BookingTemplateRepository, database.CategoryRepository);

    [Fact]
    public async Task DeletesOnlyTheTemplateWithTheGivenId()
    {
        Category category = await _testBookingTemplates.CreateStoredCategoryAsync();
        BookingTemplate templateToDelete = _testBookingTemplates.Create(category);
        BookingTemplate templateToKeep = _testBookingTemplates.Create(category);
        await _bookingTemplateRepository.InsertAsync(templateToDelete, TestContext.Current.CancellationToken);
        await _bookingTemplateRepository.InsertAsync(templateToKeep, TestContext.Current.CancellationToken);

        await _bookingTemplateRepository.DeleteAsync(templateToDelete.Id, TestContext.Current.CancellationToken);

        Assert.Null(await _testBookingTemplates.FindStoredAsync(templateToDelete.Id));
        Assert.NotNull(await _testBookingTemplates.FindStoredAsync(templateToKeep.Id));
    }

    [Fact]
    public async Task Succeeds_WhenTheTemplateIsNotStored()
    {
        Exception? exception = await Record.ExceptionAsync(
            () => _bookingTemplateRepository.DeleteAsync(Guid.CreateVersion7(), TestContext.Current.CancellationToken));

        Assert.Null(exception);
    }

    [Fact]
    public async Task TheTemplateIsDeletedTogetherWithItsCategory()
    {
        Category category = await _testBookingTemplates.CreateStoredCategoryAsync();
        BookingTemplate bookingTemplate = _testBookingTemplates.Create(category);
        await _bookingTemplateRepository.InsertAsync(bookingTemplate, TestContext.Current.CancellationToken);

        await _categoryRepository.DeleteAsync(category.Id, TestContext.Current.CancellationToken);

        Assert.Null(await _testBookingTemplates.FindStoredAsync(bookingTemplate.Id));
    }

    public ValueTask InitializeAsync()
        => ValueTask.CompletedTask;

    public ValueTask DisposeAsync()
        => _testBookingTemplates.DisposeAsync();
}
