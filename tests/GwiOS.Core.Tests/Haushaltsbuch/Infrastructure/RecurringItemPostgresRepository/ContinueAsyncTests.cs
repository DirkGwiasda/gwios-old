using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Exceptions;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;
using GwiOS.Core.Tests.TestInfrastructure;

namespace GwiOS.Core.Tests.Haushaltsbuch.Infrastructure.RecurringItemPostgresRepository;

/// <summary>
/// Covers <c>RecurringItemPostgresRepository.ContinueAsync</c> against the PostgreSQL test database.
/// </summary>
[Collection(PostgresTestDatabaseCollection.Name)]
public sealed class ContinueAsyncTests(PostgresTestDatabase database) : IAsyncLifetime
{
    private readonly IRecurringItemRepository _recurringItemRepository = database.RecurringItemRepository;
    private readonly TestRecurringItems _testRecurringItems = new(database.CategoryRepository, database.RecurringItemRepository);

    [Fact]
    public async Task EndsThePredecessorAndStoresTheSuccessor()
    {
        RecurringItem predecessor = await _testRecurringItems.CreateAsync(TestContext.Current.CancellationToken);
        await _recurringItemRepository.InsertAsync(predecessor, TestContext.Current.CancellationToken);
        RecurringItem successor = await CreateSuccessorAsync(predecessor);
        predecessor.ValidUntil = new DateOnly(2026, 10, 31);

        await _recurringItemRepository.ContinueAsync(predecessor, successor, TestContext.Current.CancellationToken);

        RecurringItem? storedPredecessor = await _testRecurringItems.FindStoredAsync(predecessor.Id);
        RecurringItem? storedSuccessor = await _testRecurringItems.FindStoredAsync(successor.Id);
        Assert.NotNull(storedPredecessor);
        Assert.NotNull(storedSuccessor);
        Assert.Equal(new DateOnly(2026, 10, 31), storedPredecessor.ValidUntil);
        Assert.Equal(predecessor.Id, storedSuccessor.PredecessorId);
        Assert.Equal(new DateOnly(2026, 11, 1), storedSuccessor.ValidFrom);
        Assert.Null(storedSuccessor.ValidUntil);
    }

    [Fact]
    public async Task StoresNothingAndThrowsRecurringItemNotFoundException_WhenThePredecessorIsNotStored()
    {
        RecurringItem predecessor = await _testRecurringItems.CreateAsync(TestContext.Current.CancellationToken);
        RecurringItem successor = await CreateSuccessorAsync(predecessor);
        successor.PredecessorId = null;

        await Assert.ThrowsAsync<RecurringItemNotFoundException>(
            () => _recurringItemRepository.ContinueAsync(predecessor, successor, TestContext.Current.CancellationToken));

        Assert.Null(await _testRecurringItems.FindStoredAsync(successor.Id));
    }

    [Fact]
    public async Task ClearsThePredecessorOfTheSuccessor_WhenThePredecessorIsDeleted()
    {
        RecurringItem predecessor = await _testRecurringItems.CreateAsync(TestContext.Current.CancellationToken);
        await _recurringItemRepository.InsertAsync(predecessor, TestContext.Current.CancellationToken);
        RecurringItem successor = await CreateSuccessorAsync(predecessor);
        predecessor.ValidUntil = new DateOnly(2026, 10, 31);
        await _recurringItemRepository.ContinueAsync(predecessor, successor, TestContext.Current.CancellationToken);

        await _recurringItemRepository.DeleteAsync(predecessor.Id, TestContext.Current.CancellationToken);

        RecurringItem? storedSuccessor = await _testRecurringItems.FindStoredAsync(successor.Id);
        Assert.NotNull(storedSuccessor);
        Assert.Null(storedSuccessor.PredecessorId);
    }

    private async Task<RecurringItem> CreateSuccessorAsync(RecurringItem predecessor)
    {
        RecurringItem successor = await _testRecurringItems.CreateAsync(TestContext.Current.CancellationToken);
        successor.Amount = 1190m;
        successor.PredecessorId = predecessor.Id;
        successor.ValidFrom = new DateOnly(2026, 11, 1);
        return successor;
    }

    public ValueTask InitializeAsync()
        => ValueTask.CompletedTask;

    public ValueTask DisposeAsync()
        => _testRecurringItems.DisposeAsync();
}
