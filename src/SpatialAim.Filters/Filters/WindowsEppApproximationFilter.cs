using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;
using SpatialAim.Filters.Curves;
using SpatialAim.Filters.Localization;

namespace SpatialAim.Filters.Filters;

[PluginName(LocalizedPluginNames.Epp)]
public sealed class WindowsEppApproximationFilter : MotionFilterBase
{
    private float accelerationStartSpeed = 250f;
    private float accelerationFullSpeed = 1800f;
    private float maximumGain = 2.5f;
    private float responseExponent = 1.4f;
    private float speedSmoothingMilliseconds = 2f;

    [LocalizedSliderProperty("epp.start", 0f, 8000f, 250f)]
    [DefaultPropertyValue(250f)]
    [LocalizedToolTip("epp.start.tip")]
    public float AccelerationStartSpeed
    {
        get => accelerationStartSpeed;
        set => accelerationStartSpeed = float.IsFinite(value) ? Math.Clamp(value, 0f, 8000f) : 250f;
    }

    [LocalizedSliderProperty("epp.full", 1f, 12000f, 1800f)]
    [DefaultPropertyValue(1800f)]
    [LocalizedToolTip("epp.full.tip")]
    public float AccelerationFullSpeed
    {
        get => accelerationFullSpeed;
        set => accelerationFullSpeed = float.IsFinite(value) ? Math.Clamp(value, 1f, 12000f) : 1800f;
    }

    [LocalizedSliderProperty("epp.maximum", 1f, 8f, 2.5f)]
    [DefaultPropertyValue(2.5f)]
    [LocalizedToolTip("epp.maximum.tip")]
    public float MaximumGain
    {
        get => maximumGain;
        set => maximumGain = float.IsFinite(value) ? Math.Clamp(value, 1f, 8f) : 2.5f;
    }

    [LocalizedSliderProperty("epp.exponent", 0.25f, 4f, 1.4f)]
    [DefaultPropertyValue(1.4f)]
    [LocalizedToolTip("epp.exponent.tip")]
    public float ResponseExponent
    {
        get => responseExponent;
        set => responseExponent = float.IsFinite(value) ? Math.Clamp(value, 0.25f, 4f) : 1.4f;
    }

    [LocalizedSliderProperty("speed.smoothing", 0f, 50f, 2f)]
    [DefaultPropertyValue(2f)]
    [LocalizedToolTip("epp.smoothing.tip")]
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