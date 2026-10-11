using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Tests.CrossCutting.Logging.Contracts;
using GwiOS.Core.Tests.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts;
using GwiOS.Core.Tests.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts;
using MonthOverviewWorkflowUnderTest = GwiOS.Core.Haushaltsbuch.Domain.Workflows.MonthOverview.MonthOverviewWorkflow;

namespace GwiOS.Core.Tests.Haushaltsbuch.Domain.Workflows.MonthOverview.MonthOverviewWorkflow;

/// <summary>
/// Runs a <c>MonthOverviewWorkflow</c> against in-memory managers and a recording logger, and offers a small category
/// tree: "Haushalt" with "Lebensmittel" and, below it, "Bio", plus "Mobilität" for expenses; "Gehalt" for income.
/// </summary>
internal sealed class MonthOverviewWorkflowProbe
{
    public MonthOverviewWorkflowProbe()
    {
        Household = new Category { Name = "Haushalt", Type = CategoryType.Expense, Color = "#ffd056" };
        Groceries = new Category { Name = "Lebensmittel", Type = CategoryType.Expense, ParentId = Household.Id };
        Organic = new Category { Name = "Bio", Type = CategoryType.Expense, ParentId = Groceries.Id };
        Mobility = new Category { Name = "Mobilität", Type = CategoryType.Expense, Color = "#3399ff" };
        Salary = new Category { Name = "Gehalt", Type = CategoryType.Income, Color = "#00ffa3" };
        foreach (Category category in new[] { Household, Groceries, Organic, Mobility, Salary })
        {
            CategoryManager.Add(category);
        }

        Workflow = new MonthOverviewWorkflowUnderTest(BookingManager, CategoryManager, Logger);
    }

    public Category Household { get; }

    public Category Groceries { get; }

    public Category Organic { get; }

    public Category Mobility { get; }

    public Category Salary { get; }

    public BookingManagerFake BookingManager { get; } = new();

    public CategoryManagerFake CategoryManager { get; } = new();

    public LoggerFake<MonthOverviewWorkflowUnderTest> Logger { get; } = new();

    public MonthOverviewWorkflowUnderTest Workflow { get; }

    /// <summary>
    /// Stores a booking of the given category, amount and date.
    /// </summary>
    public void AddBooking(
        Category category,
        decimal amount,
        DateOnly date,
        BookingStatus status = BookingStatus.Booked)
        => BookingManager.Add(new Booking
        {
            Date = date, Type = category.Type, CategoryId = category.Id, Amount = amount, Status = status
        });
}
