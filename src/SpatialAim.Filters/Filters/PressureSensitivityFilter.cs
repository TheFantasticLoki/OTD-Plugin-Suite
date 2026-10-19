using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;
using SpatialAim.Filters.Curves;
using SpatialAim.Filters.Localization;

namespace SpatialAim.Filters.Filters;

[PluginName(LocalizedPluginNames.Pressure)]
public sealed class PressureSensitivityFilter : MotionFilterBase
{
    private float pressureStart = 500f;
    private float pressureFull = 8191f;
    private float minimumGain = 0.75f;
    private float maximumGain = 2.5f;
    private float responseExponent = 1f;

    [LocalizedSliderProperty("pressure.start", 0f, 65535f, 500f)]
    [DefaultPropertyValue(500f)]
    [LocalizedToolTip("pressure.start.tip")]
    public float PressureStart
    {
        get => pressureStart;
        set => pressureStart = float.IsFinite(value) ? Math.Clamp(value, 0f, 65534f) : 500f;
    }

    [LocalizedSliderProperty("pressure.full", 1f, 65535f, 8191f)]
    [DefaultPropertyValue(8191f)]
    [LocalizedToolTip("pressure.full.tip")]
    public float PressureFull
    {
        get => pressureFull;
        set => pressureFull = float.IsFinite(value) ? Math.Clamp(value, 1f, 65535f) : 8191f;
    }

    [LocalizedSliderProperty("pressure.minimum", 0.05f, 5f, 0.75f)]
    [DefaultPropertyValue(0.75f)]
    [LocalizedToolTip("pressure.minimum.tip")]
    public float MinimumGain
    {
        get => minimumGain;
        set => minimumGain = float.IsFinite(value) ? Math.Clamp(value, 0.05f, 5f) : 0.75f;
    }

    [LocalizedSliderProperty("pressure.maximum", 0.05f, 10f, 2.5f)]
    [DefaultPropertyValue(2.5f)]
    [LocalizedToolTip("pressure.maximum.tip")]
    public float MaximumGain
    {
        get => maximumGain;
        set => maximumGain = float.IsFinite(value) ? Math.Clamp(value, 0.05f, 10f) : 2.5f;
    }

    [LocalizedSliderProperty("pressure.exponent", 0.25f, 4f, 1f)]
    [DefaultPropertyValue(1f)]
    [LocalizedToolTip("pressure.exponent.tip")]
    public float ResponseExponent
    {
        get => responseExponent;
        set => responseExponent = float.IsFinite(value) ? Math.Clamp(value, 0.25f, 4f) : 1f;
    }

    [LocalizedBooleanProperty("pressure.reverse", "")]
    [DefaultPropertyValue(false)]
    [LocalizedToolTip("pressure.reverse.tip")]
    public bool ReversePressureResponse { get; set; }

    protected override void TransformPosition(
        IAbsolutePositionReport positionReport,
        IDeviceReport deviceReport,
        float elapsedSeconds,
        float speedOutputUnitsPerSecond)
    {
        if (deviceReport is not ITabletReport tabletReport)
            return;

        var gain = CurveMath.PressureGain(
            tabletReport.Pressure,
            PressureStart,
            PressureFull,
            MinimumGain,
            MaximumGain,
            ResponseExponent,
            ReversePressureResponse);

        positionReport.Position *= gain;
    }
}