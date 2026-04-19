using Unity.ProjectAuditor.Editor;

/// <summary>
/// Extension hook for Project Auditor CI outputs.
/// </summary>
internal interface IProjectAuditorCIExtension
{
    /// <summary>
    /// Called after the report is created.
    /// </summary>
    void Process(Report report, ProjectAuditorCIOptions options);
}
