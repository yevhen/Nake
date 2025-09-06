# Changelog

## [4.0.0]

### Added
- .NET 8.0 LTS support for C# 12 language features

### Changed
- **BREAKING**: Minimum required runtime updated to .NET 8.0

### Security
- Updated Microsoft.Extensions.Logging from 2.1.1 to 8.0.1 (resolves multiple security vulnerabilities)
- Updated Microsoft.Build packages from 17.8.3 to 17.14.8 (security fixes)
- Updated System.CodeDom to 9.0.8 (security patches)
- Updated System.Security.Cryptography.ProtectedData to 9.0.8
- All security vulnerabilities resolved - no known CVEs remain

### Dependencies
- Microsoft.CodeAnalysis.Scripting: 4.8.0 → 4.14.0 (Roslyn compiler improvements)
- NUnit: 4.0.1 → 4.2.2 (testing framework updates)
- Microsoft.NET.Test.Sdk: Updated to 17.14.1
- NUnit3TestAdapter: Updated to 4.6.0
- DotnetScript.DependencyModel: 1.5.0 → 1.6.0
- Various supporting packages updated to their latest stable version