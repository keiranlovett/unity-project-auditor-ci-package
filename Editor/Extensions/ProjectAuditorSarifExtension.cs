using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Unity.ProjectAuditor.Editor;
using UnityEngine;

/// <summary>
/// Writes SARIF output.
/// </summary>
internal class ProjectAuditorSarifExtension : IProjectAuditorCIExtension
{
    /// <summary>
    /// Writes the SARIF file.
    /// </summary>
    public void Process(Report report, ProjectAuditorCIOptions options)
    {
        var issues = ProjectAuditorReportUtility.GetAllItems(report)
            .Where(x => x.IsIssue())
            .Where(ProjectAuditorReportUtility.IsMajorOrCritical)
            .Where(x => ProjectAuditorReportFilters.IsIncluded(x, options.ExcludedPathPrefixes))
            .Where(x => !string.IsNullOrWhiteSpace(x.RelativePath))
            .ToList();

        var rules = BuildRules(issues);
        var results = BuildResults(issues, rules);

        var sarif = new SarifLog
        {
            version = "2.1.0",
            runs = new List<Run>
            {
                new Run
                {
                    tool = new Tool
                    {
                        driver = new ToolComponent
                        {
                            name = "Unity Project Auditor",
                            semanticVersion = Application.unityVersion,
                            rules = rules
                        }
                    },
                    automationDetails = new RunAutomationDetails
                    {
                        id = "unity-project-auditor"
                    },
                    columnKind = "utf16CodeUnits",
                    results = results
                }
            }
        };

        File.WriteAllText(options.SarifPath, JsonUtility.ToJson(sarif, true), new UTF8Encoding(false));
    }

    /// <summary>
    /// Builds SARIF rules.
    /// </summary>
    private static List<ReportingDescriptor> BuildRules(List<ReportItem> issues)
    {
        var rulesById = new Dictionary<string, ReportingDescriptor>(StringComparer.Ordinal);

        foreach (var item in issues)
        {
            var ruleId = GetRuleId(item);
            if (rulesById.ContainsKey(ruleId))
                continue;

            rulesById[ruleId] = new ReportingDescriptor
            {
                id = ruleId,
                name = ruleId,
                shortDescription = new Message { text = SafeText(item.Description, "Project Auditor issue") },
                fullDescription = new Message { text = SafeText(item.Description, "Project Auditor issue") },
                defaultConfiguration = new ReportingConfiguration { level = MapLevel(item) }
            };
        }

        return rulesById.Values.ToList();
    }

    /// <summary>
    /// Builds SARIF results.
    /// </summary>
    private static List<Result> BuildResults(List<ReportItem> issues, List<ReportingDescriptor> rules)
    {
        var ruleIndexById = rules
            .Select((rule, index) => new { rule.id, index })
            .ToDictionary(x => x.id, x => x.index, StringComparer.Ordinal);

        var results = new List<Result>();

        foreach (var item in issues)
        {
            var relativePath = ProjectAuditorReportUtility.NormalizePath(item.RelativePath);
            var line = item.Line > 0 ? item.Line : 1;
            var ruleId = GetRuleId(item);

            results.Add(new Result
            {
                ruleId = ruleId,
                ruleIndex = ruleIndexById[ruleId],
                level = MapLevel(item),
                message = new Message { text = SafeText(item.Description, "Project Auditor issue") },
                locations = new List<LocationWrapper>
                {
                    new LocationWrapper
                    {
                        physicalLocation = new PhysicalLocation
                        {
                            artifactLocation = new ArtifactLocation { uri = relativePath },
                            region = new Region { startLine = line }
                        }
                    }
                },
                partialFingerprints = new PartialFingerprints
                {
                    primaryLocationLineHash = ComputeStableFingerprint(ruleId, relativePath, line)
                }
            });
        }

        return results;
    }

    /// <summary>
    /// Gets a stable rule id.
    /// </summary>
    private static string GetRuleId(ReportItem item)
    {
        var id = item.Id.ToString();
        return string.IsNullOrWhiteSpace(id)
            ? $"PA-{item.Category}-{StableHash(item.Description)}"
            : id;
    }

    /// <summary>
    /// Maps severity to SARIF level.
    /// </summary>
    private static string MapLevel(ReportItem item)
    {
        return item.Severity.ToString().Equals("Critical", StringComparison.OrdinalIgnoreCase)
            ? "error"
            : "warning";
    }

    /// <summary>
    /// Returns fallback text if needed.
    /// </summary>
    private static string SafeText(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    /// <summary>
    /// Builds a stable fingerprint.
    /// </summary>
    private static string ComputeStableFingerprint(string ruleId, string relativePath, int line)
    {
        return StableHash($"{ruleId}|{relativePath}|{line}") + ":" + line;
    }

    /// <summary>
    /// Computes a sha256 hash.
    /// </summary>
    private static string StableHash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input ?? string.Empty));

        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));

        return sb.ToString();
    }

    [Serializable]
    private class SarifLog
    {
        public string version;
        public List<Run> runs;
    }

    [Serializable]
    private class Run
    {
        public Tool tool;
        public RunAutomationDetails automationDetails;
        public string columnKind;
        public List<Result> results;
    }

    [Serializable]
    private class Tool
    {
        public ToolComponent driver;
    }

    [Serializable]
    private class ToolComponent
    {
        public string name;
        public string semanticVersion;
        public List<ReportingDescriptor> rules;
    }

    [Serializable]
    private class ReportingDescriptor
    {
        public string id;
        public string name;
        public Message shortDescription;
        public Message fullDescription;
        public ReportingConfiguration defaultConfiguration;
    }

    [Serializable]
    private class ReportingConfiguration
    {
        public string level;
    }

    [Serializable]
    private class Result
    {
        public string ruleId;
        public int ruleIndex;
        public string level;
        public Message message;
        public List<LocationWrapper> locations;
        public PartialFingerprints partialFingerprints;
    }

    [Serializable]
    private class Message
    {
        public string text;
    }

    [Serializable]
    private class LocationWrapper
    {
        public PhysicalLocation physicalLocation;
    }

    [Serializable]
    private class PhysicalLocation
    {
        public ArtifactLocation artifactLocation;
        public Region region;
    }

    [Serializable]
    private class ArtifactLocation
    {
        public string uri;
    }

    [Serializable]
    private class Region
    {
        public int startLine;
    }

    [Serializable]
    private class PartialFingerprints
    {
        public string primaryLocationLineHash;
    }

    [Serializable]
    private class RunAutomationDetails
    {
        public string id;
    }
}
