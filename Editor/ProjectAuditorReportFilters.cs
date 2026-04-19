using System;
using System.Linq;
using Unity.ProjectAuditor.Editor;

/// <summary>
/// Filters Project Auditor report items.
/// </summary>
internal static class ProjectAuditorReportFilters
{
    /// <summary>
    /// Returns true if the item path matches an excluded prefix.
    /// </summary>
    public static bool HasExcludedPrefix(ReportItem item, string[] excludedPrefixes)
    {
        var path = ProjectAuditorReportUtility.NormalizePath(item.RelativePath);
        if (string.IsNullOrWhiteSpace(path) || excludedPrefixes == null || excludedPrefixes.Length == 0)
            return false;

        return excludedPrefixes.Any(prefix =>
            !string.IsNullOrWhiteSpace(prefix) &&
            path.StartsWith(NormalizePrefix(prefix), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Returns true if the item should be included.
    /// </summary>
    public static bool IsIncluded(ReportItem item, string[] excludedPrefixes)
    {
        return !HasExcludedPrefix(item, excludedPrefixes);
    }

    /// <summary>
    /// Normalizes an excluded prefix.
    /// </summary>
    private static string NormalizePrefix(string prefix)
    {
        return (prefix ?? string.Empty).Replace('\\', '/').Trim();
    }
}
