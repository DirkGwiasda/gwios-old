using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Tests.TestInfrastructure;

namespace GwiOS.Core.Tests.Haushaltsbuch.Infrastructure.BookingTemplatePostgresRepository;

/// <summary>
/// Covers <c>BookingTemplatePostgresRepository.InsertAsync</c> against the PostgreSQL test database.
/// </summary>
[Collection(PostgresTestDatabaseCollection.Name)]
public sealed class InsertAsyncTests(PostgresTestDatabase database) : IAsyncLifetime
{
    private readonly IBookingTemplateRepository _bookingTemplateRepository = database.BookingTemplateRepository;
    private readonly TestBookingTemplates _testBookingTemplates =
        new(database.BookingTemplateRepository, database.CategoryRepository);

    [Fact]
    public async Task StoresAllPropertiesOfTheBookingTemplate()
    {
        Category category = await _testBookingTemplates.CreateStoredCategoryAsync();
        BookingTemplate bookingTemplate = _testBookingTemplates.Create(category, position: 4);

        await _bookingTemplateRepository.InsertAsync(bookingTemplate, TestContext.Current.CancellationToken);

        BookingTemplate? storedTemplate = await _testBookingTemplates.FindStoredAsync(bookingTemplate.Id);
        Assert.NotNull(storedTemplate);
        Assert.Equal(bookingTemplate.Description, storedTemplate.Description);
        Assert.Equal(category.Id, storedTemplate.CategoryId);
        Assert.Equal(4, storedTemplate.Position);
    }

    public ValueTask InitializeAsync()
        => ValueTask.CompletedTask;

    public ValueTask DisposeAsync()
        => _testBookingTemplates.DisposeAsync();
}
