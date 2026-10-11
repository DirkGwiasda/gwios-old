using GwiOS.Core.CrossCutting.Logging.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.BookingManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Managers.CategoryManagement.Contracts.Models;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.MonthOverview.Contracts;
using GwiOS.Core.Haushaltsbuch.Domain.Workflows.MonthOverview.Contracts.Models;

namespace GwiOS.Core.Haushaltsbuch.Domain.Workflows.MonthOverview;

internal sealed class MonthOverviewWorkflow(
    IBookingManager bookingManager,
    ICategoryManager categoryManager,
    ILogger<MonthOverviewWorkflow> logger) : IMonthOverviewWorkflow
{
    // Categories nest at most 5 levels deep; the limit only protects against a cycle in inconsistent data.
    private const int MaxCategoryDepth = 8;

    private readonly IBookingManager _bookingManager = bookingManager;
    private readonly ICategoryManager _categoryManager = categoryManager;
    private readonly ILogger<MonthOverviewWorkflow> _logger = logger;

    public async Task<MonthOverviewData> GetMonthOverviewAsync(
        DateOnly month,
        CancellationToken cancellationToken = default)
    {
        DateOnly firstDay = new(month.Year, month.Month, 1);
        DateOnly lastDay = firstDay.AddMonths(1).AddDays(-1);
        List<Booking> bookings = await _bookingManager.GetBookingsInRangeAsync(firstDay, lastDay, cancellationToken);
        List<Category> categories = await _categoryManager.GetAllCategoriesAsync(cancellationToken);

        Dictionary<Guid, List<Booking>> bookingsByCategory = bookings
            .GroupBy(booking => booking.CategoryId)
            .ToDictionary(group => group.Key, group => group.ToList());
        ILookup<Guid?, Category> childrenByParent = categories.ToLookup(category => category.ParentId);
        List<MonthOverviewNode> incomes = CreateNodes(null, CategoryType.Income, childrenByParent, bookingsByCategory, 0);
        List<MonthOverviewNode> expenses = CreateNodes(null, CategoryType.Expense, childrenByParent, bookingsByCategory, 0);
        decimal incomeTotal = incomes.Sum(node => node.TotalAmount);
        decimal expenseTotal = expenses.Sum(node => node.TotalAmount);

        _logger.LogDebug(
            "Month overview created.",
            new Dictionary<string, string>
            {
                ["Month"] = firstDay.ToString("O"),
                ["BookingCount"] = bookings.Count.ToString()
            });
        return new MonthOverviewData
        {
            Month = firstDay,
            Incomes = incomes,
            Expenses = expenses,
            IncomeTotal = incomeTotal,
            ExpenseTotal = expenseTotal,
            Balance = incomeTotal - expenseTotal
        };
    }

    // The type decides only at the root; sub-categories always share the type of their root category.
    private static List<MonthOverviewNode> CreateNodes(
        Guid? parentId,
        CategoryType type,
        ILookup<Guid?, Category> childrenByParent,
        Dictionary<Guid, List<Booking>> bookingsByCategory,
        int depth)
    {
        if (depth >= MaxCategoryDepth)
        {
            return [];
        }

        List<MonthOverviewNode> nodes = [];
        IEnumerable<Category> categories = childrenByParent[parentId]
            .Where(category => (parentId is not null) || (category.Type == type))
            .OrderBy(category => category.Name, StringComparer.CurrentCultureIgnoreCase);
        foreach (Category category in categories)
        {
            List<Booking> ownBookings = bookingsByCategory.GetValueOrDefault(category.Id) ?? [];
            List<MonthOverviewNode> children =
                CreateNodes(category.Id, type, childrenByParent, bookingsByCategory, depth + 1);
            if ((ownBookings.Count == 0) && (children.Count == 0))
            {
                continue;
            }

            decimal ownAmount = ownBookings.Sum(booking => booking.Amount);
            nodes.Add(new MonthOverviewNode
            {
                Category = category,
                OwnAmount = ownAmount,
                TotalAmount = ownAmount + children.Sum(child => child.TotalAmount),
                BookingCount = ownBookings.Count,
                Children = children
            });
        }

        return nodes;
    }
}
