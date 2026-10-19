# SpatialAim for OpenTabletDriver

SpatialAim is a configurable set of OpenTabletDriver filters for using a tablet as a relative mouse in 3D space and flight games. It changes movement deltas in OTD's output pipeline; it does not change Windows pointer settings or game sensitivity. This is fully AI Vibecoded for personal use but uploaded to share with the community.

## Filters

- **SpatialAim Pressure Sensitivity** maps pen pressure to a configurable movement multiplier. It is useful when a light touch should preserve fine aim and a firmer press should help cross a large angle.
- **SpatialAim Windows EPP Approximation** adds a configurable speed-dependent curve inspired by Windows Enhance Pointer Precision. It is an OTD-side approximation, not a bit-for-bit reproduction of Windows' internal pointer algorithm.
- **SpatialAim Flick Acceleration** keeps a chosen gain at low speed and increases it for fast movement. This is the specialized option for quick turns and large target reacquisition flicks.
- **SpatialAim Tablet Mapping** corrects the tablet-to-relative-output coordinate map with independent X/Y scale, a 5x5 spatial correction grid, and tablet-button-triggered swipe calibration for repeatable N-shaped or other path deviations.

Swipe calibrations are saved automatically per tablet and reloaded when OTD reconstructs the filter; copying the logged grid into profile settings is optional.

Pressure and speed filters apply the same gain on both axes and preserve pen pressure. Tablet Mapping reshapes absolute tablet coordinates before Relative Output Mode calculates movement deltas. Use one speed-based acceleration filter at a time; stacking them compounds their gains.

## Requirements

- OpenTabletDriver 0.6.7 or newer
- Relative Output Mode
- .NET 8 SDK to build from source

The pressure and speed filters run at `PostTransform`, after relative output has turned tablet positions into movement deltas. Tablet Mapping runs at `PreTransform`, before that conversion. These filters are intended for Relative Output Mode, not Absolute Output Mode.

## Build and install

```sh
dotnet restore SpatialAim.Filters.sln
dotnet test SpatialAim.Filters.sln -c Release
dotnet build src/SpatialAim.Filters/SpatialAim.Filters.csproj -c Release
```

Copy `src/SpatialAim.Filters/bin/Release/net8.0/SpatialAim.Filters.dll` into OpenTabletDriver's user `Plugins` directory, then restart OTD. Add the desired filter in the tablet's Relative Output Mode filter list. See [docs/BUILDING.md](docs/BUILDING.md) for build and installation details and [docs/CONFIGURATION.md](docs/CONFIGURATION.md) for every option and tuning steps.

Filter names, setting labels, and tooltips are available in English, Spanish, Japanese, Simplified Chinese, Korean, German, Polish, and French. OTD 0.6.7 reads plugin attribute text as fixed strings, so choose the locale when building, for example `dotnet build src/SpatialAim.Filters/SpatialAim.Filters.csproj -c Release -p:SpatialAimLocale=fr`. English is the default; see [docs/BUILDING.md](docs/BUILDING.md) for supported locale codes.

In VS Code, open **Terminal > Run Task** and choose a `SpatialAim:` task for restore, tests, Debug/Release builds, building all locales, cleaning, or packaging.

## Important limits

Speed is measured in post-transform relative output units per second, so thresholds depend on Relative Output Mode sensitivity and report timing. The EPP filter is deliberately described as an approximation. Tablet Mapping does not infer your ideal arm curve; calibrate its grid from repeatable movements. No filter can guarantee a specific turn angle such as 180 degrees; game sensitivity, input handling, mapping, and stroke speed all matter.

OTD Plugin-Repository metadata is provided as [a template](plugin-metadata.template.json). Replace its marked publication fields after the GitHub repository and release archive exist. The project uses the [MIT license](LICENSE). Filter defaults are declared explicitly for OTD's settings store; the warp strength starts at 0 intentionally so the filter is neutral until calibrated.

GPT-6 was used to create this plugin suite.