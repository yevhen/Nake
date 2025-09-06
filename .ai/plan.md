# Nake Modernization Implementation Plan

## Overview
This document provides a detailed, step-by-step implementation plan for modernizing the Nake project while maintaining stability on .NET 8 LTS.

**Target Configuration:**
- Framework: .NET 8 LTS (latest patch 8.0.12)
- Language: C# 12
- Support Timeline: Until November 2027

## Pre-Implementation Checklist

- [ ] Create feature branch: `modernization-net8-2025`
- [ ] Ensure local development environment has .NET 8.0.12 SDK

## Phase 1: Critical Security Updates (Day 1) ✅ COMPLETED
**Goal:** Eliminate all known security vulnerabilities

### Step 1.1: Update .NET Runtime ✅
```bash
# Update global.json to specify exact SDK version
{
  "sdk": {
    "version": "8.0.401",
    "rollForward": "latestPatch"
  }
}
```

### Step 1.2: Update Microsoft.Extensions.Logging Packages ✅
**Current:** 2.1.1 → **Target:** 8.0.1 (compatible with .NET 8) ✅ COMPLETED

1. Update Directory.Build.props: ✅
   ```xml
   <MicrosoftExtensionsLoggingVersion>8.0.1</MicrosoftExtensionsLoggingVersion>
   <MicrosoftExtensionsLoggingConsoleVersion>8.0.1</MicrosoftExtensionsLoggingConsoleVersion>
   ```

2. Expected breaking changes: ✅
   - ILogger interface changes minimal
   - Console logger configuration syntax updated

3. Test points: ✅
   - Verify console output formatting
   - Ensure no runtime errors in logging paths

### Step 1.3: Update Microsoft.Build Packages ✅
**Current:** 17.8.3 → **Target:** 17.14.8 ✅ COMPLETED

1. Update in Directory.Build.props: ✅
   ```xml
   <MicrosoftBuildVersion>17.14.8</MicrosoftBuildVersion>
   <MicrosoftBuildTasksCoreVersion>17.14.8</MicrosoftBuildTasksCoreVersion>
   <MicrosoftBuildUtilitiesCoreVersion>17.14.8</MicrosoftBuildUtilitiesCoreVersion>
   ```

2. Verify MSBuild task functionality: ✅
   - Run `dotnet build` ✅ (builds successfully)
   - Check project file parsing still works ✅
   - Validate build output paths ✅
   - Updated System.CodeDom to 9.0.0 for compatibility ✅
   - Added NoWarn for NU1701 and NU1605 during transition ✅

### Step 1.4: Security Validation ✅
- [x] Run `dotnet list package --vulnerable` ✅
- [x] Verify no CVEs remain ✅ (All projects show no vulnerable packages)
- [x] Document resolved vulnerabilities ✅

## Phase 2: Framework and Language Update (Day 1-2)

### Step 2.1: Update Target Framework
1. Verify Directory.Build.props already targets net8.0
2. Update to latest C# language version:
   ```xml
   <LangVersion>12.0</LangVersion>
   ```

### Step 2.2: Update Development Tools
1. Update test SDK and adapter:
   ```xml
   <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.8" />
   <PackageReference Include="NUnit3TestAdapter" Version="4.6.0" />
   ```

### Step 2.3: Enable Nullable Reference Types (Gradual)
1. Add to Directory.Build.props:
   ```xml
   <Nullable>annotations</Nullable>
   ```
2. Plan to enable per-file with `#nullable enable` gradually

## Phase 3: Package Updates - Careful Migration (Day 2-3)

### Step 3.1: Update Roslyn Packages (HIGH RISK)
**Current:** 4.8.0 → **Target:** 4.14.0

1. First, create comprehensive scripting tests:
   ```bash
   dotnet nake test slow=true  # Baseline
   ```

2. Update incrementally:
   ```xml
   <PackageReference Include="Microsoft.CodeAnalysis.Scripting" Version="4.14.0" />
   ```

3. Make sure tests run ok

### Step 3.2: Update NUnit (BREAKING CHANGES)
**Current:** 4.0.1 → **Target:** 4.2.2 (not latest, for stability)

1. Update package:
   ```xml
   <PackageReference Include="NUnit" Version="4.2.2" />
   ```

2. Migration tasks:
   - Search for `Assert.` usage: `grep -r "Assert\." Source/`
   - No immediate changes needed unless using removed assertions
   - Run tests to identify any failures

3. Fix any broken assertions:
   - Classic asserts → Modern equivalents
   - Document any behavior changes

### Step 3.3: Update Remaining Packages
```xml
<!-- Check if newer versions exist -->
<PackageReference Include="MedallionShell" Version="1.6.2" />
<PackageReference Include="Dotnet.Script.DependencyModel" Version="1.5.0" />
<PackageReference Include="Microsoft.Win32.Registry" Version="5.0.0" />
<PackageReference Include="System.Security.Principal.Windows" Version="5.0.0" />
```

## Phase 4: C# Modernization - Incremental (Day 3-5)

### Step 4.1: Automated Quick Wins

#### File-Scoped Namespaces
1. Create modernization script:
   ```bash
   # Find all .cs files with traditional namespaces
   find Source -name "*.cs" -type f
   ```

2. Apply transformation (per file):
   - Convert `namespace X {` → `namespace X;`
   - Reduce indentation
   - Verify compilation after each batch

#### String Interpolation
1. Find string.Format usage:
   ```bash
   grep -r "string\.Format" Source/
   ```

2. Convert patterns:
   - `string.Format("text {0}", var)` → `$"text {var}"`
   - Test output equivalence

#### Collection Expressions (C# 12)
1. Find array initializations:
   ```bash
   grep -r "new.*\[\]" Source/
   ```

2. Modernize:
   - `new Type[0]` → `[]`
   - `new[] { a, b }` → `[a, b]`

### Step 4.2: Structural Improvements

#### Primary Constructors
Target files:
- Source/Utility/EnvironmentVariable.cs
- Source/Nake/TaskArgument.cs

Before testing each change:
1. Backup original file
2. Apply transformation
3. Run relevant unit tests
4. Verify no behavior changes

#### Switch Expressions
Target complex switch statements in:
- Source/Nake/Magic/ArgumentTokenizer.cs
- Source/Utility/Shell.cs

### Step 4.3: Safety Features

#### Init-Only Properties
1. Identify immutable classes
2. Convert fields to init properties where appropriate
3. Maintain binary compatibility

#### Pattern Matching
Modernize null checks and type checks:
- `if (x != null && x is Type t)` → `if (x is Type t)`
- Simplify casting operations

## Phase 5: Testing and Validation (Day 5-6)

### Step 5.1: Comprehensive Test Suite
```bash
# Run all test categories
dotnet nake test slow=true

# Verify build configurations
dotnet nake build config=Debug
dotnet nake build config=Release

# Test package creation
dotnet nake pack

# Verify scripting still works
dotnet nake -T  # List tasks
```

### Step 5.3: Self-Hosting Validation
1. Use modernized Nake to build itself:
   ```bash
   dotnet run --project Source/Nake -- build
   ```

2. Verify recursive build stability

## Phase 6: Documentation and Release (Day 6-7)

### Step 6.1: Update Documentation
- [ ] Update README.md with new requirements

### Step 6.2: CI/CD Pipeline
- [ ] Update appveyor.yml to use .NET 8.0.12

### Step 6.3: Release Preparation
1. Version bump in Directory.Build.props
2. Create release notes highlighting:
   - Security fixes
   - Performance improvements
   - Modernization changes
   - Breaking changes (if any)

## Notes and Considerations

1. **Roslyn Scripting:** Most critical component - test thoroughly
2. **Self-hosting:** Nake builds itself, so maintain working state
3. **C# features:** Apply conservatively, prioritize stability
4. **Communication:** Keep team informed of progress and issues

## Commands Reference

```bash
# Build and test cycle
dotnet nake build config=Release
dotnet nake test slow=true
dotnet nake pack

# Verify changes
git diff Directory.Build.props
dotnet list package --outdated
dotnet list package --vulnerable

# Clean build test
git clean -xfd
dotnet nake
```