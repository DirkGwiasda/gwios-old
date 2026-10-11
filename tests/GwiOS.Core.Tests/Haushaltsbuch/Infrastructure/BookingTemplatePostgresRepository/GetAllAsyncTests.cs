using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Tests.TestInfrastructure;

namespace GwiOS.Core.Tests.Haushaltsbuch.Infrastructure.BookingTemplatePostgresRepository;

/// <summary>
/// Covers <c>BookingTemplatePostgresRepository.GetAllAsync</c> against the PostgreSQL test database.
/// </summary>
[Collection(PostgresTestDatabaseCollection.Name)]
public sealed class GetAllAsyncTests(PostgresTestDatabase database) : IAsyncLifetime
{
    private readonly IBookingTemplateRepository _bookingTemplateRepository = database.BookingTemplateRepository;
    private readonly TestBookingTemplates _testBookingTemplates =
        new(database.BookingTemplateRepository, database.CategoryRepository);

    [Fact]
    public async Task ReturnsTheTemplatesOrderedByPosition()
    {
        Category category = await _testBookingTemplates.CreateStoredCategoryAsync();
        BookingTemplate later = _testBookingTemplates.Create(category, position: 900_002);
        BookingTemplate earlier = _testBookingTemplates.Create(category, position: 900_001);
        await _bookingTemplateRepository.InsertAsync(later, TestContext.Current.CancellationToken);
        await _bookingTemplateRepository.InsertAsync(earlier, TestContext.Current.CancellationToken);

        List<BookingTemplate> bookingTemplates =
            await _bookingTemplateRepository.GetAllAsync(TestContext.Current.CancellationToken);

        List<Guid> ownIds = [.. bookingTemplates
            .Where(bookingTemplate => (bookingTemplate.Id == earlier.Id) || (bookingTemplate.Id == later.Id))
            .Select(bookingTemplate => bookingTemplate.Id)];
        Assert.Equal([earlier.Id, later.Id], ownIds);
    }

    public ValueTask InitializeAsync()
        => ValueTask.CompletedTask;

    public ValueTask DisposeAsync()
        => _testBookingTemplates.DisposeAsync();
}
