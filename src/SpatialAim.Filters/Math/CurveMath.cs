namespace SpatialAim.Filters.Curves;

public static class CurveMath
{
    public static float PressureGain(
        uint pressure,
        float pressureStart,
        float pressureFull,
        float minimumGain,
        float maximumGain,
        float exponent,
        bool reverse)
    {
        var start = Math.Clamp(pressureStart, 0f, 65534f);
        var full = Math.Max(pressureFull, start + 1f);
        var normalized = Math.Clamp((pressure - start) / (full - start), 0f, 1f);

        if (reverse)
            normalized = 1f - normalized;

        return Lerp(minimumGain, maximumGain, MathF.Pow(normalized, Math.Clamp(exponent, 0.25f, 4f)));
    }

    public static float SpeedGain(
        float speed,
        float speedStart,
        float speedFull,
        float minimumGain,
        float maximumGain,
        float exponent)
    {
        var start = Math.Clamp(speedStart, 0f, 11999f);
        var full = Math.Max(speedFull, start + 1f);
        var normalized = Math.Clamp((speed - start) / (full - start), 0f, 1f);
        var shaped = MathF.Pow(normalized, Math.Clamp(exponent, 0.25f, 4f));

        return Lerp(minimumGain, maximumGain, shaped);
    }

    private static float Lerp(float minimum, float maximum, float amount) =>
        minimum + (maximum - minimum) * amount;
}