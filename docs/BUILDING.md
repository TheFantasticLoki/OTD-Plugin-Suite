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

The automated tests cover plugin registration and pipeline stages, explicit OTD property defaults, pressure and speed curve endpoints, interpolation, exponent behavior, response inversion, degenerate thresholds, and 2D tablet-map interpolation and correction behavior.

## GitHub Actions and branches

`.github/workflows/ci-release.yml` runs restore, tests, Release build, and ZIP packaging on pushes to `dev` and `release`, and on pull requests targeting either branch. Every successful run uploads a 14-day build artifact. A Git tag such as `v0.1.0.0` publishes a GitHub release with the plugin ZIP, generated OTD metadata, and archive SHA-256, but only if the tagged commit is on `release`.

This workspace is not currently initialized as a Git repository, so the branches cannot be created here. After initializing/pushing the repository, create the branches on the remote with:

```sh
git switch -c dev
git push -u origin dev
git switch -c release
git push -u origin release
git switch dev
```

Keep day-to-day work on `dev`. Merge tested changes into `release`, then create and push a version tag from that branch to publish:

```sh
git switch release
git merge --no-ff dev
git tag v0.1.0.0
git push origin release --tags
```

Use a new four-part version for each release tag and keep `plugin-metadata.template.json`'s repository owner and URLs accurate before publishing.

## VS Code tasks

Use **Terminal > Run Task** for `SpatialAim: Restore`, `SpatialAim: Test`, `SpatialAim: Build Debug`, `SpatialAim: Build Release`, `SpatialAim: Clean`, and `SpatialAim: Package Release ZIP`. The package task uses `zip` and requires it on `PATH`.

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
