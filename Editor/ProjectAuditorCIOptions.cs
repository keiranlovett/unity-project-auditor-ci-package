using System;
using UnityEditor;
using Unity.ProjectAuditor.Editor;
using UnityEngine;

/// <summary>
/// Holds Project Auditor CI options.
/// </summary>
internal class ProjectAuditorCIOptions
{
    /// <summary>
    /// Native Project Auditor report path.
    /// </summary>
    public string ReportPath;

    /// <summary>
    /// SARIF output path.
    /// </summary>
    public string SarifPath;

    /// <summary>
    /// Markdown summary output path.
    /// </summary>
    public string SummaryPath;

    /// <summary>
    /// Categories to audit.
    /// </summary>
    public IssueCategory[] Categories = Array.Empty<IssueCategory>();

    /// <summary>
    /// Assemblies to audit.
    /// </summary>
    public string[] AssemblyNames = Array.Empty<string>();

    /// <summary>
    /// Path prefixes to exclude from reporting outputs.
    /// </summary>
    public string[] ExcludedPathPrefixes = Array.Empty<string>();

    /// <summary>
    /// Whether a platform override exists.
    /// </summary>
    public bool HasPlatform;

    /// <summary>
    /// Platform override value.
    /// </summary>
    public BuildTarget Platform;

    /// <summary>
    /// Whether a code optimization override exists.
    /// </summary>
    public bool HasCodeOptimization;

    /// <summary>
    /// Code optimization override value.
    /// </summary>
    public CodeOptimization CodeOptimization;

    /// <summary>
    /// Whether a compilation mode override exists.
    /// </summary>
    public bool HasCompilationMode;

    /// <summary>
    /// Compilation mode override value.
    /// </summary>
    public CompilationMode CompilationMode;

    /// <summary>
    /// Fails if total issues are greater than or equal to this value.
    /// </summary>
    public int FailThreshold;

    /// <summary>
    /// Fails if any issue exists.
    /// </summary>
    public bool FailOnAnyIssue;

    /// <summary>
    /// Creates options from environment variables.
    /// </summary>
    public static ProjectAuditorCIOptions FromEnvironment()
    {
        var options = new ProjectAuditorCIOptions
        {
            ReportPath = ProjectAuditorCIEnvironment.GetPath(
                "PROJECT_AUDITOR_REPORT",
                "ProjectAuditor/report.projectauditor"),

            SarifPath = ProjectAuditorCIEnvironment.GetPath(
                "PROJECT_AUDITOR_SARIF",
                "ProjectAuditor/results.sarif"),

            SummaryPath = ProjectAuditorCIEnvironment.GetPath(
                "PROJECT_AUDITOR_SUMMARY",
                "ProjectAuditor/summary.md"),

            Categories = ProjectAuditorCIEnvironment.GetIssueCategories("PROJECT_AUDITOR_CATEGORIES"),
            AssemblyNames = ProjectAuditorCIEnvironment.GetCsv("PROJECT_AUDITOR_ASSEMBLIES"),
            ExcludedPathPrefixes = GetExcludedPrefixesOrDefault(),
            FailThreshold = ProjectAuditorCIEnvironment.GetInt("PROJECT_AUDITOR_FAIL_THRESHOLD", 0),
            FailOnAnyIssue = ProjectAuditorCIEnvironment.GetBool("PROJECT_AUDITOR_FAIL_ON_ANY_ISSUE", false)
        };

        Debug.Log($"[ProjectAuditorCI] Raw PROJECT_AUDITOR_EXCLUDE_PATH_PREFIXES = '{Environment.GetEnvironmentVariable("PROJECT_AUDITOR_EXCLUDE_PATH_PREFIXES")}'");
        Debug.Log($"[ProjectAuditorCI] Parsed excluded prefixes = '{string.Join(", ", options.ExcludedPathPrefixes)}'");
        Debug.Log($"[ProjectAuditorCI] Raw PROJECT_AUDITOR_ASSEMBLIES = '{Environment.GetEnvironmentVariable("PROJECT_AUDITOR_ASSEMBLIES")}'");
        Debug.Log($"[ProjectAuditorCI] Parsed assemblies = '{string.Join(", ", options.AssemblyNames)}'");

        if (ProjectAuditorCIEnvironment.TryGetEnum("PROJECT_AUDITOR_PLATFORM", out BuildTarget platform))
        {
            options.HasPlatform = true;
            options.Platform = platform;
        }

        if (ProjectAuditorCIEnvironment.TryGetEnum("PROJECT_AUDITOR_CODE_OPTIMIZATION", out CodeOptimization codeOptimization))
        {
            options.HasCodeOptimization = true;
            options.CodeOptimization = codeOptimization;
        }

        if (ProjectAuditorCIEnvironment.TryGetEnum("PROJECT_AUDITOR_COMPILATION_MODE", out CompilationMode compilationMode))
        {
            options.HasCompilationMode = true;
            options.CompilationMode = compilationMode;
        }

        return options;
    }

    /// <summary>
    /// Builds analysis params.
    /// </summary>
    public AnalysisParams ToAnalysisParams()
    {
        var analysisParams = new AnalysisParams();

        if (Categories.Length > 0)
            analysisParams.Categories = Categories;

        if (AssemblyNames.Length > 0)
            analysisParams.AssemblyNames = AssemblyNames;

        if (HasPlatform)
            analysisParams.Platform = Platform;

        if (HasCodeOptimization)
            analysisParams.CodeOptimization = CodeOptimization;

        if (HasCompilationMode)
            analysisParams.CompilationMode = CompilationMode;

        return analysisParams;
    }

    /// <summary>
    /// Ensures output directories exist.
    /// </summary>
    public void EnsureDirectories()
    {
        ProjectAuditorCIEnvironment.EnsureParentDirectoryExists(ReportPath);
        ProjectAuditorCIEnvironment.EnsureParentDirectoryExists(SarifPath);
        ProjectAuditorCIEnvironment.EnsureParentDirectoryExists(SummaryPath);
    }

    /// <summary>
    /// Gets excluded prefixes or defaults.
    /// </summary>
    private static string[] GetExcludedPrefixesOrDefault()
    {
        var values = ProjectAuditorCIEnvironment.GetCsv("PROJECT_AUDITOR_EXCLUDE_PATH_PREFIXES");
        if (values.Length > 0)
            return values;

        return new[]
        {
            "Packages/com.unity.",
            "Library/PackageCache/"
        };
    }
}
