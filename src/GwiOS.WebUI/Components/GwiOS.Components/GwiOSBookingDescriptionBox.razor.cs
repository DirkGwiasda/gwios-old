using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace GwiOS.WebUI.Components.GwiOS.Components;

/// <summary>
/// Description field of the booking entry that suggests descriptions of earlier bookings while typing. From two
/// characters on, a list below the field shows up to eight matching combinations of description and category: first
/// those where a word starts with the typed text, then those that merely contain it, each group ordered by how often
/// and how recently the combination was used. Picking a suggestion, by click or with the arrow keys and Enter, takes
/// over its description and reports the suggestion, so that the caller can take over the category as well. Escape
/// closes the list without closing a surrounding dialog; leaving the field closes it too. Like
/// <see cref="GwiOSTextBox"/>, the text is trimmed when the user leaves the field.
/// </summary>
public partial class GwiOSBookingDescriptionBox
{
    private const int MinQueryLength = 2;
    private const int MaxSuggestionCount = 8;

    private readonly string _listId = $"gwios-description-box-list-{Guid.NewGuid():N}";
    private List<BookingSuggestion> _matches = [];
    private bool _isListOpen;
    private int _highlightedIndex = -1;

    /// <summary>
    /// The <c>id</c> of the input element, so that a label can refer to it.
    /// </summary>
    [Parameter, EditorRequired]
    public string InputId { get; set; } = string.Empty;

    /// <summary>
    /// The current text. Never <c>null</c>; empty if nothing is entered.
    /// </summary>
    [Parameter]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Raised with the new text on every change, including when a suggestion is picked, so the value can be bound
    /// with <c>@bind-Value</c>.
    /// </summary>
    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    /// <summary>
    /// The combinations of description and category to suggest from, e.g. from
    /// <c>IBookingManager.GetBookingSuggestionsAsync</c>. Suggestions whose category is not among
    /// <see cref="Categories"/> are left out.
    /// </summary>
    [Parameter]
    public IReadOnlyList<BookingSuggestion> Suggestions { get; set; } = [];

    /// <summary>
    /// All categories, to show the category path of each suggestion.
    /// </summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<Category> Categories { get; set; } = [];

    /// <summary>
    /// Raised with the picked suggestion, after <see cref="ValueChanged"/> reported its description.
    /// </summary>
    [Parameter]
    public EventCallback<BookingSuggestion> OnSuggestionSelected { get; set; }

    /// <summary>
    /// Grey text shown while the field is empty.
    /// </summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>
    /// Maximum number of characters the user can enter, or <c>null</c> for no limit.
    /// </summary>
    [Parameter]
    public int? MaxLength { get; set; }

    private bool IsListShown
        => _isListOpen && (_matches.Count > 0);

    /// <inheritdoc />
    protected override void OnParametersSet()
        => UpdateMatches();

    private string OptionId(int index)
        => $"{_listId}-{index}";

    private async Task OnInputAsync(ChangeEventArgs eventArgs)
    {
        Value = eventArgs.Value?.ToString() ?? string.Empty;
        _isListOpen = true;
        _highlightedIndex = -1;
        UpdateMatches();
        await ValueChanged.InvokeAsync(Value);
    }

    // The change event fires when the user leaves the field after changing it.
    private async Task OnChangeAsync(ChangeEventArgs eventArgs)
    {
        Value = (eventArgs.Value?.ToString() ?? string.Empty).Trim();
        await ValueChanged.InvokeAsync(Value);
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs eventArgs)
    {
        switch (eventArgs.Key)
        {
            case "ArrowDown" when _matches.Count > 0:
                _isListOpen = true;
                _highlightedIndex = Math.Min(_highlightedIndex + 1, _matches.Count - 1);
                break;
            case "ArrowUp" when IsListShown:
                _highlightedIndex = Math.Max(_highlightedIndex - 1, -1);
                break;
            case "Enter" when IsListShown && (_highlightedIndex >= 0):
                await SelectAsync(_matches[_highlightedIndex]);
                break;
            case "Escape":
                CloseList();
                break;
        }
    }

    private void CloseList()
    {
        _isListOpen = false;
        _highlightedIndex = -1;
    }

    private async Task SelectAsync(BookingSuggestion suggestion)
    {
        CloseList();
        Value = suggestion.Description;
        await ValueChanged.InvokeAsync(Value);
        await OnSuggestionSelected.InvokeAsync(suggestion);
    }

    private void UpdateMatches()
    {
        string query = Value.Trim();
        if (query.Length < MinQueryLength)
        {
            _matches = [];
            return;
        }

        HashSet<Guid> categoryIds = [.. Categories.Select(category => category.Id)];
        _matches = [.. Suggestions
            .Where(suggestion => categoryIds.Contains(suggestion.CategoryId))
            .Select(suggestion => (Suggestion: suggestion, Rank: RankMatch(suggestion.Description, query)))
            .Where(match => match.Rank is not null)
            .OrderBy(match => match.Rank)
            .ThenByDescending(match => match.Suggestion.UsageCount)
            .ThenByDescending(match => match.Suggestion.LastUsedOn)
            .Select(match => match.Suggestion)
            .Take(MaxSuggestionCount)];
        _highlightedIndex = Math.Min(_highlightedIndex, _matches.Count - 1);
    }

    // 0 if a word of the description starts with the query, 1 if it merely contains it, null if it does not match.
    private static int? RankMatch(string description, string query)
    {
        int? rank = null;
        int index = description.IndexOf(query, StringComparison.CurrentCultureIgnoreCase);
        while (index >= 0)
        {
            if ((index == 0) || !char.IsLetterOrDigit(description[index - 1]))
            {
                return 0;
            }

            rank = 1;
            index = description.IndexOf(query, index + 1, StringComparison.CurrentCultureIgnoreCase);
        }

        return rank;
    }
}
