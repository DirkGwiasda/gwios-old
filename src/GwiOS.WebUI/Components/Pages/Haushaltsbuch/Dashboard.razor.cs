using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingTemplateManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.RecurringItemManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.AvailableBalance.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.HaushaltsbuchOverview.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.HaushaltsbuchOverview.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.RecurringBookingGeneration.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.RecurringBookingGeneration.Contracts.Models;
using GwiOS.WebUI.AmountExpressions.Contracts;
using GwiOS.WebUI.Components.GwiOS.Components;
using GwiOS.WebUI.StatusMessages.Contracts;
using GwiOS.WebUI.StatusMessages.Contracts.Models;
using Microsoft.AspNetCore.Components;

namespace GwiOS.WebUI.Components.Pages.Haushaltsbuch;

/// <summary>
/// Dashboard tab of the Haushaltsbuch: the KPI figures and bookings of the selected month, where bookings are
/// created, changed in a dialog and deleted, and where due recurring items are picked in a dialog to become bookings —
/// automatically on the first visit of an unreviewed month, or on demand through the "Fixbuchungen generieren" button.
/// The booking dialog is built for quick entry: favorites fill in description and category, the amount comes first
/// and may be a short calculation, the date is a tap away, several bookings can be entered in a row, and a new booking
/// can be taken back from a bar shown after saving. Favorites are pinned and unpinned in the dialog and in the list.
/// </summary>
public partial class Dashboard
{
    private static readonly string[] MonthNames =
    [
        "Januar", "Februar", "März", "April", "Mai", "Juni",
        "Juli", "August", "September", "Oktober", "November", "Dezember"
    ];
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");
    private static readonly TimeSpan UndoDuration = TimeSpan.FromSeconds(6);
    private static readonly TimeSpan UndoneConfirmationDuration = TimeSpan.FromSeconds(2.5);

    private DateOnly _month;
    private List<Category> _categories = [];
    private List<RecurringItem> _recurringItems = [];
    private List<Booking> _bookings = [];
    private List<BookingTemplate> _bookingTemplates = [];
    private List<ProposedBooking> _proposedBookings = [];
    private decimal _availableBalance;

    private bool _isProposalDialogOpen;
    private bool _isMonthReview;
    private readonly HashSet<ProposedBooking> _selectedProposals = [];

    private Booking? _editedBooking;
    private DateOnly _editDate;
    private CategoryType _editType;
    private Guid? _editCategoryId;
    private string _editDescription = string.Empty;
    private string _editAmountText = string.Empty;
    private BookingStatus _editStatus;
    private string? _amountError;
    private string? _categoryError;
    private string? _favoriteError;
    private string? _flashMessage;
    private bool _isMoreOpen;
    private int _amountFocusRequest;
    private bool _isSaving;

    private UndoBarState? _undoBar;

    [Inject]
    private IBookingManager BookingManager { get; set; } = default!;

    [Inject]
    private IBookingTemplateManager BookingTemplateManager { get; set; } = default!;

    [Inject]
    private IHaushaltsbuchOverviewWorkflow OverviewWorkflow { get; set; } = default!;

    [Inject]
    private IAvailableBalanceWorkflow AvailableBalanceWorkflow { get; set; } = default!;

    [Inject]
    private IRecurringBookingGenerationWorkflow RecurringBookingGenerationWorkflow { get; set; } = default!;

    [Inject]
    private IStatusMessageService StatusMessages { get; set; } = default!;

    [Inject]
    private IAmountExpressionEvaluator AmountExpressionEvaluator { get; set; } = default!;

    [Inject]
    private TimeProvider TimeProvider { get; set; } = default!;

    private DateOnly Today
        => DateOnly.FromDateTime(TimeProvider.GetLocalNow().DateTime);

    private DateOnly FirstDay
        => new(_month.Year, _month.Month, 1);

    private DateOnly LastDay
        => new(_month.Year, _month.Month, DateTime.DaysInMonth(_month.Year, _month.Month));

    private string MonthLabel
        => $"{MonthNames[_month.Month - 1]} {_month.Year}";

    private decimal IncomeBooked
        => SumBookings(CategoryType.Income, BookingStatus.Booked);

    private decimal IncomePending
        => SumBookings(CategoryType.Income, BookingStatus.Pending);

    private decimal ExpenseBooked
        => SumBookings(CategoryType.Expense, BookingStatus.Booked);

    private decimal ExpensePending
        => SumBookings(CategoryType.Expense, BookingStatus.Pending);

    private decimal BookedBalance
        => IncomeBooked - ExpenseBooked;

    private bool IsNewBooking
        => (_editedBooking is not null) && !_bookings.Any(booking => booking.Id == _editedBooking.Id);

    private string DialogTitle
        => IsNewBooking ? "Neue Buchung" : "Buchung bearbeiten";

    private string MoreLabel
        => $"{(_isMoreOpen ? "▾ Weniger" : "▸ Mehr")} · {(_editStatus == BookingStatus.Booked ? "Gebucht" : "Ausstehend")}";

    // The favorite matching the description and category in the form, so picking it or typing the same marks it.
    private Guid? ActiveTemplateId
        => _editCategoryId is Guid categoryId ? FindBookingTemplate(_editDescription, categoryId)?.Id : null;

    private bool IsFormFavorite
        => ActiveTemplateId is not null;

    private string GenerateFixBookingsLabel
        => _proposedBookings.Count == 0
            ? "Fixbuchungen vollständig"
            : $"Fixbuchungen für {MonthNames[_month.Month - 1]} generieren ({_proposedBookings.Count})";

    private string ProposalDialogTitle
        => _isMonthReview ? $"Fixposten für {MonthLabel} prüfen" : $"Fixbuchungen für {MonthLabel} generieren";

    protected override async Task OnInitializedAsync()
    {
        _month = new DateOnly(Today.Year, Today.Month, 1);
        await LoadAsync();
    }

    private async Task ChangeMonthAsync(DateOnly month)
    {
        _month = month;
        await LoadAsync();
    }

    private decimal SumBookings(CategoryType type, BookingStatus status)
        => _bookings.Where(booking => (booking.Type == type) && (booking.Status == status)).Sum(booking => booking.Amount);

    // A booking generated from a recurring item is labeled with that item instead, so it needs no dash.
    private static bool HasDescriptionText(Booking booking)
        => !string.IsNullOrWhiteSpace(booking.Description) || (booking.RecurringItemId is null);

    private static string GetDescription(Booking booking)
        => string.IsNullOrWhiteSpace(booking.Description) ? "—" : booking.Description;

    private static string GetAmountClass(CategoryType type)
        => type == CategoryType.Income ? "gwios-amount gwios-amount--success" : "gwios-amount gwios-amount--danger";

    private static string FormatAmount(decimal value)
        => value.ToString("C", GermanCulture);

    private static string FormatSignedAmount(decimal value)
        => (value >= 0 ? "+ " : "– ") + Math.Abs(value).ToString("C", GermanCulture);

    private void OpenNewBooking()
    {
        _flashMessage = null;
        ResetFormForNewBooking(Today);
    }

    private void OpenEditBooking(Booking booking)
    {
        _editedBooking = booking;
        _editDate = booking.Date;
        _editType = booking.Type;
        _editCategoryId = booking.CategoryId;
        _editDescription = booking.Description ?? string.Empty;
        _editAmountText = AmountExpressionEvaluator.Format(booking.Amount);
        _editStatus = booking.Status;
        _isMoreOpen = false;
        _flashMessage = null;
        ClearFormErrors();
    }

    // Starts an empty form for the next booking; the date is kept, so a series of receipts from one day is quick.
    private void ResetFormForNewBooking(DateOnly date)
    {
        _editedBooking = new Booking
        {
            Date = date, Type = CategoryType.Expense, CategoryId = Guid.Empty, Amount = 0,
            Status = BookingStatus.Booked
        };
        _editDate = date;
        _editType = CategoryType.Expense;
        _editCategoryId = null;
        _editDescription = string.Empty;
        _editAmountText = string.Empty;
        _editStatus = BookingStatus.Booked;
        _isMoreOpen = false;
        _amountFocusRequest++;
        ClearFormErrors();
    }

    private void ClearFormErrors()
    {
        _amountError = null;
        _categoryError = null;
        _favoriteError = null;
    }

    private void CloseDialog()
        => _editedBooking = null;

    private void ApplyTemplate(BookingTemplate template)
    {
        Category? category = _categories.FirstOrDefault(candidate => candidate.Id == template.CategoryId);
        _editDescription = template.Description;
        _editCategoryId = category?.Id;
        _editType = category?.Type ?? _editType;
        ClearFormErrors();
        _amountFocusRequest++;
    }

    private void ChangeDescription(string description)
    {
        _editDescription = description;
        _favoriteError = null;
    }

    private void SelectCategory(Guid? categoryId)
    {
        _editCategoryId = categoryId;
        _categoryError = null;
        _favoriteError = null;
    }

    private void SelectEditStatus(string item)
        => _editStatus = item == "Gebucht" ? BookingStatus.Booked : BookingStatus.Pending;

    private async Task SaveBookingAsync()
    {
        if (_isSaving || !TryCreateBookingFromForm(out Booking? booking))
        {
            return;
        }

        bool isNew = IsNewBooking;
        _isSaving = true;
        try
        {
            if (isNew)
            {
                await BookingManager.CreateBookingAsync(booking);
                StatusMessages.Publish(StatusLevel.Neutral, $"Buchung über {FormatAmount(booking.Amount)} erfasst");
                ShowUndoBar(booking);
            }
            else
            {
                await BookingManager.UpdateBookingAsync(booking);
                StatusMessages.Publish(StatusLevel.Success, $"Buchung über {FormatAmount(booking.Amount)} gespeichert");
            }
        }
        finally
        {
            _isSaving = false;
        }

        CloseDialog();
        await ReloadDataAsync();
    }

    private async Task SaveBookingAndContinueAsync()
    {
        if (_isSaving || !IsNewBooking || !TryCreateBookingFromForm(out Booking? booking))
        {
            return;
        }

        _isSaving = true;
        try
        {
            await BookingManager.CreateBookingAsync(booking);
        }
        finally
        {
            _isSaving = false;
        }

        StatusMessages.Publish(StatusLevel.Neutral, $"Buchung über {FormatAmount(booking.Amount)} erfasst");
        _flashMessage = $"✓ {DescribeBooking(booking)} – bereit für die nächste";
        ResetFormForNewBooking(booking.Date);
        await ReloadDataAsync();
    }

    private bool TryCreateBookingFromForm([NotNullWhen(true)] out Booking? booking)
    {
        booking = null;
        decimal? amount = AmountExpressionEvaluator.Evaluate(_editAmountText).Value;
        _amountError = GetAmountError(amount);
        _categoryError = _editCategoryId is null ? "Bitte eine Kategorie wählen." : null;
        if ((_editedBooking is null) || (amount is not decimal validAmount) || (_amountError is not null)
            || (_editCategoryId is not Guid categoryId))
        {
            return false;
        }

        booking = new Booking
        {
            Id = _editedBooking.Id,
            Date = _editDate,
            Type = _editType,
            CategoryId = categoryId,
            Description = string.IsNullOrWhiteSpace(_editDescription) ? null : _editDescription.Trim(),
            Amount = validAmount,
            Status = _editStatus,
            RecurringItemId = _editedBooking.RecurringItemId
        };
        return true;
    }

    private string? GetAmountError(decimal? amount)
        => (string.IsNullOrWhiteSpace(_editAmountText), amount) switch
        {
            (true, _) => "Bitte einen Betrag eingeben.",
            (false, null) => "Bitte einen gültigen Betrag eingeben, z. B. 12,50 oder 12,50+3,20.",
            (false, <= 0) => "Der Betrag muss größer als 0 sein.",
            _ => null
        };

    // E.g. "Edeka · −47,83 € gebucht".
    private string DescribeBooking(Booking booking)
        => $"{booking.Description ?? "Buchung"} · {(booking.Type == CategoryType.Income ? "+" : "−")}"
            + $"{AmountExpressionEvaluator.Format(booking.Amount)} € "
            + (booking.Status == BookingStatus.Booked ? "gebucht" : "vorgemerkt");

    private void ShowUndoBar(Booking booking)
        => _undoBar = new UndoBarState(DescribeBooking(booking), booking.Id, UndoDuration);

    private async Task UndoLastBookingAsync()
    {
        if (_undoBar?.BookingId is not Guid bookingId)
        {
            return;
        }

        await BookingManager.DeleteBookingAsync(bookingId);
        _undoBar = new UndoBarState("Buchung zurückgenommen", null, UndoneConfirmationDuration);
        StatusMessages.Publish(StatusLevel.Warning, "Buchung zurückgenommen");
        await ReloadDataAsync();
    }

    private async Task DeleteBookingAsync(Booking booking)
    {
        await BookingManager.DeleteBookingAsync(booking.Id);
        StatusMessages.Publish(StatusLevel.Warning, "Buchung gelöscht");
        await ReloadDataAsync();
    }

    private BookingTemplate? FindBookingTemplate(string description, Guid categoryId)
        => _bookingTemplates.FirstOrDefault(template =>
            (template.CategoryId == categoryId)
            && string.Equals(template.Description.Trim(), description.Trim(), StringComparison.OrdinalIgnoreCase));

    private static bool CanBecomeFavorite(Booking booking)
        => !string.IsNullOrWhiteSpace(booking.Description);

    private bool IsFavorite(Booking booking)
        => FindBookingTemplate(booking.Description ?? string.Empty, booking.CategoryId) is not null;

    private async Task ToggleFavoriteFromFormAsync()
    {
        if (string.IsNullOrWhiteSpace(_editDescription) || (_editCategoryId is not Guid categoryId))
        {
            _favoriteError = "Für einen Favoriten braucht es Beschreibung und Kategorie.";
            return;
        }

        _favoriteError = null;
        await ToggleFavoriteAsync(_editDescription.Trim(), categoryId);
    }

    private async Task ToggleFavoriteFromBookingAsync(Booking booking)
        => await ToggleFavoriteAsync(booking.Description?.Trim() ?? string.Empty, booking.CategoryId);

    private async Task ToggleFavoriteAsync(string description, Guid categoryId)
    {
        if (FindBookingTemplate(description, categoryId) is BookingTemplate existingTemplate)
        {
            await BookingTemplateManager.DeleteBookingTemplateAsync(existingTemplate.Id);
            StatusMessages.Publish(StatusLevel.Neutral, $"Favorit „{description}“ entfernt");
        }
        else
        {
            await BookingTemplateManager.CreateBookingTemplateAsync(
                new BookingTemplate { Description = description, CategoryId = categoryId });
            StatusMessages.Publish(StatusLevel.Success, $"„{description}“ als Favorit gespeichert");
        }

        await ReloadDataAsync();
    }

    private void ToggleProposal(ProposedBooking proposal, bool isSelected)
    {
        if (isSelected)
        {
            _selectedProposals.Add(proposal);
        }
        else
        {
            _selectedProposals.Remove(proposal);
        }
    }

    private void OpenFixBookingSelection()
    {
        if (_proposedBookings.Count == 0)
        {
            return;
        }

        OpenProposalDialog();
        _isMonthReview = false;
    }

    private void OpenMonthReview()
    {
        OpenProposalDialog();
        _isMonthReview = true;
    }

    private void OpenProposalDialog()
    {
        _selectedProposals.Clear();
        _selectedProposals.UnionWith(_proposedBookings);
        _isProposalDialogOpen = true;
    }

    private void CloseProposalDialog()
        => _isProposalDialogOpen = false;

    private async Task ConfirmProposalsAsync()
    {
        int confirmedCount = _selectedProposals.Count;
        await RecurringBookingGenerationWorkflow.ConfirmAsync(_month, [.. _selectedProposals]);
        CloseProposalDialog();
        StatusMessages.Publish(StatusLevel.Success, $"{confirmedCount} Fixbuchungen für {MonthLabel} übernommen");
        await ReloadDataAsync();
    }

    private async Task LoadAsync()
    {
        await ReloadDataAsync();

        bool needsReview = await RecurringBookingGenerationWorkflow.NeedsReviewAsync(_month);
        if (needsReview && (_proposedBookings.Count > 0))
        {
            OpenMonthReview();
        }
        else
        {
            CloseProposalDialog();
            if (needsReview)
            {
                // Nothing to propose this month; mark it reviewed without bothering anyone with an empty popup.
                await RecurringBookingGenerationWorkflow.ConfirmAsync(_month, []);
            }
        }
    }

    private async Task ReloadDataAsync()
    {
        HaushaltsbuchOverviewData overview = await OverviewWorkflow.GetOverviewAsync(FirstDay, LastDay);
        _categories = overview.Categories;
        _recurringItems = overview.RecurringItems;
        _bookings = overview.Bookings;
        _bookingTemplates = overview.BookingTemplates;
        _availableBalance = await AvailableBalanceWorkflow.GetAvailableBalanceAsync(_month);
        _proposedBookings = await RecurringBookingGenerationWorkflow.GetProposedBookingsAsync(_month);
    }

    // What the undo bar shows; a new instance restarts the bar with its own duration. BookingId is the booking that
    // "Rückgängig" deletes, or null once it was taken back.
    private sealed class UndoBarState(string text, Guid? bookingId, TimeSpan duration)
    {
        public string Text { get; } = text;

        public Guid? BookingId { get; } = bookingId;

        public bool CanUndo
            => BookingId is not null;

        public TimeSpan Duration { get; } = duration;
    }
}
