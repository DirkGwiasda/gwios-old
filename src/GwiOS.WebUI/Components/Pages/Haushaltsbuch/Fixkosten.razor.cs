using System.Globalization;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Exceptions;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.HaushaltsbuchOverview.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.HaushaltsbuchOverview.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.RecurringItemContinuation.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.RecurringItemContinuation.Contracts.Models;
using GwiOS.WebUI.StatusMessages.Contracts;
using GwiOS.WebUI.StatusMessages.Contracts.Models;
using Microsoft.AspNetCore.Components;

namespace GwiOS.WebUI.Components.Pages.Haushaltsbuch;

/// <summary>
/// Fixkosten tab of the Haushaltsbuch: the recurring income and expense items that have not ended yet, created,
/// changed in a dialog, continued with changed values from a given day on, activated/deactivated and deleted.
/// </summary>
public partial class Fixkosten
{
    private static readonly string[] TabItems = ["Wiederkehrende Ausgaben", "Wiederkehrende Einnahmen"];
    private static readonly string[] MonthNames =
    [
        "Januar", "Februar", "März", "April", "Mai", "Juni",
        "Juli", "August", "September", "Oktober", "November", "Dezember"
    ];
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    private List<RecurringItem> _recurringItems = [];
    private List<Category> _categories = [];
    private CategoryType _selectedType = CategoryType.Expense;

    private RecurringItem? _editedItem;
    private string _editName = string.Empty;
    private Guid? _editCategoryId;
    private CategoryType _editType;
    private decimal _editAmount;
    private RecurringInterval _editInterval = RecurringInterval.Monthly;
    private int _editDueDay = 1;
    private int _editStartMonth;
    private bool _editActive = true;
    private DateOnly? _continueDate;
    private string? _continueError;
    private List<BookingRescheduling>? _pendingReschedulings;
    private string? _nameError;
    private string? _amountError;
    private bool _isSaving;

    [Inject]
    private IRecurringItemManager RecurringItemManager { get; set; } = default!;

    [Inject]
    private IHaushaltsbuchOverviewWorkflow OverviewWorkflow { get; set; } = default!;

    [Inject]
    private IRecurringItemContinuationWorkflow ContinuationWorkflow { get; set; } = default!;

    [Inject]
    private IStatusMessageService StatusMessages { get; set; } = default!;

    [Inject]
    private TimeProvider TimeProvider { get; set; } = default!;

    private string SelectedTabItem
        => _selectedType == CategoryType.Income ? "Wiederkehrende Einnahmen" : "Wiederkehrende Ausgaben";

    private DateOnly Today
        => DateOnly.FromDateTime(TimeProvider.GetLocalNow().DateTime);

    // Ended items, e.g. predecessors of a continued item, stay stored but are no longer listed.
    private List<RecurringItem> FilteredItems
        => [.. _recurringItems.Where(item => (item.Type == _selectedType) && !(item.ValidUntil < Today))];

    private decimal ActiveMonthlyEquivalentSum
        => FilteredItems
            .Where(item => item.Active && RecurringItemManager.IsValidOn(item, Today))
            .Sum(GetMonthlyEquivalent);

    // Only an item without an end can be continued; one with an end has been continued already.
    private bool CanContinue
        => !IsNewItem && (_editedItem?.ValidUntil is null);

    private bool IsNewItem
        => (_editedItem is not null) && !_recurringItems.Any(item => item.Id == _editedItem.Id);

    private string DialogTitle
        => IsNewItem ? "Neuer wiederkehrender Posten" : "Wiederkehrenden Posten bearbeiten";

    protected override async Task OnInitializedAsync()
        => await LoadAsync();

    private void SelectTab(string item)
        => _selectedType = item == "Wiederkehrende Einnahmen" ? CategoryType.Income : CategoryType.Expense;

    private void SelectEditType(string item)
    {
        _editType = item == "Fixeinnahme" ? CategoryType.Income : CategoryType.Expense;
        _editCategoryId = null;
    }

    private void SelectEditActive(string item)
        => _editActive = item == "Aktiv";

    private void OpenNewItem()
    {
        _editedItem = new RecurringItem
        {
            Name = string.Empty, Type = _selectedType, CategoryId = Guid.Empty, Amount = 0,
            Interval = RecurringInterval.Monthly, DueDay = 1, StartMonth = 0, Active = true
        };
        _editName = string.Empty;
        _editCategoryId = null;
        _editType = _selectedType;
        _editAmount = 0;
        _editInterval = RecurringInterval.Monthly;
        _editDueDay = 1;
        _editStartMonth = 0;
        _editActive = true;
        _nameError = null;
        _amountError = null;
    }

    private void OpenEditItem(RecurringItem item)
    {
        _editedItem = item;
        _editName = item.Name;
        _editCategoryId = item.CategoryId;
        _editType = item.Type;
        _editAmount = item.Amount;
        _editInterval = item.Interval;
        _editDueDay = item.DueDay;
        _editStartMonth = item.StartMonth;
        _editActive = item.Active;
        _nameError = null;
        _amountError = null;
        _continueDate = Today.AddMonths(1).AddDays(1 - Today.Day);
        _continueError = null;
    }

    private void CloseDialog()
    {
        _editedItem = null;
        _pendingReschedulings = null;
    }

    private async Task SaveItemAsync()
    {
        if ((_editedItem is null) || _isSaving || !ValidateEditFields())
        {
            return;
        }

        bool isNew = IsNewItem;
        RecurringItem item = CreateItemFromEditFields(_editedItem.Id);
        item.PredecessorId = _editedItem.PredecessorId;
        item.ValidFrom = _editedItem.ValidFrom;
        item.ValidUntil = _editedItem.ValidUntil;

        _isSaving = true;
        try
        {
            if (isNew)
            {
                await RecurringItemManager.CreateRecurringItemAsync(item);
                StatusMessages.Publish(StatusLevel.Neutral, $"Posten „{item.Name}“ angelegt");
            }
            else
            {
                await RecurringItemManager.UpdateRecurringItemAsync(item);
                StatusMessages.Publish(StatusLevel.Success, $"Posten „{item.Name}“ gespeichert");
            }
        }
        catch (RecurringItemValidationException)
        {
            StatusMessages.Publish(StatusLevel.Error, "Posten konnte nicht gespeichert werden – Eingaben prüfen");
            return;
        }
        finally
        {
            _isSaving = false;
        }

        CloseDialog();
        await LoadAsync();
    }

    private async Task ContinueItemAsync()
    {
        if ((_editedItem is null) || _isSaving || !ValidateEditFields())
        {
            return;
        }

        if (_continueDate is not DateOnly effectiveDate)
        {
            _continueError = "Bitte ein Datum angeben.";
            return;
        }

        List<BookingRescheduling> reschedulings = await ContinuationWorkflow.GetBookingReschedulingsAsync(
            _editedItem.Id,
            effectiveDate,
            CreateItemFromEditFields(Guid.CreateVersion7()));
        if (reschedulings.Count > 0)
        {
            // Changing bookings' dates or deleting them is left to the user to decide.
            _pendingReschedulings = reschedulings;
            return;
        }

        await ExecuteContinuationAsync(effectiveDate, BookingDateHandling.KeepDates);
    }

    private void CancelRescheduling()
        => _pendingReschedulings = null;

    private async Task ConfirmReschedulingAsync(BookingDateHandling bookingDateHandling)
    {
        if (_continueDate is DateOnly effectiveDate)
        {
            await ExecuteContinuationAsync(effectiveDate, bookingDateHandling);
        }
    }

    private async Task ExecuteContinuationAsync(DateOnly effectiveDate, BookingDateHandling bookingDateHandling)
    {
        if ((_editedItem is null) || _isSaving)
        {
            return;
        }

        RecurringItem successor = CreateItemFromEditFields(Guid.CreateVersion7());
        _isSaving = true;
        try
        {
            RecurringItemContinuationResult result = await ContinuationWorkflow.ContinueAsync(
                _editedItem.Id,
                effectiveDate,
                successor,
                bookingDateHandling);
            StatusMessages.Publish(StatusLevel.Success, FormatContinuationMessage(result, effectiveDate));
        }
        catch (RecurringItemNotContinuableException)
        {
            _pendingReschedulings = null;
            _continueError = _editedItem.ValidFrom is DateOnly validFrom
                ? $"Das Datum muss nach dem {validFrom.ToString("dd.MM.yyyy", GermanCulture)} liegen."
                : "Der Posten kann nicht fortgeführt werden.";
            return;
        }
        catch (RecurringItemValidationException)
        {
            _pendingReschedulings = null;
            StatusMessages.Publish(StatusLevel.Error, "Posten konnte nicht fortgeführt werden – Eingaben prüfen");
            return;
        }
        finally
        {
            _isSaving = false;
        }

        CloseDialog();
        await LoadAsync();
    }

    private static string FormatContinuationMessage(RecurringItemContinuationResult result, DateOnly effectiveDate)
    {
        List<string> parts =
        [
            $"Posten „{result.Successor.Name}“ ab {effectiveDate.ToString("dd.MM.yyyy", GermanCulture)} fortgeführt"
        ];
        AddCountPart(parts, result.AdjustedBookingCount, "angepasst");
        AddCountPart(parts, result.MovedBookingCount, "verschoben");
        AddCountPart(parts, result.DeletedBookingCount, "gelöscht");
        return string.Join(", ", parts);
    }

    private static void AddCountPart(List<string> parts, int count, string verb)
    {
        if (count > 0)
        {
            parts.Add($"{count} {(count == 1 ? "Buchung" : "Buchungen")} {verb}");
        }
    }

    private static string FormatReschedulingTarget(BookingRescheduling rescheduling)
        => rescheduling.NewDate is DateOnly newDate
            ? $"→ {newDate.ToString("dd.MM.yyyy", GermanCulture)}"
            : "wird gelöscht";

    private bool ValidateEditFields()
    {
        _nameError = string.IsNullOrWhiteSpace(_editName) ? "Die Bezeichnung ist ein Pflichtfeld." : null;
        _amountError = _editAmount <= 0 ? "Bitte einen Betrag größer 0 angeben." : null;
        return (_nameError is null) && (_amountError is null);
    }

    private RecurringItem CreateItemFromEditFields(Guid id)
        => new()
        {
            Id = id,
            Name = _editName.Trim(),
            Type = _editType,
            CategoryId = _editCategoryId ?? Guid.Empty,
            Amount = _editAmount,
            Interval = _editInterval,
            DueDay = Math.Clamp(_editDueDay, 1, 28),
            StartMonth = _editStartMonth,
            Active = _editActive
        };

    private async Task ToggleActiveAsync(RecurringItem item)
    {
        RecurringItem changed = new()
        {
            Id = item.Id, Name = item.Name, Type = item.Type, CategoryId = item.CategoryId, Amount = item.Amount,
            Interval = item.Interval, DueDay = item.DueDay, StartMonth = item.StartMonth, Active = !item.Active,
            PredecessorId = item.PredecessorId, ValidFrom = item.ValidFrom, ValidUntil = item.ValidUntil
        };
        await RecurringItemManager.UpdateRecurringItemAsync(changed);
        StatusMessages.Publish(
            changed.Active ? StatusLevel.Success : StatusLevel.Warning,
            $"Posten „{item.Name}“ {(changed.Active ? "aktiviert" : "deaktiviert")}");
        await LoadAsync();
    }

    private async Task DeleteItemAsync(RecurringItem item)
    {
        await RecurringItemManager.DeleteRecurringItemAsync(item.Id);
        StatusMessages.Publish(StatusLevel.Warning, $"Posten „{item.Name}“ gelöscht");
        await LoadAsync();
    }

    private decimal GetMonthlyEquivalent(RecurringItem item)
        => item.Interval switch
        {
            RecurringInterval.Weekly => item.Amount * 52 / 12,
            RecurringInterval.Monthly => item.Amount,
            RecurringInterval.Quarterly => item.Amount / 3,
            RecurringInterval.Yearly => item.Amount / 12,
            _ => item.Amount
        };

    // Shows a pending start or a coming end; an item already in effect without an end needs no hint.
    private string? GetValidityHint(RecurringItem item)
        => item.ValidFrom > Today
            ? $"ab {item.ValidFrom.Value.ToString("dd.MM.yyyy", GermanCulture)}"
            : item.ValidUntil is DateOnly validUntil
                ? $"bis {validUntil.ToString("dd.MM.yyyy", GermanCulture)}"
                : null;

    private static string GetIntervalLabel(RecurringInterval interval)
        => interval switch
        {
            RecurringInterval.Weekly => "Wöchentlich",
            RecurringInterval.Monthly => "Monatlich",
            RecurringInterval.Quarterly => "Quartalsweise",
            RecurringInterval.Yearly => "Jährlich",
            _ => interval.ToString()
        };

    private static string GetAmountClass(CategoryType type)
        => type == CategoryType.Income ? "gwios-amount gwios-amount--success" : "gwios-amount gwios-amount--danger";

    private static string FormatAmount(decimal value)
        => value.ToString("C", GermanCulture);

    // The Fixkosten tab does not need any bookings, so it asks the overview workflow for the smallest possible range.
    private async Task LoadAsync()
    {
        HaushaltsbuchOverviewData overview = await OverviewWorkflow.GetOverviewAsync(Today, Today);
        _recurringItems = overview.RecurringItems;
        _categories = overview.Categories;
    }
}
