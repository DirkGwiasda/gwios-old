using GwiOS.Core.Haushaltsbuch.Domain.Workflows.MonthOverview.Contracts.Models;
using GwiOS.WebUI.Pdf.Contracts;

namespace GwiOS.WebUI.Tests.TestInfrastructure;

/// <summary>
/// Stands in for <see cref="IMonthOverviewPdfRenderer"/>: returns <see cref="Pdf"/> and records what it was asked to
/// render.
/// </summary>
public sealed class MonthOverviewPdfRendererFake : IMonthOverviewPdfRenderer
{
    /// <summary>
    /// The bytes every rendering returns.
    /// </summary>
    public byte[] Pdf { get; set; } = "%PDF-1.4"u8.ToArray();

    /// <summary>
    /// The overviews and creation times rendered, in call order.
    /// </summary>
    public List<(MonthOverviewData Overview, DateTimeOffset CreatedAt)> Renderings { get; } = [];

    public byte[] Render(MonthOverviewData overview, DateTimeOffset createdAt)
    {
        Renderings.Add((overview, createdAt));
        return Pdf;
    }
}
