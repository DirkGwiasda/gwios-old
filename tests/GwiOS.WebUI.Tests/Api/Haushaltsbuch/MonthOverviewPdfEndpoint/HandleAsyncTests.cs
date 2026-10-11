using GwiOS.Core.CrossCutting.Logging.Domain.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.MonthOverview.Contracts.Models;
using GwiOS.WebUI.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using MonthOverviewPdfEndpointUnderTest = GwiOS.WebUI.Api.Haushaltsbuch.MonthOverviewPdfEndpoint;

namespace GwiOS.WebUI.Tests.Api.Haushaltsbuch.MonthOverviewPdfEndpoint;

/// <summary>
/// Covers <c>MonthOverviewPdfEndpoint.HandleAsync</c>.
/// </summary>
public sealed class HandleAsyncTests
{
    private readonly MonthOverviewWorkflowFake _workflow = new();
    private readonly MonthOverviewPdfRendererFake _renderer = new();
    private readonly TimeProviderFake _timeProvider = new() { UtcNow = new DateTimeOffset(2026, 11, 3, 12, 15, 0, TimeSpan.Zero) };
    private readonly LoggerFake<MonthOverviewPdfEndpointUnderTest> _logger = new();
    private readonly MonthOverviewPdfEndpointUnderTest _endpoint;

    public HandleAsyncTests()
    {
        _timeProvider.UseLocalOffset(TimeSpan.FromHours(1));
        _endpoint = new MonthOverviewPdfEndpointUnderTest(_workflow, _renderer, _timeProvider, _logger);
    }

    [Fact]
    public async Task CreatesTheOverviewOfTheGivenMonth()
    {
        await _endpoint.HandleAsync("2026-10", Xunit.TestContext.Current.CancellationToken);

        Assert.Equal([new DateOnly(2026, 10, 1)], _workflow.Requests);
    }

    [Fact]
    public async Task RendersTheOverviewWithTheCurrentLocalTimeAsCreationTime()
    {
        MonthOverviewData overview = TestMonthOverviews.CreateOctober();
        _workflow.Overview = overview;

        await _endpoint.HandleAsync("2026-10", Xunit.TestContext.Current.CancellationToken);

        (MonthOverviewData renderedOverview, DateTimeOffset createdAt) = Assert.Single(_renderer.Renderings);
        Assert.Same(overview, renderedOverview);
        Assert.Equal(new DateTimeOffset(2026, 11, 3, 13, 15, 0, TimeSpan.FromHours(1)), createdAt);
    }

    [Fact]
    public async Task SendsThePdfAsDownloadNamedAfterTheMonth()
    {
        _renderer.Pdf = [1, 2, 3];

        IResult result = await _endpoint.HandleAsync("2026-10", Xunit.TestContext.Current.CancellationToken);

        FileContentHttpResult file = Assert.IsType<FileContentHttpResult>(result);
        Assert.Equal("monatsuebersicht-2026-10.pdf", file.FileDownloadName);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal([1, 2, 3], file.FileContents.ToArray());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("10.2026")]
    [InlineData("2026-13")]
    public async Task RejectsAMissingOrMalformedMonthWithoutCreatingAnything(string? month)
    {
        IResult result = await _endpoint.HandleAsync(month, Xunit.TestContext.Current.CancellationToken);

        Assert.IsType<BadRequest<string>>(result);
        Assert.Empty(_workflow.Requests);
        Assert.Empty(_renderer.Renderings);
    }

    [Fact]
    public async Task LogsTheDownloadWithTheMonth()
    {
        await _endpoint.HandleAsync("2026-10", Xunit.TestContext.Current.CancellationToken);

        LogEntry logEntry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Information, logEntry.LogLevel);
        Assert.Equal("2026-10", logEntry.ContextData["Month"]);
    }
}
