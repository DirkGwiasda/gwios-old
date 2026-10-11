using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Exceptions;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Tests.TestInfrastructure;

namespace GwiOS.Core.Tests.Haushaltsbuch.Infrastructure.BookingTemplatePostgresRepository;

/// <summary>
/// Covers <c>BookingTemplatePostgresRepository.UpdatePositionsAsync</c> against the PostgreSQL test database.
/// </summary>
[Collection(PostgresTestDatabaseCollection.Name)]
public sealed class UpdatePositionsAsyncTests(PostgresTestDatabase database) : IAsyncLifetime
{
    private readonly IBookingTemplateRepository _bookingTemplateRepository = database.BookingTemplateRepository;
    private readonly TestBookingTemplates _testBookingTemplates =
        new(database.BookingTemplateRepository, database.CategoryRepository);

    [Fact]
    public async Task StoresThePositionsOfAllGivenTemplates()
    {
        Category category = await _testBookingTemplates.CreateStoredCategoryAsync();
        BookingTemplate first = _testBookingTemplates.Create(category, position: 0);
        BookingTemplate second = _testBookingTemplates.Create(category, position: 1);
        await _bookingTemplateRepository.InsertAsync(first, TestContext.Current.CancellationToken);
        await _bookingTemplateRepository.InsertAsync(second, TestContext.Current.CancellationToken);
        first.Position = 11;
        second.Position = 10;

        await _bookingTemplateRepository.UpdatePositionsAsync([first, second], TestContext.Current.CancellationToken);

        Assert.Equal(11, (await _testBookingTemplates.FindStoredAsync(first.Id))?.Position);
        Assert.Equal(10, (await _testBookingTemplates.FindStoredAsync(second.Id))?.Position);
    }

    [Fact]
    public async Task ChangesNoPositionAndThrowsBookingTemplateNotFoundException_WhenOneTemplateIsNotStored()
    {
        Category category = await _testBookingTemplates.CreateStoredCategoryAsync();
        BookingTemplate stored = _testBookingTemplates.Create(category, position: 0);
        BookingTemplate notStored = _testBookingTemplates.Create(category, position: 1);
        await _bookingTemplateRepository.InsertAsync(stored, TestContext.Current.CancellationToken);
        stored.Position = 5;

        BookingTemplateNotFoundException exception = await Assert.ThrowsAsync<BookingTemplateNotFoundException>(
            () => _bookingTemplateRepository.UpdatePositionsAsync(
                [stored, notStored],
                TestContext.Current.CancellationToken));

        Assert.Equal(notStored.Id, exception.BookingTemplateId);
        Assert.Equal(0, (await _testBookingTemplates.FindStoredAsync(stored.Id))?.Position);
    }

    public ValueTask InitializeAsync()
        => ValueTask.CompletedTask;

    public ValueTask DisposeAsync()
        => _testBookingTemplates.DisposeAsync();
}
