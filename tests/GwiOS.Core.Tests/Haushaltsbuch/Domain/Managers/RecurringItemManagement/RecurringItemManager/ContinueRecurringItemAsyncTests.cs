using GwiOS.Core.CrossCutting.Logging.Domain.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Exceptions;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;

namespace GwiOS.Core.Tests.Haushaltsbuch.Domain.Managers.RecurringItemManagement.RecurringItemManager;

/// <summary>
/// Covers <c>RecurringItemManager.ContinueRecurringItemAsync</c>.
/// </summary>
public sealed class ContinueRecurringItemAsyncTests
{
    private static readonly DateOnly EffectiveDate = new(2026, 11, 1);

    private readonly RecurringItemManagerProbe _probe = new();
    private readonly RecurringItem _insurance = CreateInsurance(89.5m);
    private readonly RecurringItem _raisedInsurance = CreateInsurance(94.2m);

    [Fact]
    public async Task EndsThePredecessorOnTheDayBeforeTheEffectiveDate()
    {
        await StoreAsync(_insurance);

        await ContinueAsync(_insurance.Id, EffectiveDate, _raisedInsurance);

        RecurringItem predecessor = await GetStoredAsync(_insurance.Id);
        Assert.Equal(new DateOnly(2026, 10, 31), predecessor.ValidUntil);
        Assert.Equal(89.5m, predecessor.Amount);
    }

    [Fact]
    public async Task StoresTheSuccessorWithThePredecessorAndTheEffectiveDateAndNoEnd()
    {
        await StoreAsync(_insurance);
        _raisedInsurance.ValidUntil = new DateOnly(2027, 1, 1);

        RecurringItem returned = await ContinueAsync(_insurance.Id, EffectiveDate, _raisedInsurance);

        RecurringItem successor = await GetStoredAsync(_raisedInsurance.Id);
        Assert.Same(returned, successor);
        Assert.Equal(_insurance.Id, successor.PredecessorId);
        Assert.Equal(EffectiveDate, successor.ValidFrom);
        Assert.Null(successor.ValidUntil);
        Assert.Equal(94.2m, successor.Amount);
    }

    [Fact]
    public async Task StoresBothChangesTogether()
    {
        await StoreAsync(_insurance);

        await ContinueAsync(_insurance.Id, EffectiveDate, _raisedInsurance);

        Assert.Equal(1, _probe.RecurringItemRepository.ContinueCallCount);
    }

    [Fact]
    public async Task Succeeds_WhenTheEffectiveDateIsAfterTheStartOfThePredecessor()
    {
        _insurance.ValidFrom = new DateOnly(2026, 10, 31);
        await StoreAsync(_insurance);

        await ContinueAsync(_insurance.Id, EffectiveDate, _raisedInsurance);

        Assert.Equal(new DateOnly(2026, 10, 31), (await GetStoredAsync(_insurance.Id)).ValidUntil);
    }

    [Fact]
    public async Task ThrowsRecurringItemNotFoundException_WhenThePredecessorIsNotStored()
    {
        RecurringItemNotFoundException exception = await Assert.ThrowsAsync<RecurringItemNotFoundException>(
            () => ContinueAsync(_insurance.Id, EffectiveDate, _raisedInsurance));

        Assert.Equal(_insurance.Id, exception.RecurringItemId);
    }

    [Fact]
    public async Task ThrowsRecurringItemNotContinuableException_WhenThePredecessorAlreadyHasAnEnd()
    {
        _insurance.ValidUntil = new DateOnly(2026, 12, 31);
        await StoreAsync(_insurance);

        await Assert.ThrowsAsync<RecurringItemNotContinuableException>(
            () => ContinueAsync(_insurance.Id, EffectiveDate, _raisedInsurance));

        Assert.Equal(0, _probe.RecurringItemRepository.ContinueCallCount);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task ThrowsRecurringItemNotContinuableException_WhenThePredecessorDoesNotStartBeforeTheEffectiveDate(
        int predecessorStartDay)
    {
        _insurance.ValidFrom = new DateOnly(2026, 11, predecessorStartDay);
        await StoreAsync(_insurance);

        RecurringItemNotContinuableException exception = await Assert.ThrowsAsync<RecurringItemNotContinuableException>(
            () => ContinueAsync(_insurance.Id, EffectiveDate, _raisedInsurance));

        Assert.Equal(_insurance.Id, exception.RecurringItemId);
        Assert.Equal(EffectiveDate, exception.EffectiveDate);
    }

    [Fact]
    public async Task ThrowsRecurringItemValidationExceptionAndStoresNothing_WhenTheSuccessorIsInvalid()
    {
        await StoreAsync(_insurance);
        _probe.RecurringItemValidator.ValidationErrors = ["The name must not be empty."];

        await Assert.ThrowsAsync<RecurringItemValidationException>(
            () => ContinueAsync(_insurance.Id, EffectiveDate, _raisedInsurance));

        Assert.Equal(0, _probe.RecurringItemRepository.ContinueCallCount);
        Assert.Null((await GetStoredAsync(_insurance.Id)).ValidUntil);
    }

    [Fact]
    public async Task LogsTheContinuationWithBothIdsAndTheEffectiveDate()
    {
        await StoreAsync(_insurance);

        await ContinueAsync(_insurance.Id, EffectiveDate, _raisedInsurance);

        LogEntry logEntry = Assert.Single(_probe.Logger.Entries);
        Assert.Equal(LogLevel.Information, logEntry.LogLevel);
        Assert.Equal("Recurring item continued.", logEntry.Message);
        Assert.Equal(_raisedInsurance.Id.ToString(), logEntry.ContextData["RecurringItemId"]);
        Assert.Equal(_insurance.Id.ToString(), logEntry.ContextData["PredecessorId"]);
        Assert.Equal("2026-11-01", logEntry.ContextData["EffectiveDate"]);
    }

    [Fact]
    public async Task LogsAWarning_WhenThePredecessorCannotBeContinued()
    {
        _insurance.ValidUntil = new DateOnly(2026, 12, 31);
        await StoreAsync(_insurance);

        await Assert.ThrowsAsync<RecurringItemNotContinuableException>(
            () => ContinueAsync(_insurance.Id, EffectiveDate, _raisedInsurance));

        LogEntry logEntry = Assert.Single(_probe.Logger.Entries);
        Assert.Equal(LogLevel.Warning, logEntry.LogLevel);
        Assert.Equal(_insurance.Id.ToString(), logEntry.ContextData["RecurringItemId"]);
    }

    private static RecurringItem CreateInsurance(decimal amount)
        => new()
        {
            Name = "Hausratversicherung",
            Type = CategoryType.Expense,
            CategoryId = Guid.CreateVersion7(),
            Amount = amount,
            Interval = RecurringInterval.Monthly
        };

    private Task StoreAsync(RecurringItem item)
        => _probe.RecurringItemRepository.InsertAsync(item, TestContext.Current.CancellationToken);

    private async Task<RecurringItem> GetStoredAsync(Guid id)
        => (await _probe.RecurringItemRepository.GetAllAsync(TestContext.Current.CancellationToken))
            .Single(item => item.Id == id);

    private Task<RecurringItem> ContinueAsync(Guid predecessorId, DateOnly effectiveDate, RecurringItem successor)
        => _probe.RecurringItemManager.ContinueRecurringItemAsync(
            predecessorId,
            effectiveDate,
            successor,
            TestContext.Current.CancellationToken);
}
