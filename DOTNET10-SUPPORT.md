# .NET 10 Support

This repository has been updated to support .NET 10 (LTS) as a target framework.

## Current Status

The codebase has been prepared for .NET 10 targeting:

- **Library projects** (in `src/modules/`): Multi-target `net8.0`, `net9.0`, and `net10.0`
- **Application projects** (in `src/apps/`): Target `net10.0`
- **Test projects** (in `test/`): Target `net10.0`
- **Build system** (`build/_build.csproj`): Targets `net10.0`

## Package Versions

The central package management file (`Directory.Packages.props`) has been updated to prepare for .NET 10:
- `MicrosoftVersion`: 10.0.0
- `ResilienceVersion`: 10.0.0

These versions will need to be adjusted to the actual released versions once .NET 10 SDK and runtime packages are available.

## CI/CD

GitHub Actions workflows have been updated to use the .NET 10 SDK:
- `.github/workflows/pr.yml`: Uses .NET 10.x
- `.github/workflows/packages.yml`: Multi-SDK setup with 8.0.x, 9.0.x, and 10.0.x
- `.github/workflows/docker-ca.yml`: Uses .NET 10.x
- `.github/workflows/copilot-setup-steps.yml.yml`: Uses .NET 10.x

## Building the Project

Once .NET 10 SDK is released and available:

1. Install the .NET 10 SDK from https://dotnet.microsoft.com/download/dotnet/10.0
2. Build the solution:
   ```bash
   dotnet restore
   dotnet build
   ```

## Backward Compatibility

The changes maintain full backward compatibility:
- Libraries continue to support .NET 8.0 and .NET 9.0 through multi-targeting
- Consumers using .NET 8.0 or .NET 9.0 can still reference and use the Elsa Workflows libraries
- Only applications and tests have been updated to target .NET 10 exclusively

## Migration Notes

### For Library Consumers
If you're consuming Elsa Workflows libraries in your application:
- No changes required if you're using .NET 8.0 or .NET 9.0
- To use .NET 10, simply upgrade your application's target framework to `net10.0`

### For Contributors
When .NET 10 becomes available:
1. Update `Directory.Packages.props` with actual .NET 10 package versions
2. Test all projects build successfully with the .NET 10 SDK
3. Run all tests to ensure compatibility
4. Update documentation as needed

## Related Changes

The following projects have been updated with explicit .NET 10 targets:
- `src/modules/Elsa.Persistence.EFCore.MySql`
- `src/extensions/Elsa.Testing.Extensions`
- All test projects in `test/integration/`, `test/component/`, and `test/performance/`

Note: .NET 7.0 support has been removed from `Elsa.Testing.Extensions` as part of this update, as it is no longer a supported .NET version.
