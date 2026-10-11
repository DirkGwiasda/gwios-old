using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Tests.TestInfrastructure;

namespace GwiOS.Core.Tests.Haushaltsbuch.Infrastructure.BookingTemplatePostgresRepository;

/// <summary>
/// Covers <c>BookingTemplatePostgresRepository.EnsureStorageCreatedAsync</c> against the PostgreSQL test database.
/// </summary>
[Collection(PostgresTestDatabaseCollection.Name)]
public sealed class EnsureStorageCreatedAsyncTests(PostgresTestDatabase database) : IAsyncLifetime
{
    private readonly IBookingTemplateRepository _bookingTemplateRepository = database.BookingTemplateRepository;
    private readonly TestBookingTemplates _testBookingTemplates =
        new(database.BookingTemplateRepository, database.CategoryRepository);

    [Fact]
    public async Task KeepsExistingBookingTemplates_WhenTheTablesAlreadyExist()
    {
        Category category = await _testBookingTemplates.CreateStoredCategoryAsync();
        BookingTemplate bookingTemplate = _testBookingTemplates.Create(category);
        await _bookingTemplateRepository.InsertAsync(bookingTemplate, TestContext.Current.CancellationToken);

        await _bookingTemplateRepository.EnsureStorageCreatedAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(await _testBookingTemplates.FindStoredAsync(bookingTemplate.Id));
    }

    public ValueTask InitializeAsync()
        => ValueTask.CompletedTask;

    public ValueTask DisposeAsync()
        => _testBookingTemplates.DisposeAsync();
}
