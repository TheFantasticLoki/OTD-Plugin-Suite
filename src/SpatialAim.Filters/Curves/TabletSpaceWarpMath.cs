using System.Globalization;
using System.Numerics;

namespace SpatialAim.Filters.Curves;

public static class TabletSpaceWarpMath
{
    public const int GridSide = 5;
    public const int GridValueCount = GridSide * GridSide;
    public const float MaximumCorrectionFraction = 0.1f;

    public static Vector2 Map(
        Vector2 position,
        float digitizerMaximumX,
        float digitizerMaximumY,
        float centerXFraction,
        float centerYFraction,
        float scaleX,
        float scaleY,
        IReadOnlyList<float> correctionX,
        IReadOnlyList<float> correctionY)
    {
        if (Math.Clamp(scaleX, 0.5f, 1.5f) == 1f &&
            Math.Clamp(scaleY, 0.5f, 1.5f) == 1f &&
            IsNeutral(correctionX) && IsNeutral(correctionY))
        {
            return position;
        }

        var maximumX = Math.Max(digitizerMaximumX, 1f);
        var maximumY = Math.Max(digitizerMaximumY, 1f);
        var inputX = position.X / maximumX;
        var inputY = position.Y / maximumY;
        var centerX = Math.Clamp(centerXFraction, 0f, 1f);
        var centerY = Math.Clamp(centerYFraction, 0f, 1f);
        var xOffset = SampleGrid(correctionX, inputX, inputY);
        var yOffset = SampleGrid(correctionY, inputX, inputY);
        var mappedX = centerX + (inputX - centerX) * Math.Clamp(scaleX, 0.5f, 1.5f) + xOffset;
        var mappedY = centerY + (inputY - centerY) * Math.Clamp(scaleY, 0.5f, 1.5f) + yOffset;

        return new Vector2(mappedX * maximumX, mappedY * maximumY);
    }

    public static float[] ParseCorrectionSamples(string? value)
    {
        TryParseCorrectionSamples(value, out var samples);
        return samples;
    }

    public static bool TryParseCorrectionSamples(string? value, out float[] samples)
    {
        samples = new float[GridValueCount];
        var tokens = (value ?? string.Empty).Split(
            new[] { ',', ';', ' ', '\r', '\n', '\t' },
            StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length != GridValueCount)
            return false;

        for (var index = 0; index < tokens.Length; index++)
        {
            if (!float.TryParse(tokens[index], NumberStyles.Float, CultureInfo.InvariantCulture, out var sample) ||
                !float.IsFinite(sample))
            {
                samples = new float[GridValueCount];
                return false;
            }

            samples[index] = Math.Clamp(sample, -MaximumCorrectionFraction, MaximumCorrectionFraction);
        }

        return true;
    }

    public static bool TryCalibrateHorizontalSwipes(
        IReadOnlyList<IReadOnlyList<Vector2>> strokes,
        float digitizerMaximumX,
        float digitizerMaximumY,
        int requiredSwipes,
        out float[] verticalCorrections)
    {
        verticalCorrections = new float[GridValueCount];
        var maximumX = Math.Max(digitizerMaximumX, 1f);
        var maximumY = Math.Max(digitizerMaximumY, 1f);
        var gridSums = new float[GridSide];
        var gridCounts = new int[GridSide];
        var validSwipeCount = 0;

        foreach (var stroke in strokes)
        {
            if (stroke.Count < 8)
                continue;

            var start = stroke[0];
            var end = stroke[^1];
            if (MathF.Abs(end.X - start.X) < maximumX * 0.4f)
                continue;

            validSwipeCount++;
            for (var column = 0; column < GridSide; column++)
            {
                var targetX = maximumX * column / (GridSide - 1);
                var lowerX = Math.Min(start.X, end.X);
                var upperX = Math.Max(start.X, end.X);
                if (targetX < lowerX || targetX > upperX ||
                    !TryGetYAtX(stroke, targetX, out var actualY))
                {
                    continue;
                }

                var progress = (targetX - start.X) / (end.X - start.X);
                var expectedY = start.Y + (end.Y - start.Y) * progress;
                gridSums[column] += (expectedY - actualY) / maximumY;
                gridCounts[column]++;
            }
        }

        if (validSwipeCount < Math.Max(requiredSwipes, 1))
            return false;

        var correctionByColumn = new float[GridSide];
        for (var column = 0; column < GridSide; column++)
        {
            if (gridCounts[column] > 0)
                correctionByColumn[column] = Math.Clamp(
                    gridSums[column] / gridCounts[column],
                    -MaximumCorrectionFraction,
                    MaximumCorrectionFraction);
        }

        for (var row = 0; row < GridSide; row++)
            Array.Copy(correctionByColumn, 0, verticalCorrections, row * GridSide, GridSide);

        return true;
    }

    private static float SampleGrid(IReadOnlyList<float> samples, float x, float y)
    {
        if (samples.Count != GridValueCount)
            return 0f;

        var gridX = Math.Clamp(x, 0f, 1f) * (GridSide - 1);
        var gridY = Math.Clamp(y, 0f, 1f) * (GridSide - 1);
        var left = (int)gridX;
        var top = (int)gridY;
        var right = Math.Min(left + 1, GridSide - 1);
        var bottom = Math.Min(top + 1, GridSide - 1);
        var horizontalBlend = gridX - left;
        var verticalBlend = gridY - top;
        var topValue = Lerp(samples[top * GridSide + left], samples[top * GridSide + right], horizontalBlend);
        var bottomValue = Lerp(samples[bottom * GridSide + left], samples[bottom * GridSide + right], horizontalBlend);

        return Lerp(topValue, bottomValue, verticalBlend);
    }

    private static float Lerp(float start, float end, float amount) => start + (end - start) * amount;

    private static bool IsNeutral(IReadOnlyList<float> samples)
    {
        for (var index = 0; index < samples.Count; index++)
            if (samples[index] != 0f)
                return false;

        return true;
    }

    private static bool TryGetYAtX(IReadOnlyList<Vector2> stroke, float targetX, out float y)
    {
        for (var index = 1; index < stroke.Count; index++)
        {
            var start = stroke[index - 1];
            var end = stroke[index];
            var minimumX = Math.Min(start.X, end.X);
            var maximumX = Math.Max(start.X, end.X);
            if (targetX < minimumX || targetX > maximumX)
                continue;

            var width = end.X - start.X;
            if (MathF.Abs(width) < 0.001f)
                continue;

            var amount = (targetX - start.X) / width;
            y = start.Y + (end.Y - start.Y) * amount;
            return true;
        }

        y = 0f;
        return false;
    }
}