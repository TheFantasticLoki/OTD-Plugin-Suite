using System.Globalization;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using SpatialAim.Filters.Curves;

namespace SpatialAim.Filters.Filters;

[PluginName("SpatialAim Tablet Mapping")]
public sealed class TabletSpaceWarpFilter : IPositionedPipelineElement<IDeviceReport>
{
    private const string NeutralCorrectionSamples = "0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0";

    private float digitizerMaximumX = 65535f;
    private float digitizerMaximumY = 65535f;
    private float mappingCenterXPercent = 50f;
    private float mappingCenterYPercent = 50f;
    private float horizontalScale = 1f;
    private float verticalScale = 1f;
    private string horizontalCorrectionSamples = NeutralCorrectionSamples;
    private string verticalCorrectionSamples = NeutralCorrectionSamples;
    private float[] parsedHorizontalCorrections = new float[TabletSpaceWarpMath.GridValueCount];
    private float[] parsedVerticalCorrections = new float[TabletSpaceWarpMath.GridValueCount];
    private float calibrationSwipeCount = 3f;
    private float calibrationContactPressure = 1f;
    private bool isCalibrating;
    private readonly List<List<Vector2>> calibrationStrokes = new();
    private List<Vector2>? activeCalibrationStroke;
    private TabletReference? tablet;

    public event Action<IDeviceReport>? Emit;

    public PipelinePosition Position => PipelinePosition.PreTransform;

    [TabletReference]
    public TabletReference? Tablet
    {
        get => tablet;
        set
        {
            tablet = value;
            if (tablet == null)
                return;

            var tabletKey = TabletMappingCalibrationStore.GetTabletKey(tablet);
            var storagePath = TabletMappingCalibrationStore.GetStoragePath(tabletKey);
            if (TabletMappingCalibrationStore.TryLoad(tabletKey, out var correctionGrid))
            {
                VerticalCorrectionSamples = correctionGrid;
                TabletMappingCalibrationSignal.WriteConsoleDiagnostic(
                    $"Loaded calibration for '{tablet.Properties.Name}' from '{storagePath}'. Vertical grid: {correctionGrid}");
            }
            else
            {
                TabletMappingCalibrationSignal.WriteConsoleDiagnostic(
                    $"No saved calibration found for '{tablet.Properties.Name}' at '{storagePath}'.");
            }
        }
    }

    [SliderProperty("Digitizer maximum X", 1f, 1048575f, 65535f)]
    [DefaultPropertyValue(65535f)]
    [ToolTip("Open OTD's Tablet Debugger and move the pen around the tablet edges/corners. Enter the largest raw X value shown; use the full digitizer maximum, not the active-area width.")]
    public float DigitizerMaximumX
    {
        get => digitizerMaximumX;
        set => digitizerMaximumX = float.IsFinite(value) ? Math.Clamp(value, 1f, 1048575f) : 65535f;
    }

    [SliderProperty("Digitizer maximum Y", 1f, 1048575f, 65535f)]
    [DefaultPropertyValue(65535f)]
    [ToolTip("Open OTD's Tablet Debugger and move the pen around the tablet edges/corners. Enter the largest raw Y value shown; use the full digitizer maximum, not the active-area height.")]
    public float DigitizerMaximumY
    {
        get => digitizerMaximumY;
        set => digitizerMaximumY = float.IsFinite(value) ? Math.Clamp(value, 1f, 1048575f) : 65535f;
    }

    [SliderProperty("Mapping center X (%)", 0f, 100f, 50f)]
    [DefaultPropertyValue(50f)]
    [ToolTip("Horizontal pivot for overall X scaling, as a percentage of the full digitizer range. The default keeps the tablet center fixed while scaling.")]
    public float MappingCenterXPercent
    {
        get => mappingCenterXPercent;
        set => mappingCenterXPercent = float.IsFinite(value) ? Math.Clamp(value, 0f, 100f) : 50f;
    }

    [SliderProperty("Mapping center Y (%)", 0f, 100f, 50f)]
    [DefaultPropertyValue(50f)]
    [ToolTip("Vertical pivot for overall Y scaling, as a percentage of the full digitizer range. The default keeps the tablet center fixed while scaling.")]
    public float MappingCenterYPercent
    {
        get => mappingCenterYPercent;
        set => mappingCenterYPercent = float.IsFinite(value) ? Math.Clamp(value, 0f, 100f) : 50f;
    }

    [SliderProperty("Horizontal map scale", 0.5f, 1.5f, 1f)]
    [DefaultPropertyValue(1f)]
    [ToolTip("Overall X mapping scale around Mapping center X. 1 is unchanged; above 1 expands horizontal travel and below 1 compresses it.")]
    public float HorizontalScale
    {
        get => horizontalScale;
        set => horizontalScale = float.IsFinite(value) ? Math.Clamp(value, 0.5f, 1.5f) : 1f;
    }

    [SliderProperty("Vertical map scale", 0.5f, 1.5f, 1f)]
    [DefaultPropertyValue(1f)]
    [ToolTip("Overall Y mapping scale around Mapping center Y. 1 is unchanged; above 1 expands vertical travel and below 1 compresses it.")]
    public float VerticalScale
    {
        get => verticalScale;
        set => verticalScale = float.IsFinite(value) ? Math.Clamp(value, 0.5f, 1.5f) : 1f;
    }

    [Property("Horizontal correction grid (25 values)")]
    [DefaultPropertyValue(NeutralCorrectionSamples)]
    [ToolTip("Comma-separated horizontal output corrections as percentages of full digitizer width. Enter 25 values in row-major order: X=0,25,50,75,100% across each Y row (Y=0,25,50,75,100%). Values are clamped to +/-10%. Use this field to compensate for horizontal drift that varies by tablet position.")]
    public string HorizontalCorrectionSamples
    {
        get => horizontalCorrectionSamples;
        set
        {
            horizontalCorrectionSamples = value ?? NeutralCorrectionSamples;
            parsedHorizontalCorrections = TabletSpaceWarpMath.ParseCorrectionSamples(horizontalCorrectionSamples);
        }
    }

    [Property("Vertical correction grid (25 values)")]
    [DefaultPropertyValue(NeutralCorrectionSamples)]
    [ToolTip("Comma-separated vertical output corrections as fractions of full digitizer height, in 25-value row-major order. After calibration, copy the values after VerticalCorrectionSamples= from the OTD daemon console and paste them here to update the visible tablet profile. Calibration also saves and reloads them automatically for this tablet.")]
    public string VerticalCorrectionSamples
    {
        get => verticalCorrectionSamples;
        set
        {
            verticalCorrectionSamples = value ?? NeutralCorrectionSamples;
            parsedVerticalCorrections = TabletSpaceWarpMath.ParseCorrectionSamples(verticalCorrectionSamples);
        }
    }

    [SliderProperty("Calibration swipes required", 2f, 8f, 3f)]
    [DefaultPropertyValue(3f)]
    [ToolTip("Number of separate tip-down horizontal swipes to average. Lift the pen between swipes. Each accepted swipe must span at least 40% of the digitizer width.")]
    public float CalibrationSwipeCount
    {
        get => calibrationSwipeCount;
        set => calibrationSwipeCount = float.IsFinite(value) ? Math.Clamp(MathF.Round(value), 2f, 8f) : 3f;
    }

    [SliderProperty("Calibration contact pressure", 0f, 65535f, 1f)]
    [DefaultPropertyValue(1f)]
    [ToolTip("Minimum raw pen pressure recorded as a tip-down calibration stroke. Increase it if hover movement is being captured; pressure units depend on the tablet.")]
    public float CalibrationContactPressure
    {
        get => calibrationContactPressure;
        set => calibrationContactPressure = float.IsFinite(value) ? Math.Clamp(value, 0f, 65535f) : 1f;
    }

    public void Consume(IDeviceReport report)
    {
        if (Tablet != null && TabletMappingCalibrationSignal.TryTakeToggle(Tablet))
            ToggleCalibration();

        if (isCalibrating && report is ITabletReport tabletReport)
            ProcessCalibrationInput(tabletReport);

        if (report is IAbsolutePositionReport positionReport)
        {
            positionReport.Position = TabletSpaceWarpMath.Map(
                positionReport.Position,
                DigitizerMaximumX,
                DigitizerMaximumY,
                MappingCenterXPercent / 100f,
                MappingCenterYPercent / 100f,
                HorizontalScale,
                VerticalScale,
                parsedHorizontalCorrections,
                parsedVerticalCorrections);
        }

        Emit?.Invoke(report);
    }

    private void ProcessCalibrationInput(ITabletReport report)
    {
        if (report.Pressure >= CalibrationContactPressure)
        {
            activeCalibrationStroke ??= new List<Vector2>();
            if (activeCalibrationStroke.Count < 2048)
                activeCalibrationStroke.Add(report.Position);
        }
        else
        {
            FinishActiveStroke();
        }
    }

    private void ToggleCalibration()
    {
        if (isCalibrating)
            CompleteCalibration();
        else
            BeginCalibration();
    }

    private void BeginCalibration()
    {
        calibrationStrokes.Clear();
        activeCalibrationStroke = null;
        isCalibrating = true;
        TabletMappingCalibrationSignal.WriteConsoleDiagnostic(
            $"Calibration started for '{Tablet?.Properties.Name ?? "unknown tablet"}'. Draw {CalibrationSwipeCount:0} horizontal swipes, lift between them, then press the assigned tablet button again.");
        OpenTabletDriver.Plugin.Log.WriteNotify(
            "SpatialAim Tablet Mapping",
            $"Calibration armed. Draw {CalibrationSwipeCount:0} horizontal tip-down swipes, lifting between each, then press the assigned tablet button again.");
    }

    private void CompleteCalibration()
    {
        FinishActiveStroke();
        isCalibrating = false;

        var strokes = calibrationStrokes
            .Select(stroke => (IReadOnlyList<Vector2>)stroke)
            .ToArray();
        if (!TabletSpaceWarpMath.TryCalibrateHorizontalSwipes(
                strokes,
                DigitizerMaximumX,
                DigitizerMaximumY,
                (int)CalibrationSwipeCount,
                out var corrections))
        {
            TabletMappingCalibrationSignal.WriteConsoleDiagnostic(
                $"Calibration rejected for '{Tablet?.Properties.Name ?? "unknown tablet"}': collected {calibrationStrokes.Count} strokes; {CalibrationSwipeCount:0} valid swipes required. Existing mapping was kept.");
            OpenTabletDriver.Plugin.Log.WriteNotify(
                "SpatialAim Tablet Mapping",
                $"Calibration needs {CalibrationSwipeCount:0} valid swipes. No mapping was changed; press the assigned tablet button to try again.",
                OpenTabletDriver.Plugin.LogLevel.Warning);
            return;
        }

        var correctionGrid = string.Join(
            ", ",
            corrections.Select(value => value.ToString("0.0000", CultureInfo.InvariantCulture)));
        VerticalCorrectionSamples = correctionGrid;
        var saved = Tablet != null && TabletMappingCalibrationStore.TrySave(
            TabletMappingCalibrationStore.GetTabletKey(Tablet),
            correctionGrid);
        var storagePath = Tablet == null
            ? "<no tablet reference>"
            : TabletMappingCalibrationStore.GetStoragePath(TabletMappingCalibrationStore.GetTabletKey(Tablet));
        TabletMappingCalibrationSignal.WriteConsoleDiagnostic(
            $"Calibration finished for '{Tablet?.Properties.Name ?? "unknown tablet"}'. Saved={saved}; file='{storagePath}'; VerticalCorrectionSamples={correctionGrid}");
        OpenTabletDriver.Plugin.Log.WriteNotify(
            "SpatialAim Tablet Mapping",
            saved
                ? "Calibration applied and saved automatically for this tablet. See the OTD daemon console for the grid."
                : "Calibration applied for this session, but could not be saved automatically. See the OTD daemon console/log for details.");
    }

    private void FinishActiveStroke()
    {
        if (activeCalibrationStroke is { Count: >= 2 })
            calibrationStrokes.Add(activeCalibrationStroke);

        activeCalibrationStroke = null;
    }
}