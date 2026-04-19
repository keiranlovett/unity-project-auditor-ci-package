using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.ProjectAuditor.Editor;

/// <summary>
/// Runs Project Auditor in CI and from the Unity Editor.
/// </summary>
public static class ProjectAuditorCI
{
    /// <summary>
    /// Batch mode entrypoint for CI.
    /// </summary>
    public static void AuditAndExport()
    {
        RunAudit(shouldExitEditor: true);
    }

    /// <summary>
    /// Menu entrypoint for editor usage.
    /// </summary>
    [MenuItem("Tools/Project Auditor/Run Audit And Export")]
    public static void AuditAndExportFromMenu()
    {
        RunAudit(shouldExitEditor: false);
    }
    
    /// <summary>
    /// Menu utility for opening output folder easily.
    /// </summary>
    [MenuItem("Tools/Project Auditor/Open Output Folder")]
    public static void OpenOutputFolder()
    {
        var options = ProjectAuditorCIOptions.FromEnvironment();
        var folder = System.IO.Path.GetDirectoryName(options.ReportPath);

        if (!string.IsNullOrWhiteSpace(folder))
            EditorUtility.RevealInFinder(folder);
    }

    /// <summary>
    /// Runs the audit flow.
    /// </summary>
    private static void RunAudit(bool shouldExitEditor)
    {
        int exitCode = 0;
        try
        {
            var options = ProjectAuditorCIOptions.FromEnvironment();
            options.EnsureDirectories();

            var analysisParams = options.ToAnalysisParams();

            int streamedIssueCount = 0;
            analysisParams.OnIncomingIssues = issues =>
            {
                if (issues == null)
                    return;

                streamedIssueCount += issues.Count();
                Debug.Log($"[ProjectAuditorCI] Incoming issues: +{issues.Count()} (total streamed: {streamedIssueCount})");
            };

            analysisParams.OnCompleted = _ =>
            {
                Debug.Log("[ProjectAuditorCI] Analysis complete.");
            };

            var auditor = new ProjectAuditor();
            var report = auditor.Audit(analysisParams);
            report.Save(options.ReportPath);

            var extensions = new IProjectAuditorCIExtension[]
            {
                new ProjectAuditorSarifExtension(),
                new ProjectAuditorMarkdownSummaryExtension(),
                new ProjectAuditorGitHubAnnotationsExtension()
            };

            foreach (var extension in extensions)
                extension.Process(report, options);

            Debug.Log($"[ProjectAuditorCI] Report saved to: {options.ReportPath}");

            int totalIssues = ProjectAuditorReportUtility.CountAllIssues(
                report,
                item => ProjectAuditorReportFilters.IsIncluded(item, options.ExcludedPathPrefixes));

            int effectiveThreshold = options.FailOnAnyIssue ? 1 : Math.Max(0, options.FailThreshold);

            if (effectiveThreshold > 0 && totalIssues >= effectiveThreshold)
            {
                Debug.LogError($"[ProjectAuditorCI] Failing build. Total issues {totalIssues} >= threshold {effectiveThreshold}.");
                exitCode = 2;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!shouldExitEditor)
            {
                Debug.Log("[ProjectAuditorCI] Audit finished from menu.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[ProjectAuditorCI] Unhandled exception.");
            Debug.LogException(ex);
            exitCode = 1;
        }
        finally
        {
            if (shouldExitEditor)
                EditorApplication.Exit(exitCode);
        }
    }
}