# Build and installation

## Prerequisites

- .NET 8 SDK
- OpenTabletDriver 0.6.7 or newer to load the plugin

The library references `OpenTabletDriver.Plugin` 0.6.7, whose API and target framework are available in the .NET 8 SDK. The plugin itself is a .NET 8 assembly. Newer OTD versions may target a newer .NET runtime; build against the plugin API version supported by the OTD installation you intend to use.

## Build and test

From the repository root:

```sh
dotnet restore SpatialAim.Filters.sln
dotnet test SpatialAim.Filters.sln -c Release
dotnet build src/SpatialAim.Filters/SpatialAim.Filters.csproj -c Release
```

The plugin assembly is produced at:

```text
src/SpatialAim.Filters/bin/Release/net8.0/SpatialAim.Filters.dll
```

## Localized builds

OTD 0.6.7 reads plugin names, setting labels, and tooltips from fixed attribute strings. Select the language at build time with `SpatialAimLocale`; the default is English (`en`). Supported values are `en`, `es`, `ja`, `zh-CN`, `ko`, `de`, `pl`, and `fr`.

For example, build a French plugin with:

```sh
dotnet build src/SpatialAim.Filters/SpatialAim.Filters.csproj -c Release -p:SpatialAimLocale=fr
```

The translation catalog is embedded in the plugin DLL, so installation remains a single-file copy. Replace the installed DLL with the build for the desired locale and restart OTD to switch languages. Unsupported locale values fail the build.

The automated tests cover plugin registration and pipeline stages, localized plugin metadata and catalog entries, explicit OTD property defaults, pressure and speed curve endpoints, interpolation, exponent behavior, response inversion, degenerate thresholds, and 2D tablet-map interpolation and correction behavior.

## GitHub Actions and branches

`.github/workflows/ci-release.yml` runs tests and builds/packages all eight locales on pushes to `Dev`, `Release`, `dev`, `release`, or `main`, and on pull requests targeting those branches. Every successful run uploads a 14-day artifact containing one ZIP per locale. A pushed Git tag such as `v1.0.1` publishes all locale ZIPs, generated OTD metadata, and checksums, but only if its commit is already on the remote `Release` or `release` branch.

Tags created in VS Code are local until pushed. Push the release branch first, then push the version tag to trigger the release workflow:

```sh
git push origin Release
git push origin v1.0.1
```

Branch names are case-sensitive. This repository uses `Dev` and `Release`; if the tag is pushed before its commit is on `Release`, the workflow rejects it. A branch push alone does not push a local tag. Do not reuse an existing version tag; create a new version after pushing the release changes. The `v1.0.0` tag already exists remotely, so subsequent releases should use a newer version.

Use **Terminal > Run Task > SpatialAim: Build All Locales** to test each locale and create local ZIPs under `artifacts/`. The English ZIP is `artifacts/SpatialAim.Filters.zip`; other ZIPs include their locale code. The task leaves the standard build output in English.

To merge and tag a release:

```sh
git switch Release
git merge --no-ff Dev
git tag v1.0.1
git push origin Release
git push origin v1.0.1
```

Use a new version for each release tag and keep `plugin-metadata.template.json`'s repository owner and URLs accurate before publishing.

## VS Code tasks

Use **Terminal > Run Task** for `SpatialAim: Restore`, `SpatialAim: Test`, `SpatialAim: Build Debug`, `SpatialAim: Build Release`, `SpatialAim: Build All Locales`, `SpatialAim: Clean`, and `SpatialAim: Package Release ZIP`. The locale task uses Python 3's standard library for ZIP creation; the single-locale package task uses `zip` on `PATH`.

## Prepare a release and OTD metadata

`plugin-metadata.template.json` contains the repository owner and URLs; the release workflow fills in the version, download URL, and SHA-256 from the pushed tag and built archive. Verify the owner and repository URL before publishing, and only submit the generated release metadata to the OTD Plugin Repository after checking the release assets.

Build the release archive with the plugin DLL at its root, as required by OTD's plugin repository:

```sh
mkdir -p artifacts
zip -j artifacts/SpatialAim.Filters.zip src/SpatialAim.Filters/bin/Release/net8.0/SpatialAim.Filters.dll
sha256sum artifacts/SpatialAim.Filters.zip
```

Publish that archive as the `0.1.0.0` GitHub release asset, copy its exact SHA-256 into the metadata file, and replace every `REPLACE_WITH_...` value. The OTD repository entry then belongs at `Repository/0.6.7.0/<owner>/<repository>/SpatialAim.json` in the Plugin-Repository project. Update the plugin version and supported driver version in the path and JSON when those values change.

## Install

1. Copy `SpatialAim.Filters.dll` into the OpenTabletDriver user `Plugins` directory. Do not copy the OpenTabletDriver framework assembly from the build output.
2. Restart OpenTabletDriver so it discovers the new assembly.
3. Select the tablet, choose **Relative Output Mode**, and add one of the SpatialAim filters in its filter list.

Newly added filter instances receive the values declared in each property's default metadata. If an instance already has saved zero values, OTD may keep those saved values instead of applying new defaults; remove and re-add that filter instance or reset its settings to initialize it from the new defaults.
4. Apply a conservative configuration and test in a low-risk environment before playing.

Plugin directory locations can vary by platform and OTD packaging. Use the plugin folder exposed by your OTD installation or its user data directory if the exact path is not apparent. The plugin is loaded by OTD, not by the game.

## Development notes

- The pressure and speed filters implement `IPositionedPipelineElement<IDeviceReport>` at `PipelinePosition.PostTransform`.
- Tablet Mapping implements the same pipeline interface at `PipelinePosition.PreTransform`, so it modifies absolute tablet coordinates before the relative output transform.
- Swipe calibration is triggered by the `SpatialAim Calibration Toggle` state-binding plugin on a tablet Auxiliary button and segments strokes using raw pressure reports; the result is applied to the live filter and saved per tablet under the user's local application-data directory.
- The speed filters estimate speed from transformed relative deltas and elapsed report time. Their thresholds are in relative output units per second, not tablet pressure units or raw tablet counts.
- Out-of-range reports reset the speed history so repositioning the pen does not carry an old acceleration estimate into the next stroke.
- The pressure filter modifies only the positional delta and leaves pressure and other report fields unchanged.
- OTD 0.6.8's plugin package targets .NET 10. If building specifically against 0.6.8 with a .NET 10 SDK, update the package reference and target framework together, then rerun the suite tests.
