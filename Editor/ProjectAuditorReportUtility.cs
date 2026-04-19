using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor;

/// <summary>
/// Utility methods for Project Auditor reports.
/// </summary>
internal static class ProjectAuditorReportUtility
{
    /// <summary>
    /// Counts all issues in the report.
    /// </summary>
    public static int CountAllIssues(Report report)
    {
        int total = 0;

        foreach (IssueCategory category in Enum.GetValues(typeof(IssueCategory)))
            total += CountIssues(report, category);

        return total;
    }

    /// <summary>
    /// Counts issues for a category.
    /// </summary>
    public static int CountIssues(Report report, IssueCategory category)
    {
        var items = report.FindByCategory(category);
        return items != null ? items.Count : 0;
    }

    /// <summary>
    /// Counts all filtered issues.
    /// </summary>
    public static int CountAllIssues(Report report, Func<ReportItem, bool> predicate)
    {
        return GetAllItems(report)
            .Where(x => x.IsIssue())
            .Where(predicate)
            .Count();
    }

    /// <summary>
    /// Counts filtered issues for a category.
    /// </summary>
    public static int CountIssues(Report report, IssueCategory category, Func<ReportItem, bool> predicate)
    {
        var items = report.FindByCategory(category);
        if (items == null || items.Count == 0)
            return 0;

        return items
            .Where(x => x.IsIssue())
            .Where(predicate)
            .Count();
    }

    /// <summary>
    /// Gets all report items.
    /// </summary>
    public static List<ReportItem> GetAllItems(Report report)
    {
        var items = new List<ReportItem>();

        foreach (IssueCategory category in Enum.GetValues(typeof(IssueCategory)))
        {
            var categoryItems = report.FindByCategory(category);
            if (categoryItems == null || categoryItems.Count == 0)
                continue;

            items.AddRange(categoryItems);
        }

        return items;
    }

    /// <summary>
    /// Returns true if severity is Major or Critical.
    /// </summary>
    public static bool IsMajorOrCritical(ReportItem item)
    {
        var severity = item.Severity.ToString();
        return severity.Equals("Major", StringComparison.OrdinalIgnoreCase) ||
               severity.Equals("Critical", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Normalizes a path for logs
    /// </summary>
    public static string NormalizePath(string path)
    {
        return string.IsNullOrWhiteSpace(path) ? string.Empty : path.Replace('\\', '/');
    }
}