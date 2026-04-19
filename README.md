# Unity Project Auditor CI (Package)

A small experimental Unity Editor package for running Unity Project Auditor in CI with GitHub Actions.

Once the audit completes, extensions generate:
- a SARIF file for GitHub code scanning
- GitHub annotations
- a Markdown summary for GitHub Actions


## Notes

- SARIF includes only `Critical` and `Major` issues with file paths.
- GitHub annotations emit `::error` for `Critical` issues and `::warning` for `Major` issues.
- The Markdown summary includes the top 10 `Critical` and `Major` issues after filtering.
- The native `.projectauditor` file is still saved in full.

## Workflow example

See:

```text
.github/workflows/project-auditor.yml
```

## Install via UPM

This package can be installed directly from a GitHub repository using Unity Package Manager.

In Unity:

1. Open **Window > Package Manager**
2. Click the **+** button
3. Select **Add package from git URL...**
4. Paste the repository URL

Example:

```text
https://github.com/keiranlovett/unity-project-auditor-ci-package.git
```

Or manually

```
{
  "dependencies": {
    "com.kvcl.project-auditor-ci": "https://github.com/keiranlovett/unity-project-auditor-ci-package.git"
  }
}
```

## Usage

Use this method in CI:

```text
ProjectAuditorCI.AuditAndExport
```

### Environment variables

#### Failure Behaviour

- `PROJECT_AUDITOR_FAIL_THRESHOLD` — Fails the run when the total issue count meets or exceeds this value.
- `PROJECT_AUDITOR_FAIL_ON_ANY_ISSUE` — Fails the run if any issue is found.

#### Output Paths

- `PROJECT_AUDITOR_REPORT` — Output path for the `.projectauditor` report file.
- `PROJECT_AUDITOR_SARIF` — Output path for the SARIF file.
- `PROJECT_AUDITOR_SUMMARY` — Output path for the `.md` summary file.

#### Audit Scope

- `PROJECT_AUDITOR_CATEGORIES` — Comma-separated list of Project Auditor categories to include in the audit.
- `PROJECT_AUDITOR_ASSEMBLIES` — Comma-separated list of Assemblies to audit.
- `PROJECT_AUDITOR_PLATFORM` — (Optional) Build target override used for the audit. Use a valid `UnityEditor.BuildTarget` value such as `StandaloneWindows64`, `Android`, `iOS`, or `WebGL`. See the official Unity `BuildTarget` documentation. 
- `PROJECT_AUDITOR_CODE_OPTIMIZATION` — (Optional) Code optimisation mode used during analysis. Valid values are `Debug` and `Release`. See the official Unity `CodeOptimization` documentation. 
- `PROJECT_AUDITOR_COMPILATION_MODE` — (Optional) Compilation mode used when auditing code. Valid values are `Editor` and `Player`. See the official Unity Project Auditor `CompilationMode` documentation. 

Example:

```text
PROJECT_AUDITOR_ASSEMBLIES=MyGame.Core,MyGame.Runtime
```

#### Output filtering

- `PROJECT_AUDITOR_EXCLUDE_PATH_PREFIXES` - Suppresses package and and any other paths from reporting outputs.

Example:

```text
PROJECT_AUDITOR_EXCLUDE_PATH_PREFIXES=Packages/com.unity.,Library/PackageCache/
```

## TODO:
- Improved Extensions Support
- Extension: Per Assembly Reports


## License

MIT License