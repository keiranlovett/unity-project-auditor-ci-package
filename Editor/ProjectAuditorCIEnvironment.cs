using System;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor;
using UnityEngine;

/// <summary>
/// Reads Project Auditor CI environment settings.
/// </summary>
internal static class ProjectAuditorCIEnvironment
{
    /// <summary>
    /// Gets a path from env or fallback.
    /// </summary>
    public static string GetPath(string envVarName, string fallbackRelativePath)
    {
        var raw = Environment.GetEnvironmentVariable(envVarName);
        var path = string.IsNullOrWhiteSpace(raw)
            ? Path.Combine(Directory.GetCurrentDirectory(), fallbackRelativePath)
            : raw;

        return Path.GetFullPath(path);
    }

    /// <summary>
    /// Gets a csv string array from env.
    /// </summary>
    public static string[] GetCsv(string envVarName)
    {
        var raw = Environment.GetEnvironmentVariable(envVarName);
        if (string.IsNullOrWhiteSpace(raw))
            return Array.Empty<string>();

        return raw
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();
    }

    /// <summary>
    /// Gets issue categories from env.
    /// </summary>
    public static IssueCategory[] GetIssueCategories(string envVarName)
    {
        var values = GetCsv(envVarName);
        if (values.Length == 0)
            return Array.Empty<IssueCategory>();

        return values
            .Where(x => Enum.TryParse(x, true, out IssueCategory _))
            .Select(x => (IssueCategory)Enum.Parse(typeof(IssueCategory), x, true))
            .Distinct()
            .ToArray();
    }

    /// <summary>
    /// Gets a bool from env.
    /// </summary>
    public static bool GetBool(string envVarName, bool defaultValue)
    {
        var raw = Environment.GetEnvironmentVariable(envVarName);
        if (string.IsNullOrWhiteSpace(raw))
            return defaultValue;

        switch (raw.Trim().ToLowerInvariant())
        {
            case "1":
            case "true":
            case "yes":
            case "y":
            case "on":
                return true;

            case "0":
            case "false":
            case "no":
            case "n":
            case "off":
                return false;

            default:
                Debug.LogWarning($"[ProjectAuditorCI] Invalid boolean value '{raw}' for {envVarName}. Using default {defaultValue}.");
                return defaultValue;
        }
    }

    /// <summary>
    /// Gets an int from env.
    /// </summary>
    public static int GetInt(string envVarName, int defaultValue)
    {
        var raw = Environment.GetEnvironmentVariable(envVarName);
        if (string.IsNullOrWhiteSpace(raw))
            return defaultValue;

        return int.TryParse(raw, out int value) ? value : defaultValue;
    }

    /// <summary>
    /// Tries to get an enum from env.
    /// </summary>
    public static bool TryGetEnum<TEnum>(string envVarName, out TEnum value) where TEnum : struct
    {
        var raw = Environment.GetEnvironmentVariable(envVarName);
        if (!string.IsNullOrWhiteSpace(raw) && Enum.TryParse(raw.Trim(), true, out value))
            return true;

        value = default;
        return false;
    }

    /// <summary>
    /// Ensures a parent directory exists.
    /// </summary>
    public static void EnsureParentDirectoryExists(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
    }
}
