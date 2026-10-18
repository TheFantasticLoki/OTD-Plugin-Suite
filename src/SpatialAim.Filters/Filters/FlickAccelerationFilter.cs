using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;
using SpatialAim.Filters.Curves;

namespace SpatialAim.Filters.Filters;

[PluginName("SpatialAim Flick Acceleration")]
public sealed class FlickAccelerationFilter : MotionFilterBase
{
    private float flickStartSpeed = 650f;
    private float fullBoostSpeed = 3500f;
    private float precisionGain = 0.85f;
    private float maximumGain = 4f;
    private float responseExponent = 0.8f;
    private float speedSmoothingMilliseconds = 2f;

    [SliderProperty("Flick start speed", 0f, 8000f, 650f)]
    [DefaultPropertyValue(650f)]
    [ToolTip("Relative output units per second where the multiplier starts moving from Precision gain toward Maximum gain.")]
    public float FlickStartSpeed
    {
        get => flickStartSpeed;
        set => flickStartSpeed = float.IsFinite(value) ? Math.Clamp(value, 0f, 8000f) : 650f;
    }

    [SliderProperty("Full boost speed", 1f, 12000f, 3500f)]
    [DefaultPropertyValue(3500f)]
    [ToolTip("Speed at which the full flick multiplier is reached. Lower this for earlier turning acceleration; raise it to reserve the strongest boost for very fast flicks.")]
    public float FullBoostSpeed
    {
        get => fullBoostSpeed;
        set => fullBoostSpeed = float.IsFinite(value) ? Math.Clamp(value, 1f, 12000f) : 3500f;
    }

    [SliderProperty("Precision gain", 0.25f, 2f, 0.85f)]
    [DefaultPropertyValue(0.85f)]
    [ToolTip("Multiplier below Flick start speed. Values below 1 favor fine aim; values above 1 keep slow movement responsive.")]
    public float PrecisionGain
    {
        get => precisionGain;
        set => precisionGain = float.IsFinite(value) ? Math.Clamp(value, 0.25f, 2f) : 0.85f;
    }

    [SliderProperty("Maximum flick gain", 1f, 10f, 4f)]
    [DefaultPropertyValue(4f)]
    [ToolTip("Multiplier at and above Full boost speed. This is a movement multiplier, not a guaranteed turn angle; the game sensitivity and available tablet travel still determine the resulting turn.")]
    public float MaximumGain
    {
        get => maximumGain;
        set => maximumGain = float.IsFinite(value) ? Math.Clamp(value, 1f, 10f) : 4f;
    }

    [SliderProperty("Response exponent", 0.25f, 4f, 0.8f)]
    [DefaultPropertyValue(0.8f)]
    [ToolTip("Shapes the boost ramp. Below 1 gives stronger acceleration soon after Flick start speed; above 1 keeps most of the boost for the fastest motion.")]
    public float ResponseExponent
    {
        get => responseExponent;
        set => responseExponent = float.IsFinite(value) ? Math.Clamp(value, 0.25f, 4f) : 0.8f;
    }

    [SliderProperty("Speed smoothing (ms)", 0f, 50f, 2f)]
    [DefaultPropertyValue(2f)]
    [ToolTip("Low-pass time constant for measured report speed. 0 reacts immediately; a small value can steady the boost without making fast flicks feel delayed.")]
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
            FlickStartSpeed,
            FullBoostSpeed,
            PrecisionGain,
            MaximumGain,
            ResponseExponent);

        positionReport.Position *= gain;
    }
}