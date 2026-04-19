using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor;

/// <summary>
/// Writes a markdown summary.
/// </summary>
internal class ProjectAuditorMarkdownSummaryExtension : IProjectAuditorCIExtension
{
    /// <summary>
    /// Writes the summary file.
    /// </summary>
    public void Process(Report report, ProjectAuditorCIOptions options)
    {
        bool IncludeInSummary(ReportItem item)
        {
            return ProjectAuditorReportFilters.IsIncluded(item, options.ExcludedPathPrefixes);
        }

        int totalIssues = ProjectAuditorReportUtility.CountAllIssues(report, IncludeInSummary);
        int codeIssues = ProjectAuditorReportUtility.CountIssues(report, IssueCategory.Code, IncludeInSummary);
        int assetIssues = ProjectAuditorReportUtility.CountIssues(report, IssueCategory.AssetIssue, IncludeInSummary);
        int settingsIssues = ProjectAuditorReportUtility.CountIssues(report, IssueCategory.ProjectSetting, IncludeInSummary);
        int threshold = options.FailOnAnyIssue ? 1 : options.FailThreshold;

        var topIssues = ProjectAuditorReportUtility.GetAllItems(report)
            .Where(x => x.IsIssue())
            .Where(ProjectAuditorReportUtility.IsMajorOrCritical)
            .Where(IncludeInSummary)
            .OrderByDescending(GetSeverityRank)
            .ThenBy(x => x.RelativePath)
            .ThenBy(x => x.Line)
            .Take(10)
            .ToList();

        var lines = new List<string>
        {
            "# Project Auditor Report",
            "",
            "## Overview",
            $"- Total issues: {totalIssues}",
            $"- Code: {codeIssues}",
            $"- Assets: {assetIssues}",
            $"- Settings: {settingsIssues}",
            "",
            "## Filtering",
            options.ExcludedPathPrefixes.Length > 0
                ? $"- Excluded path prefixes: `{string.Join("`, `", options.ExcludedPathPrefixes)}`"
                : "- No excluded path prefixes configured.",
            "",
            "## Status",
            threshold > 0 && totalIssues >= threshold
                ? $":warning: Threshold exceeded ({totalIssues} issues >= {threshold})"
                : ":white_check_mark: Threshold passed",
            "",
            "## Top 10 Major and Critical Issues"
        };

        if (topIssues.Count == 0)
        {
            lines.Add("");
            lines.Add("- No Major or Critical issues found after filtering.");
        }
        else
        {
            lines.Add("");

            for (int i = 0; i < topIssues.Count; i++)
            {
                var item = topIssues[i];
                var path = string.IsNullOrWhiteSpace(item.RelativePath)
                    ? "(no path)"
                    : ProjectAuditorReportUtility.NormalizePath(item.RelativePath);

                var line = item.Line > 0 ? item.Line.ToString() : "-";
                lines.Add($"{i + 1}. **[{item.Severity}]** `{path}:{line}` - {item.Description}");
            }
        }

        File.WriteAllLines(options.SummaryPath, lines);
    }
    /// <summary>
    /// Returns a numeric severity rank.
    /// </summary>
    private static int GetSeverityRank(ReportItem item)
    {
        var severity = item.Severity.ToString();

        if (severity.Equals("Critical", System.StringComparison.OrdinalIgnoreCase))
            return 2;

        if (severity.Equals("Major", System.StringComparison.OrdinalIgnoreCase))
            return 1;

        return 0;
    }
}
