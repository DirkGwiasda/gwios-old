using GwiOS.Core.CrossCutting.Logging.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Exceptions;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;

namespace GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement;

internal sealed class RecurringItemManager(
    IRecurringItemRepository recurringItemRepository,
    IRecurringItemValidator recurringItemValidator,
    IRecurringItemScheduler recurringItemScheduler,
    ILogger<RecurringItemManager> logger) : IRecurringItemManager
{
    private readonly IRecurringItemRepository _recurringItemRepository = recurringItemRepository;
    private readonly IRecurringItemValidator _recurringItemValidator = recurringItemValidator;
    private readonly IRecurringItemScheduler _recurringItemScheduler = recurringItemScheduler;
    private readonly ILogger<RecurringItemManager> _logger = logger;

    public async Task<RecurringItem> CreateRecurringItemAsync(
        RecurringItem recurringItem,
        CancellationToken cancellationToken = default)
    {
        ThrowIfInvalid(recurringItem);
        await _recurringItemRepository.InsertAsync(recurringItem, cancellationToken);
        _logger.LogInformation("Recurring item created.", CreateRecurringItemIdData(recurringItem.Id));
        return recurringItem;
    }

    public async Task UpdateRecurringItemAsync(RecurringItem recurringItem, CancellationToken cancellationToken = default)
    {
        ThrowIfInvalid(recurringItem);
        await _recurringItemRepository.UpdateAsync(recurringItem, cancellationToken);
        _logger.LogInformation("Recurring item updated.", CreateRecurringItemIdData(recurringItem.Id));
    }

    public async Task<RecurringItem> ContinueRecurringItemAsync(
        Guid predecessorId,
        DateOnly effectiveDate,
        RecurringItem successor,
        CancellationToken cancellationToken = default)
    {
        RecurringItem predecessor = await GetStoredRecurringItemAsync(predecessorId, cancellationToken);
        ThrowIfNotContinuable(predecessor, effectiveDate);

        successor.PredecessorId = predecessor.Id;
        successor.ValidFrom = effectiveDate;
        successor.ValidUntil = null;
        ThrowIfInvalid(successor);
        RecurringItem endedPredecessor = CopyWithValidUntil(predecessor, effectiveDate.AddDays(-1));

        await _recurringItemRepository.ContinueAsync(endedPredecessor, successor, cancellationToken);
        _logger.LogInformation(
            "Recurring item continued.",
            new Dictionary<string, string>
            {
                ["RecurringItemId"] = successor.Id.ToString(),
                ["PredecessorId"] = predecessor.Id.ToString(),
                ["EffectiveDate"] = effectiveDate.ToString("O")
            });
        return successor;
    }

    public async Task<List<RecurringItem>> GetAllRecurringItemsAsync(CancellationToken cancellationToken = default)
        => await _recurringItemRepository.GetAllAsync(cancellationToken);

    public async Task DeleteRecurringItemAsync(Guid recurringItemId, CancellationToken cancellationToken = default)
    {
        await ReopenPredecessorAsync(recurringItemId, cancellationToken);
        await _recurringItemRepository.DeleteAsync(recurringItemId, cancellationToken);
        _logger.LogInformation("Recurring item deleted.", CreateRecurringItemIdData(recurringItemId));
    }

    public IReadOnlyList<DateOnly> GetDueDates(RecurringItem recurringItem, DateOnly month)
        => _recurringItemScheduler.GetDueDates(recurringItem, month);

    public bool IsValidOn(RecurringItem recurringItem, DateOnly date)
        => _recurringItemScheduler.IsValidOn(recurringItem, date);

    private async Task<RecurringItem> GetStoredRecurringItemAsync(
        Guid recurringItemId,
        CancellationToken cancellationToken)
    {
        List<RecurringItem> recurringItems = await _recurringItemRepository.GetAllAsync(cancellationToken);
        return recurringItems.FirstOrDefault(item => item.Id == recurringItemId)
            ?? throw new RecurringItemNotFoundException(recurringItemId);
    }

    // An item with an end has been continued already (or ends anyway); continuing it on or before its start would
    // leave it without a single day in effect.
    private void ThrowIfNotContinuable(RecurringItem predecessor, DateOnly effectiveDate)
    {
        if ((predecessor.ValidUntil is null) && ((predecessor.ValidFrom is null) || (predecessor.ValidFrom < effectiveDate)))
        {
            return;
        }

        _logger.LogWarning(
            "Recurring item cannot be continued.",
            new Dictionary<string, string>
            {
                ["RecurringItemId"] = predecessor.Id.ToString(),
                ["EffectiveDate"] = effectiveDate.ToString("O")
            });
        throw new RecurringItemNotContinuableException(predecessor.Id, effectiveDate);
    }

    // Deleting a successor undoes its continuation: the predecessor it ended is in effect again without an end.
    private async Task ReopenPredecessorAsync(Guid recurringItemId, CancellationToken cancellationToken)
    {
        List<RecurringItem> recurringItems = await _recurringItemRepository.GetAllAsync(cancellationToken);
        RecurringItem? successor = recurringItems.FirstOrDefault(item => item.Id == recurringItemId);
        RecurringItem? predecessor = recurringItems.FirstOrDefault(item => item.Id == successor?.PredecessorId);
        if ((successor?.ValidFrom is not DateOnly validFrom)
            || (predecessor is null)
            || (predecessor.ValidUntil != validFrom.AddDays(-1)))
        {
            return;
        }

        await _recurringItemRepository.UpdateAsync(CopyWithValidUntil(predecessor, null), cancellationToken);
        _logger.LogInformation("Recurring item reopened.", CreateRecurringItemIdData(predecessor.Id));
    }

    private static RecurringItem CopyWithValidUntil(RecurringItem recurringItem, DateOnly? validUntil)
        => new()
        {
            Id = recurringItem.Id,
            Name = recurringItem.Name,
            Type = recurringItem.Type,
            CategoryId = recurringItem.CategoryId,
            Amount = recurringItem.Amount,
            Interval = recurringItem.Interval,
            DueDay = recurringItem.DueDay,
            StartMonth = recurringItem.StartMonth,
            Active = recurringItem.Active,
            PredecessorId = recurringItem.PredecessorId,
            ValidFrom = recurringItem.ValidFrom,
            ValidUntil = validUntil
        };

    private void ThrowIfInvalid(RecurringItem recurringItem)
    {
        if (!_recurringItemValidator.IsValid(recurringItem, out List<string> validationErrors))
        {
            throw new RecurringItemValidationException(recurringItem.Id, validationErrors);
        }
    }

    private static Dictionary<string, string> CreateRecurringItemIdData(Guid recurringItemId)
        => new() { ["RecurringItemId"] = recurringItemId.ToString() };
}
