using System.Globalization;
using GwiOS.Core.CrossCutting.Logging.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.MonthOverview.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.MonthOverview.Contracts.Models;
using GwiOS.WebUI.Api.Haushaltsbuch.Contracts;
using GwiOS.WebUI.Pdf.Contracts;

namespace GwiOS.WebUI.Api.Haushaltsbuch;

internal sealed class MonthOverviewPdfEndpoint(
    IMonthOverviewWorkflow monthOverviewWorkflow,
    IMonthOverviewPdfRenderer monthOverviewPdfRenderer,
    TimeProvider timeProvider,
    ILogger<MonthOverviewPdfEndpoint> logger) : IMonthOverviewPdfEndpoint
{
    private const string PdfContentType = "application/pdf";
    private const string MonthFormat = "yyyy-MM";

    private readonly IMonthOverviewWorkflow _monthOverviewWorkflow = monthOverviewWorkflow;
    private readonly IMonthOverviewPdfRenderer _monthOverviewPdfRenderer = monthOverviewPdfRenderer;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<MonthOverviewPdfEndpoint> _logger = logger;

    public async Task<IResult> HandleAsync(string? month, CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParseExact(month, MonthFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly firstDay))
        {
            _logger.LogWarning("Month overview PDF requested without a valid month.");
            return Results.BadRequest("Der Monat muss im Format JJJJ-MM angegeben werden, z. B. 2026-10.");
        }

        MonthOverviewData overview = await _monthOverviewWorkflow.GetMonthOverviewAsync(firstDay, cancellationToken);
        byte[] pdf = _monthOverviewPdfRenderer.Render(overview, _timeProvider.GetLocalNow());
        _logger.LogInformation(
            "Month overview PDF downloaded.",
            new Dictionary<string, string> { ["Month"] = firstDay.ToString(MonthFormat, CultureInfo.InvariantCulture) });
        return Results.File(pdf, PdfContentType, $"monatsuebersicht-{firstDay.ToString(MonthFormat, CultureInfo.InvariantCulture)}.pdf");
    }
}
