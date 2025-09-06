# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build System

Nake is a self-hosting task runner for .NET Core. The project uses its own build tool through `Nake.csx` scripts.

### Common Commands

```bash
# Run the default build task (builds in Debug mode)
dotnet nake

# Build with specific configuration
dotnet nake build config=Release
dotnet nake build config=Debug verbose=true

# Run tests
dotnet nake test         # Runs fast tests only
dotnet nake test slow=true  # Runs all tests including slow ones

# Create NuGet packages
dotnet nake pack
dotnet nake pack skipFullCheck=true  # Skip full test suite

# Publish to NuGet (requires NuGetApiKey environment variable)
dotnet nake publish

# List available tasks
dotnet nake -T
```

## Project Structure

The solution follows a multi-project .NET structure with shared configuration:

- **Source/Nake/** - Main executable, the task runner CLI tool
- **Source/Nake.Tests/** - Integration and unit tests for the task runner
- **Source/Utility/** - Utility library with file operations, shell commands, etc.
- **Source/Utility.Tests/** - Tests for the utility library
- **Source/Meta/** - Meta-programming utilities for task definitions

Key configuration files:
- **Directory.Build.props** - Shared MSBuild properties for all projects
- **Nake.csx** - Main build script using Nake's own DSL
- Target framework: .NET 8 (`net8`)

## Development Workflow

1. The project uses Roslyn scripting (`.csx` files) for build automation
2. Dependencies are managed through Directory.Build.props with centralized versioning
3. Tests use NUnit 4.x framework
4. Package versions are controlled in Directory.Build.props

## Testing

Tests are written using NUnit and can be run via:
- `dotnet nake test` - Runs fast tests only (excludes tests marked with `[Category("Slow")]`)
- `dotnet nake test slow=true` - Runs all tests including slow integration tests
- Test results are output to `Artifacts/nunit-test-results.xml`

## Nake DSL Concepts

When working with Nake scripts (`.csx` files):
- `[Nake]` attribute marks methods as command-line tasks
- `[Step]` attribute marks tasks with run-once semantics (build steps)
- Tasks can have parameters that are passed from command line
- Environment variables use `$VAR$` or `%VAR%` syntax
- Shell commands can be executed using string interpolation with await: `await $"command"`