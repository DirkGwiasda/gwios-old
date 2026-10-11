namespace GwiOS.WebUI.Api.Haushaltsbuch.Contracts;

/// <summary>
/// Handles <c>GET /api/haushaltsbuch/monatsuebersicht?monat=2026-10</c>: sends the overview of the given month as a
/// PDF download. The PDF is created on request and not stored on the server.
/// </summary>
public interface IMonthOverviewPdfEndpoint
{
    /// <summary>
    /// Creates the overview of the month given as <c>yyyy-MM</c> and returns it as a PDF download named after the
    /// month, e.g. <c>monatsuebersicht-2026-10.pdf</c>. Returns 400 Bad Request if the month is missing or not in this
    /// format.
    /// </summary>
    Task<IResult> HandleAsync(string? month, CancellationToken cancellationToken);
}
