using GwiOS.Core.CrossCutting.Logging.Domain.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Exceptions;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Tests.TestInfrastructure;

namespace GwiOS.Core.Tests.Haushaltsbuch.Infrastructure.BookingTemplatePostgresRepository;

/// <summary>
/// Covers <c>BookingTemplatePostgresRepository.UpdateAsync</c> against the PostgreSQL test database.
/// </summary>
[Collection(PostgresTestDatabaseCollection.Name)]
public sealed class UpdateAsyncTests(PostgresTestDatabase database) : IAsyncLifetime
{
    private readonly PostgresTestDatabase _database = database;
    private readonly IBookingTemplateRepository _bookingTemplateRepository = database.BookingTemplateRepository;
    private readonly TestBookingTemplates _testBookingTemplates =
        new(database.BookingTemplateRepository, database.CategoryRepository);

    [Fact]
    public async Task OverwritesDescriptionAndCategoryButKeepsThePosition()
    {
        Category category = await _testBookingTemplates.CreateStoredCategoryAsync();
        Category otherCategory = await _testBookingTemplates.CreateStoredCategoryAsync();
        BookingTemplate bookingTemplate = _testBookingTemplates.Create(category, position: 3);
        await _bookingTemplateRepository.InsertAsync(bookingTemplate, TestContext.Current.CancellationToken);
        BookingTemplate changedTemplate = new()
        {
            Id = bookingTemplate.Id, Description = $"{bookingTemplate.Description}-changed",
            CategoryId = otherCategory.Id, Position = 9
        };

        await _bookingTemplateRepository.UpdateAsync(changedTemplate, TestContext.Current.CancellationToken);

        BookingTemplate? storedTemplate = await _testBookingTemplates.FindStoredAsync(bookingTemplate.Id);
        Assert.NotNull(storedTemplate);
        Assert.Equal(changedTemplate.Description, storedTemplate.Description);
        Assert.Equal(otherCategory.Id, storedTemplate.CategoryId);
        Assert.Equal(3, storedTemplate.Position);
    }

    [Fact]
    public async Task ThrowsBookingTemplateNotFoundExceptionAndLogsAWarning_WhenTheTemplateIsNotStored()
    {
        Category category = await _testBookingTemplates.CreateStoredCategoryAsync();
        BookingTemplate bookingTemplate = _testBookingTemplates.Create(category);

        BookingTemplateNotFoundException exception = await Assert.ThrowsAsync<BookingTemplateNotFoundException>(
            () => _bookingTemplateRepository.UpdateAsync(bookingTemplate, TestContext.Current.CancellationToken));

        Assert.Equal(bookingTemplate.Id, exception.BookingTemplateId);
        Assert.Contains(
            _database.BookingTemplateRepositoryLogger.Entries,
            logEntry => (logEntry.LogLevel == LogLevel.Warning)
                && (logEntry.ContextData.GetValueOrDefault("BookingTemplateId") == bookingTemplate.Id.ToString()));
    }

    public ValueTask InitializeAsync()
        => ValueTask.CompletedTask;

    public ValueTask DisposeAsync()
        => _testBookingTemplates.DisposeAsync();
}
