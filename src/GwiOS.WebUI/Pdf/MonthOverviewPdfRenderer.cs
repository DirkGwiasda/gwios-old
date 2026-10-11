using System.Globalization;
using GwiOS.Core.CrossCutting.Logging.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.MonthOverview.Contracts.Models;
using GwiOS.WebUI.MonthOverviews;
using GwiOS.WebUI.Pdf.Contracts;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GwiOS.WebUI.Pdf;

internal sealed class MonthOverviewPdfRenderer(
    IQuestPdfConfigurator questPdfConfigurator,
    ILogger<MonthOverviewPdfRenderer> logger) : IMonthOverviewPdfRenderer
{
    private const string FontFamily = "Inter";

    // Print colors as in the forecast PDF: dark text on white, the accent blue for headings.
    private const string TextColor = "#1d2533";
    private const string MutedColor = "#5b6b80";
    private const string AccentColor = "#1f5fa8";
    private const string IncomeColor = "#0a7d50";
    private const string ExpenseColor = "#b3261e";
    private const string RuleColor = "#d5dde8";
    private const string HeaderBackground = "#eef3f9";

    private const float IndentPerLevel = 12;
    private const float AmountColumnWidth = 2.5f;

    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    private readonly IQuestPdfConfigurator _questPdfConfigurator = questPdfConfigurator;
    private readonly ILogger<MonthOverviewPdfRenderer> _logger = logger;

    public byte[] Render(MonthOverviewData overview, DateTimeOffset createdAt)
    {
        _questPdfConfigurator.EnsureConfigured();
        byte[] pdf = Document.Create(document => document.Page(page => ComposePage(page, overview, createdAt)))
            .GeneratePdf();
        _logger.LogDebug(
            "Month overview PDF rendered.",
            new Dictionary<string, string>
            {
                ["Month"] = overview.Month.ToString("O"),
                ["Bytes"] = pdf.Length.ToString(CultureInfo.InvariantCulture)
            });
        return pdf;
    }

    private static void ComposePage(PageDescriptor page, MonthOverviewData overview, DateTimeOffset createdAt)
    {
        int levelCount = MonthOverviewLayout.CountLevels(overview);
        page.Size(PageSizes.A4);
        page.Margin(1.8f, Unit.Centimetre);
        page.DefaultTextStyle(style => style.FontFamily(FontFamily).FontSize(9.5f).FontColor(TextColor));
        page.Header().ShowOnce().Element(header => ComposeHeader(header, overview, createdAt));
        page.Content().PaddingTop(14).Column(column =>
        {
            column.Spacing(18);
            column.Item().Element(section =>
                ComposeSection(section, "Einnahmen", overview.Incomes, overview.IncomeTotal, IncomeColor, levelCount));
            column.Item().Element(section =>
                ComposeSection(section, "Ausgaben", overview.Expenses, overview.ExpenseTotal, ExpenseColor, levelCount));
            column.Item().BorderTop(1.5f).BorderColor(AccentColor).PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text("Saldo").FontSize(11).Bold();
                row.AutoItem().Text(FormatSignedAmount(overview.Balance)).FontSize(11).Bold()
                    .FontColor(overview.Balance >= 0 ? IncomeColor : ExpenseColor);
            });
        });
        page.Footer().Element(ComposeFooter);
    }

    private static void ComposeHeader(IContainer container, MonthOverviewData overview, DateTimeOffset createdAt)
        => container.BorderBottom(1).BorderColor(AccentColor).PaddingBottom(8).Column(column =>
        {
            column.Item().Text($"Monatsübersicht {FormatMonth(overview.Month)}").FontSize(18).Bold().FontColor(AccentColor);
            column.Item().Text(
                $"gebuchte und ausstehende Buchungen · erstellt am {createdAt:dd.MM.yyyy} um {createdAt:HH:mm} Uhr")
                .FontSize(9).FontColor(MutedColor);
        });

    // Stepped form: one amount column per level, the root categories rightmost, each sub-level one column further left.
    private static void ComposeSection(
        IContainer container,
        string title,
        IReadOnlyList<MonthOverviewNode> nodes,
        decimal total,
        string amountColor,
        int levelCount)
        => container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                for (int level = 0; level < levelCount; level++)
                {
                    columns.ConstantColumn(AmountColumnWidth, Unit.Centimetre);
                }
            });
            table.Header(header =>
                header.Cell().ColumnSpan((uint)(levelCount + 1)).Element(HeaderCell).Text(title).FontSize(11).FontColor(amountColor));
            if (nodes.Count == 0)
            {
                table.Cell().ColumnSpan((uint)(levelCount + 1)).Element(BodyCell)
                    .Text($"Keine {title} in diesem Monat.").Italic().FontColor(MutedColor);
            }

            foreach (MonthOverviewRow row in MonthOverviewLayout.CreateRows(nodes))
            {
                bool isRoot = row.Depth == 0;
                table.Cell().Element(BodyCell).PaddingLeft(row.Depth * IndentPerLevel).Text(text =>
                {
                    TextSpanDescriptor name = text.Span(row.Name);
                    if (isRoot)
                    {
                        name.SemiBold();
                    }
                    else if (row.IsDirectShare)
                    {
                        name.Italic().FontColor(MutedColor);
                    }
                });
                for (int level = levelCount - 1; level >= 0; level--)
                {
                    IContainer cell = table.Cell().Element(BodyCell).AlignRight();
                    if (level != row.Depth)
                    {
                        continue;
                    }

                    TextBlockDescriptor amount = cell.Text(FormatAmount(row.Amount))
                        .FontColor(isRoot ? amountColor : MutedColor);
                    if (isRoot)
                    {
                        amount.SemiBold();
                    }
                }
            }

            table.Cell().ColumnSpan((uint)levelCount).Element(TotalCell).Text($"Summe {title}").Bold();
            table.Cell().Element(TotalCell).AlignRight().Text(FormatAmount(total)).Bold().FontColor(amountColor);
        });

    private static IContainer HeaderCell(IContainer container)
        => container.Background(HeaderBackground).PaddingVertical(4).PaddingHorizontal(4).DefaultTextStyle(
            style => style.FontSize(8.5f).SemiBold().FontColor(AccentColor));

    private static IContainer BodyCell(IContainer container)
        => container.BorderBottom(0.5f).BorderColor(RuleColor).PaddingVertical(3).PaddingHorizontal(4);

    private static IContainer TotalCell(IContainer container)
        => container.BorderTop(1).BorderColor(TextColor).PaddingVertical(4).PaddingHorizontal(4);

    private static void ComposeFooter(IContainer container)
        => container.AlignCenter().Text(text =>
        {
            text.DefaultTextStyle(style => style.FontSize(8).FontColor(MutedColor));
            text.Span("GwiOS · Haushaltsbuch · Seite ");
            text.CurrentPageNumber();
            text.Span(" von ");
            text.TotalPages();
        });

    private static string FormatMonth(DateOnly month)
        => month.ToString("MMMM yyyy", GermanCulture);

    private static string FormatAmount(decimal value)
        => value.ToString("C", GermanCulture);

    private static string FormatSignedAmount(decimal value)
        => (value >= 0 ? "+ " : "– ") + Math.Abs(value).ToString("C", GermanCulture);
}
