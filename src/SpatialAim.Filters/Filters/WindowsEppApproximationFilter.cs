using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;
using SpatialAim.Filters.Curves;

namespace SpatialAim.Filters.Filters;

[PluginName("SpatialAim Windows EPP Approximation")]
public sealed class WindowsEppApproximationFilter : MotionFilterBase
{
    private float accelerationStartSpeed = 250f;
    private float accelerationFullSpeed = 1800f;
    private float maximumGain = 2.5f;
    private float responseExponent = 1.4f;
    private float speedSmoothingMilliseconds = 2f;

    [SliderProperty("Acceleration start speed", 0f, 8000f, 250f)]
    [DefaultPropertyValue(250f)]
    [ToolTip("Relative output units per second below which this filter stays at 1x gain. Raise it to preserve a wider precision-speed band.")]
    public float AccelerationStartSpeed
    {
        get => accelerationStartSpeed;
        set => accelerationStartSpeed = float.IsFinite(value) ? Math.Clamp(value, 0f, 8000f) : 250f;
    }

    [SliderProperty("Full acceleration speed", 1f, 12000f, 1800f)]
    [DefaultPropertyValue(1800f)]
    [ToolTip("Speed at which Maximum gain is reached. Keep this above Acceleration start speed; the filter safely handles equal or reversed values.")]
    public float AccelerationFullSpeed
    {
        get => accelerationFullSpeed;
        set => accelerationFullSpeed = float.IsFinite(value) ? Math.Clamp(value, 1f, 12000f) : 1800f;
    }

    [SliderProperty("Maximum gain", 1f, 8f, 2.5f)]
    [DefaultPropertyValue(2.5f)]
    [ToolTip("Upper movement multiplier. Start conservatively; a multiplier of 2 means a report delta is doubled once full acceleration speed is reached.")]
    public float MaximumGain
    {
        get => maximumGain;
        set => maximumGain = float.IsFinite(value) ? Math.Clamp(value, 1f, 8f) : 2.5f;
    }

    [SliderProperty("Response exponent", 0.25f, 4f, 1.4f)]
    [DefaultPropertyValue(1.4f)]
    [ToolTip("Controls how gain rises between the two speed limits. 1 is linear, below 1 accelerates earlier, and above 1 delays most of the gain until faster movement.")]
    public float ResponseExponent
    {
        get => responseExponent;
        set => responseExponent = float.IsFinite(value) ? Math.Clamp(value, 0.25f, 4f) : 1.4f;
    }

    [SliderProperty("Speed smoothing (ms)", 0f, 50f, 2f)]
    [DefaultPropertyValue(2f)]
    [ToolTip("Low-pass time constant for report-to-report speed changes. 0 disables smoothing; higher values reduce gain jitter but add ramp-up and ramp-down delay.")]
    public float SpeedSmoothingMilliseconds
    {
        get => speedSmoothingMilliseconds;
        set => speedSmoothingMilliseconds = float.IsFinite(value) ? Math.Clamp(value, 0f, 50f) : 2f;
    }

    protected override float SpeedSmoothingTimeConstantMs => speedSmoothingMilliseconds;

    protected override void TransformPosition(
        IAbsolutePositionReport positionReport,
        IDeviceReport deviceReport,
        float elapsedSeconds,
        float speedOutputUnitsPerSecond)
    {
        var gain = CurveMath.SpeedGain(
            speedOutputUnitsPerSecond,
            AccelerationStartSpeed,
            AccelerationFullSpeed,
            1f,
            MaximumGain,
            ResponseExponent);

        positionReport.Position *= gain;
    }
}