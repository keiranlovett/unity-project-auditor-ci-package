using System;
using System.Linq;
using Unity.ProjectAuditor.Editor;

/// <summary>
/// Writes GitHub workflow annotations.
/// </summary>
internal class ProjectAuditorGitHubAnnotationsExtension : IProjectAuditorCIExtension
{
    /// <summary>
    /// Emits GitHub annotations to stdout.
    /// </summary>
    public void Process(Report report, ProjectAuditorCIOptions options)
    {
        var issues = ProjectAuditorReportUtility.GetAllItems(report)
            .Where(x => x.IsIssue())
            .Where(ProjectAuditorReportUtility.IsMajorOrCritical)
            .Where(x => ProjectAuditorReportFilters.IsIncluded(x, options.ExcludedPathPrefixes))
            .Where(x => !string.IsNullOrWhiteSpace(x.RelativePath))
            .OrderByDescending(GetSeverityRank)
            .ThenBy(x => x.RelativePath)
            .ThenBy(x => x.Line);

        foreach (var item in issues)
        {
            var level = item.Severity.ToString().Equals("Critical", StringComparison.OrdinalIgnoreCase)
                ? "error"
                : "warning";

            var file = Escape(ProjectAuditorReportUtility.NormalizePath(item.RelativePath));
            var line = item.Line > 0 ? item.Line : 1;
            var title = Escape($"Project Auditor [{item.Severity}]");
            var message = Escape(item.Description);

            Console.WriteLine($"::{level} file={file},line={line},title={title}::{message}");
        }
    }

    /// <summary>
    /// Returns a numeric severity rank.
    /// </summary>
    private static int GetSeverityRank(ReportItem item)
    {
        var severity = item.Severity.ToString();

        if (severity.Equals("Critical", StringComparison.OrdinalIgnoreCase))
            return 2;

        if (severity.Equals("Major", StringComparison.OrdinalIgnoreCase))
            return 1;

        return 0;
    }

    /// <summary>
    /// Escapes GitHub annotation values.
    /// </summary>
    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace("%", "%25")
            .Replace("\r", "%0D")
            .Replace("\n", "%0A")
            .Replace(":", "%3A")
            .Replace(",", "%2C");
    }
}
