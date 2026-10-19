using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;
using SpatialAim.Filters.Curves;
using SpatialAim.Filters.Localization;

namespace SpatialAim.Filters.Filters;

[PluginName(LocalizedPluginNames.Flick)]
public sealed class FlickAccelerationFilter : MotionFilterBase
{
    private float flickStartSpeed = 650f;
    private float fullBoostSpeed = 3500f;
    private float precisionGain = 0.85f;
    private float maximumGain = 4f;
    private float responseExponent = 0.8f;
    private float speedSmoothingMilliseconds = 2f;

    [LocalizedSliderProperty("flick.start", 0f, 8000f, 650f)]
    [DefaultPropertyValue(650f)]
    [LocalizedToolTip("flick.start.tip")]
    public float FlickStartSpeed
    {
        get => flickStartSpeed;
        set => flickStartSpeed = float.IsFinite(value) ? Math.Clamp(value, 0f, 8000f) : 650f;
    }

    [LocalizedSliderProperty("flick.full", 1f, 12000f, 3500f)]
    [DefaultPropertyValue(3500f)]
    [LocalizedToolTip("flick.full.tip")]
    public float FullBoostSpeed
    {
        get => fullBoostSpeed;
        set => fullBoostSpeed = float.IsFinite(value) ? Math.Clamp(value, 1f, 12000f) : 3500f;
    }

    [LocalizedSliderProperty("flick.precision", 0.25f, 2f, 0.85f)]
    [DefaultPropertyValue(0.85f)]
    [LocalizedToolTip("flick.precision.tip")]
    public float PrecisionGain
    {
        get => precisionGain;
        set => precisionGain = float.IsFinite(value) ? Math.Clamp(value, 0.25f, 2f) : 0.85f;
    }

    [LocalizedSliderProperty("flick.maximum", 1f, 10f, 4f)]
    [DefaultPropertyValue(4f)]
    [LocalizedToolTip("flick.maximum.tip")]
    public float MaximumGain
    {
        get => maximumGain;
        set => maximumGain = float.IsFinite(value) ? Math.Clamp(value, 1f, 10f) : 4f;
    }

    [LocalizedSliderProperty("flick.exponent", 0.25f, 4f, 0.8f)]
    [DefaultPropertyValue(0.8f)]
    [LocalizedToolTip("flick.exponent.tip")]
    public float ResponseExponent
    {
        get => responseExponent;
        set => responseExponent = float.IsFinite(value) ? Math.Clamp(value, 0.25f, 4f) : 0.8f;
    }

    [LocalizedSliderProperty("speed.smoothing", 0f, 50f, 2f)]
    [DefaultPropertyValue(2f)]
    [LocalizedToolTip("flick.smoothing.tip")]
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