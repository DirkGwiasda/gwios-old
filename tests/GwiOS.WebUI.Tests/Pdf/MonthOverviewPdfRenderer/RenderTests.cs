using System.Text;
using GwiOS.Core.CrossCutting.Logging.Domain.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.MonthOverview.Contracts.Models;
using GwiOS.WebUI.Tests.TestInfrastructure;
using MonthOverviewPdfRendererUnderTest = GwiOS.WebUI.Pdf.MonthOverviewPdfRenderer;
using QuestPdfConfiguratorImplementation = GwiOS.WebUI.Pdf.QuestPdfConfigurator;

namespace GwiOS.WebUI.Tests.Pdf.MonthOverviewPdfRenderer;

/// <summary>
/// Covers <c>MonthOverviewPdfRenderer.Render</c> with the real QuestPDF library and the embedded fonts.
/// </summary>
public sealed class RenderTests
{
    private readonly LoggerFake<MonthOverviewPdfRendererUnderTest> _logger = new();
    private readonly MonthOverviewPdfRendererUnderTest _renderer;
    private readonly DateTimeOffset _createdAt = new(2026, 11, 3, 13, 15, 0, TimeSpan.FromHours(1));

    public RenderTests()
    {
        _renderer = new MonthOverviewPdfRendererUnderTest(
            new QuestPdfConfiguratorImplementation(new LoggerFake<QuestPdfConfiguratorImplementation>()),
            _logger);
    }

    [Fact]
    public void ReturnsAPdfDocumentWithTheInterFont()
    {
        byte[] pdf = _renderer.Render(TestMonthOverviews.CreateOctober(), _createdAt);

        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
        Assert.Contains("Inter-Regular", Encoding.Latin1.GetString(pdf));
    }

    [Fact]
    public void RendersAMonthWithoutBookings()
    {
        byte[] pdf = _renderer.Render(TestMonthOverviews.CreateEmpty(new DateOnly(2026, 10, 1)), _createdAt);

        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public void RendersAnOverviewLongerThanOnePage()
    {
        List<MonthOverviewNode> manyCategories = [.. Enumerable.Range(1, 120).Select(number => TestMonthOverviews.CreateNode(
            new Category { Name = $"Kategorie {number}", Type = CategoryType.Expense }, number, number, 1, []))];
        MonthOverviewData overview = new()
        {
            Month = new DateOnly(2026, 10, 1), Incomes = [], Expenses = manyCategories, IncomeTotal = 0,
            ExpenseTotal = manyCategories.Sum(node => node.TotalAmount), Balance = -manyCategories.Sum(node => node.TotalAmount)
        };

        byte[] pdf = _renderer.Render(overview, _createdAt);

        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public void LogsTheMonthAndTheSize()
    {
        byte[] pdf = _renderer.Render(TestMonthOverviews.CreateOctober(), _createdAt);

        LogEntry logEntry = Assert.Single(_logger.Entries, entry => entry.Message == "Month overview PDF rendered.");
        Assert.Equal(LogLevel.Debug, logEntry.LogLevel);
        Assert.Equal("2026-10-01", logEntry.ContextData["Month"]);
        Assert.Equal(pdf.Length.ToString(), logEntry.ContextData["Bytes"]);
    }
}
