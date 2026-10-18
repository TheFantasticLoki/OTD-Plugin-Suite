using System.Diagnostics;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;

namespace SpatialAim.Filters.Filters;

public abstract class MotionFilterBase : IPositionedPipelineElement<IDeviceReport>
{
    private long lastTimestamp;
    private float smoothedSpeed;

    public event Action<IDeviceReport>? Emit;

    public PipelinePosition Position => PipelinePosition.PostTransform;

    protected virtual float SpeedSmoothingTimeConstantMs => 0f;

    public void Consume(IDeviceReport report)
    {
        if (report is OutOfRangeReport)
        {
            lastTimestamp = 0;
            smoothedSpeed = 0f;
            Emit?.Invoke(report);
            return;
        }

        if (report is IAbsolutePositionReport positionReport)
        {
            var timestamp = Stopwatch.GetTimestamp();
            var elapsedSeconds = lastTimestamp == 0
                ? 0f
                : (float)Stopwatch.GetElapsedTime(lastTimestamp, timestamp).TotalSeconds;
            var rawSpeed = elapsedSeconds is > 0f and <= 0.25f
                ? positionReport.Position.Length() / elapsedSeconds
                : 0f;

            lastTimestamp = timestamp;
            smoothedSpeed = SmoothSpeed(rawSpeed, elapsedSeconds, SpeedSmoothingTimeConstantMs);
            TransformPosition(positionReport, report, elapsedSeconds, smoothedSpeed);
        }

        Emit?.Invoke(report);
    }

    protected abstract void TransformPosition(
        IAbsolutePositionReport positionReport,
        IDeviceReport deviceReport,
        float elapsedSeconds,
        float speedOutputUnitsPerSecond);

    private float SmoothSpeed(float speed, float elapsedSeconds, float smoothingMilliseconds)
    {
        if (smoothingMilliseconds <= 0f || elapsedSeconds <= 0f)
            return speed;

        var timeConstant = smoothingMilliseconds / 1000f;
        var blend = 1f - MathF.Exp(-elapsedSeconds / timeConstant);
        return smoothedSpeed + blend * (speed - smoothedSpeed);
    }
}