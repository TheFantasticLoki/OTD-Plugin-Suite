using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;
using SpatialAim.Filters.Curves;

namespace SpatialAim.Filters.Filters;

[PluginName("SpatialAim Pressure Sensitivity")]
public sealed class PressureSensitivityFilter : MotionFilterBase
{
    private float pressureStart = 500f;
    private float pressureFull = 8191f;
    private float minimumGain = 0.75f;
    private float maximumGain = 2.5f;
    private float responseExponent = 1f;

    [SliderProperty("Pressure start", 0f, 65535f, 500f)]
    [DefaultPropertyValue(500f)]
    [ToolTip("Raw pen pressure where the gain begins moving away from Minimum gain. Check your tablet's values in OTD's Tablet Debugger; pressure units vary by device.")]
    public float PressureStart
    {
        get => pressureStart;
        set => pressureStart = float.IsFinite(value) ? Math.Clamp(value, 0f, 65534f) : 500f;
    }

    [SliderProperty("Pressure full scale", 1f, 65535f, 8191f)]
    [DefaultPropertyValue(8191f)]
    [ToolTip("Raw pressure at which Maximum gain is reached. If this is not greater than Pressure start, the response uses a one-unit transition.")]
    public float PressureFull
    {
        get => pressureFull;
        set => pressureFull = float.IsFinite(value) ? Math.Clamp(value, 1f, 65535f) : 8191f;
    }

    [SliderProperty("Minimum gain", 0.05f, 5f, 0.75f)]
    [DefaultPropertyValue(0.75f)]
    [ToolTip("Movement multiplier at the low end of the pressure response. Values below 1 reduce movement; values above 1 increase it.")]
    public float MinimumGain
    {
        get => minimumGain;
        set => minimumGain = float.IsFinite(value) ? Math.Clamp(value, 0.05f, 5f) : 0.75f;
    }

    [SliderProperty("Maximum gain", 0.05f, 10f, 2.5f)]
    [DefaultPropertyValue(2.5f)]
    [ToolTip("Movement multiplier at full pressure. This changes tablet movement only; it does not rewrite the pressure value sent to the game.")]
    public float MaximumGain
    {
        get => maximumGain;
        set => maximumGain = float.IsFinite(value) ? Math.Clamp(value, 0.05f, 10f) : 2.5f;
    }

    [SliderProperty("Response exponent", 0.25f, 4f, 1f)]
    [DefaultPropertyValue(1f)]
    [ToolTip("Shapes the pressure ramp. 1 is linear, below 1 reaches higher gains earlier, and above 1 keeps low-pressure gain longer.")]
    public float ResponseExponent
    {
        get => responseExponent;
        set => responseExponent = float.IsFinite(value) ? Math.Clamp(value, 0.25f, 4f) : 1f;
    }

    [BooleanProperty("Reverse pressure response", "")]
    [DefaultPropertyValue(false)]
    [ToolTip("When enabled, high pressure uses Minimum gain and light pressure moves toward Maximum gain. Useful when a firm press should stabilize aim.")]
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