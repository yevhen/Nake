# Nake Project Modernization Assessment Report

## 📊 Executive Summary

This report provides a comprehensive analysis of the Nake project modernization requirements, including .NET SDK updates, security vulnerabilities, package dependencies, and C# language modernization opportunities.

**Assessment Date:** January 2025  
**Current Project State:** .NET 8, C# 11.0, Multiple outdated packages with security vulnerabilities

## 🎯 Target Versions

### Latest .NET SDK Versions (January 2025)

- **Latest Stable Version:** .NET 9.0.1 (released January 14, 2025)
  - Standard Term Support (STS) - 18 months support until May 12, 2026
  - Includes C# 13 language support
  - Performance and feature improvements

- **Latest LTS Version:** .NET 8.0.12 (released January 14, 2025)
  - Long Term Support until November 2027
  - Currently used by the project
  - Includes C# 12 language support

### C# Language Versions

- **C# 13** - Available with .NET 9
  - params Collections Enhancement
  - New Lock Type Support
  - ref struct Enhancements
  - Partial Properties and Indexers
  - Overload Resolution Priority
  - Index from End in Object Initializers
  - field Keyword (Preview)

- **C# 12** - Available with .NET 8
  - Primary constructors
  - Collection expressions
  - Inline arrays
  - Optional parameters in lambda expressions
  - ref readonly parameters
  - Alias any type
  - Experimental attribute

## ⚠️ Critical Security Issues

### Immediate Action Required

1. **Microsoft.Extensions.Logging Packages**
   - Current Version: 2.1.1 (from .NET Core 2.1 era, ~2018)
   - Latest Version: 9.0.8
   - **Risk Level: CRITICAL**
   - Missing 6+ years of security patches and improvements

2. **Runtime Vulnerabilities**
   - **CVE-2025-30399** (June 2025): Remote code execution in .NET 8 runtime
   - **CVE-2025-26646** (May 2025): Microsoft.Build.Tasks.core.dll security issue
   - **CVE-2024-38229**, **CVE-2024-43485**, **CVE-2024-35264**: Previous 2024 vulnerabilities

## 📦 Package Dependency Analysis

### Current Package Versions

| Package | Current | Latest | Priority | Breaking Changes |
|---------|---------|---------|----------|------------------|
| **Microsoft.Extensions.Logging** | 2.1.1 | 9.0.8 | CRITICAL | Minor API changes |
| **Microsoft.Extensions.Logging.Console** | 2.1.1 | 9.0.8 | CRITICAL | Minor API changes |
| **Microsoft.CodeAnalysis.Scripting** | 4.8.0 | 4.14.0 | HIGH | Yes - SyntaxNode serialization removed |
| **Microsoft.Build** | 17.8.3 | 17.14.8 | HIGH | No |
| **Microsoft.Build.Tasks.Core** | 17.8.3 | 17.14.8 | HIGH | No |
| **Microsoft.Build.Utilities.Core** | 17.8.3 | 17.14.8 | HIGH | No |
| **NUnit** | 4.0.1 | 4.4.0 | MEDIUM | Yes - Classic asserts moved |
| **NUnit3TestAdapter** | 4.5.0 | Latest | MEDIUM | No |
| **Microsoft.NET.Test.Sdk** | 17.8.0 | 17.14.8 | MEDIUM | No |
| **MedallionShell** | 1.6.2 | Check | LOW | Unknown |
| **Dotnet.Script.DependencyModel** | 1.5.0 | Check | LOW | Unknown |
| **Microsoft.Win32.Registry** | 5.0.0 | Latest | LOW | No |
| **System.CodeDom** | 8.0.0 | Current | OK | - |
| **System.Security.Principal.Windows** | 5.0.0 | Latest | LOW | No |
| **System.Security.Cryptography.ProtectedData** | 8.0.0 | Current | OK | - |

### Breaking Changes Details

#### Microsoft.CodeAnalysis.Scripting (4.8.0 → 4.14.0)
- SyntaxNode serialization/deserialization APIs removed
- Assembly.Location returns empty string on non-Windows platforms
- Requires .NET SDK 6.0.2+ for development

#### NUnit (4.0.1 → 4.4.0)
- Classic asserts moved to `NUnit.Framework.Legacy` namespace
- `Assert.AreEqual` → `ClassicAssert.AreEqual`
- Minimum target framework: .NET Framework 4.6.2 and .NET 6.0

## 🔧 C# Language Modernization Opportunities

### Quick Wins (Low Risk, High Impact)

1. **File-Scoped Namespaces**
   ```csharp
   // Before
   namespace Nake
   {
       public class Program { }
   }
   
   // After
   namespace Nake;
   
   public class Program { }
   ```

2. **String Interpolation**
   ```csharp
   // Before
   string.Format("Usage: {0} [options]", Runner.Label())
   
   // After
   $"Usage: {Runner.Label()} [options]"
   ```

3. **Collection Expressions (C# 12)**
   ```csharp
   // Before
   new TaskArgument[0]
   
   // After
   []
   ```

4. **Switch Expressions**
   ```csharp
   // Before
   switch (state.CurrentChar)
   {
       case '\'': HandleQuote(); break;
       case ' ': HandleWhitespace(); break;
       default: HandleDefault(); break;
   }
   
   // After
   state.CurrentChar switch
   {
       '\'' => HandleQuote(),
       ' ' => HandleWhitespace(),
       _ => HandleDefault()
   };
   ```

### Significant Improvements (Medium Risk, High Impact)

1. **Primary Constructors**
   ```csharp
   // Before
   struct EnvironmentVariable
   {
       public readonly string Name;
       public readonly string Value;
       
       public EnvironmentVariable(string name, string value)
       {
           Name = name;
           Value = value;
       }
   }
   
   // After
   struct EnvironmentVariable(string name, string value)
   {
       public readonly string Name = name;
       public readonly string Value = value;
   }
   ```

2. **Record Types for DTOs**
   ```csharp
   // Before
   public class TaskDeclaration : IEquatable<TaskDeclaration>
   {
       // Manual equality implementation
   }
   
   // After
   public record TaskDeclaration(string Path, MethodDeclarationSyntax Declaration, bool IsStep);
   ```

3. **Nullable Reference Types**
   - Add `#nullable enable` to all files
   - Annotate nullable parameters and return types
   - Eliminate potential null reference exceptions

4. **Init-Only Properties**
   ```csharp
   // Before
   public readonly string BasePath;
   
   // After
   public string BasePath { get; init; }
   ```

### Additional Modernization Opportunities

- **Global Using Directives** - Reduce repetitive using statements
- **Raw String Literals** - Better multi-line string handling
- **Required Members** - Enforce required properties
- **Pattern Matching Improvements** - More expressive conditionals

## 🚀 Migration Strategy Options

### Option 1: Conservative Approach (.NET 8 LTS)

**Pros:**
- Long-term support until November 2027
- Minimal migration risk
- C# 12 features available
- Stable for production

**Cons:**
- Missing latest performance improvements
- No C# 13 features

**Migration Steps:**
1. Update to .NET 8.0.12 (latest patch)
2. Update all security-critical packages immediately
3. Gradually update other packages
4. Enable C# 12 language features
5. Implement quick-win modernizations

### Option 2: Progressive Approach (.NET 9)

**Pros:**
- Latest features and performance
- C# 13 language features
- Better cloud-native support
- Enhanced AI integration capabilities

**Cons:**
- Shorter support window (18 months)
- Slightly higher migration risk
- Not LTS

**Migration Steps:**
1. Update to .NET 9.0.1
2. Update all packages to latest versions
3. Enable C# 13 language features
4. Implement comprehensive modernizations
5. Plan for .NET 10 LTS upgrade in November 2025

## 📋 Recommended Action Plan

### Phase 1: Critical Security Updates (Immediate)
1. Update Microsoft.Extensions.Logging packages to 9.0.8
2. Update .NET runtime to latest patch version
3. Update Microsoft.Build packages to 17.14.8

### Phase 2: Framework Decision (Week 1)
1. Evaluate support requirements
2. Choose between .NET 8 LTS or .NET 9
3. Update target framework in Directory.Build.props

### Phase 3: Package Updates (Week 1-2)
1. Update Roslyn packages (test scripting functionality)
2. Update NUnit (migrate classic asserts)
3. Update remaining packages

### Phase 4: C# Modernization (Week 2-3)
1. Apply file-scoped namespaces globally
2. Replace string.Format with interpolation
3. Implement collection expressions
4. Convert to switch expressions
5. Add nullable reference types (gradual)

### Phase 5: Testing & Validation (Week 3-4)
1. Run full test suite
2. Validate scripting functionality
3. Test package creation and publishing
4. Performance benchmarking

## 🎯 Success Criteria

- ✅ All security vulnerabilities resolved
- ✅ All packages updated to latest compatible versions
- ✅ C# language version upgraded to 12 or 13
- ✅ Code modernized with latest language features
- ✅ All tests passing
- ✅ Build and publish workflows functional
- ✅ No performance regressions

## 📝 Notes

- The project's self-hosting nature requires careful testing of build scripts
- Roslyn scripting functionality is critical - test thoroughly after updates
- Consider creating a branch for modernization work
- Document any breaking changes for users
- Update README with new requirements

## 🔗 References

- [.NET 9 Release Notes](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- [.NET 8 LTS Documentation](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [C# 13 Features](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-13)
- [C# 12 Features](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12)
- [Breaking Changes in .NET 9](https://docs.microsoft.com/en-us/dotnet/core/compatibility/9.0)
- [NUnit Migration Guide](https://docs.nunit.org/articles/nunit/release-notes/breaking-changes.html)