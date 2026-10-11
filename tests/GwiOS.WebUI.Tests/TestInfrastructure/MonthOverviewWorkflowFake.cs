using GwiOS.Core.Haushaltsbuch.Domain.Workflows.MonthOverview.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.MonthOverview.Contracts.Models;

namespace GwiOS.WebUI.Tests.TestInfrastructure;

/// <summary>
/// Stands in for <see cref="IMonthOverviewWorkflow"/>: returns <see cref="Overview"/> and records the requested months.
/// </summary>
public sealed class MonthOverviewWorkflowFake : IMonthOverviewWorkflow
{
    /// <summary>
    /// The overview every call returns.
    /// </summary>
    public MonthOverviewData Overview { get; set; } = TestMonthOverviews.CreateEmpty(new DateOnly(2026, 10, 1));

    /// <summary>
    /// The months requested, in call order.
    /// </summary>
    public List<DateOnly> Requests { get; } = [];

    public Task<MonthOverviewData> GetMonthOverviewAsync(DateOnly month, CancellationToken cancellationToken = default)
    {
        Requests.Add(month);
        return Task.FromResult(Overview);
    }
}
