using System.Reflection;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using SpatialAim.Filters.Curves;
using SpatialAim.Filters.Filters;
using SpatialAim.Filters.Localization;
using System.Numerics;
using Xunit;

namespace SpatialAim.Filters.Tests;

public sealed class CurveMathTests
{
    [Theory]
    [InlineData(typeof(PressureSensitivityFilter), LocalizedPluginNames.Pressure, PipelinePosition.PostTransform)]
    [InlineData(typeof(WindowsEppApproximationFilter), LocalizedPluginNames.Epp, PipelinePosition.PostTransform)]
    [InlineData(typeof(FlickAccelerationFilter), LocalizedPluginNames.Flick, PipelinePosition.PostTransform)]
    [InlineData(typeof(TabletSpaceWarpFilter), LocalizedPluginNames.Mapping, PipelinePosition.PreTransform)]
    public void FiltersAreNamedAtTheirExpectedPipelineStage(
        Type filterType,
        string expectedName,
        PipelinePosition expectedPosition)
    {
        var pluginName = filterType.GetCustomAttribute<PluginNameAttribute>();
        var filter = Assert.IsAssignableFrom<IPositionedPipelineElement<IDeviceReport>>(
            Activator.CreateInstance(filterType));

        Assert.Equal(expectedName, pluginName?.Name);
        Assert.Equal(expectedPosition, filter.Position);
    }

    [Fact]
    public void CalibrationToggleIsAnOtdStateBinding()
    {
        var bindingType = typeof(TabletMappingCalibrationBinding);
        var pluginName = bindingType.GetCustomAttribute<PluginNameAttribute>();

        Assert.Equal(LocalizedPluginNames.Calibration, pluginName?.Name);
        Assert.IsAssignableFrom<IStateBinding>(Activator.CreateInstance(bindingType));
    }

    [Theory]
    [InlineData(typeof(PressureSensitivityFilter))]
    [InlineData(typeof(WindowsEppApproximationFilter))]
    [InlineData(typeof(FlickAccelerationFilter))]
    [InlineData(typeof(TabletSpaceWarpFilter))]
    public void ConfigurablePropertiesDeclareExplicitDefaults(Type filterType)
    {
        var settings = filterType.GetProperties()
            .Select(property => new
            {
                Property = property,
                Slider = property.GetCustomAttribute<SliderPropertyAttribute>(),
                Boolean = property.GetCustomAttribute<BooleanPropertyAttribute>(),
                Default = property.GetCustomAttribute<DefaultPropertyValueAttribute>()
            })
            .Where(setting => setting.Property.GetCustomAttribute<PropertyAttribute>() is not null)
            .ToArray();

        Assert.NotEmpty(settings);
        Assert.All(settings, setting => Assert.NotNull(setting.Default));

        foreach (var setting in settings)
        {
            if (setting.Slider is not null)
            {
                var defaultValue = Assert.IsType<float>(setting.Default!.Value);
                Assert.Equal(setting.Slider.DefaultValue, defaultValue);

                Assert.NotEqual(0f, defaultValue);
            }
            else if (setting.Boolean is null)
            {
                var defaultValue = Assert.IsType<string>(setting.Default!.Value);
                Assert.Equal(TabletSpaceWarpMath.GridValueCount, TabletSpaceWarpMath.ParseCorrectionSamples(defaultValue).Length);
                Assert.All(TabletSpaceWarpMath.ParseCorrectionSamples(defaultValue), sample => Assert.Equal(0f, sample));
            }
            else
            {
                Assert.IsType<bool>(setting.Default!.Value);
            }
        }
    }

    [Theory]
    [InlineData(0u, 0.75f)]
    [InlineData(500u, 0.75f)]
    [InlineData(8191u, 2.5f)]
    [InlineData(12000u, 2.5f)]
    public void PressureGainClampsToConfiguredEndpoints(uint pressure, float expected)
    {
        var actual = CurveMath.PressureGain(pressure, 500f, 8191f, 0.75f, 2.5f, 1f, false);

        Assert.Equal(expected, actual, precision: 5);
    }

    [Fact]
    public void PressureGainCanReverseResponse()
    {
        var lightPressure = CurveMath.PressureGain(500u, 500f, 8191f, 0.75f, 2.5f, 1f, true);
        var firmPressure = CurveMath.PressureGain(8191u, 500f, 8191f, 0.75f, 2.5f, 1f, true);

        Assert.Equal(2.5f, lightPressure);
        Assert.Equal(0.75f, firmPressure);
    }

    [Fact]
    public void PressureGainHandlesInvalidPressureWindow()
    {
        var actual = CurveMath.PressureGain(101u, 100f, 100f, 1f, 3f, 1f, false);

        Assert.Equal(3f, actual);
    }

    [Theory]
    [InlineData(0f, 1f)]
    [InlineData(100f, 1f)]
    [InlineData(600f, 2f)]
    [InlineData(1100f, 3f)]
    [InlineData(2000f, 3f)]
    public void SpeedGainInterpolatesAndSaturates(float speed, float expected)
    {
        var actual = CurveMath.SpeedGain(speed, 100f, 1100f, 1f, 3f, 1f);

        Assert.Equal(expected, actual, precision: 5);
    }

    [Fact]
    public void SpeedGainExponentDelaysAcceleration()
    {
        var linear = CurveMath.SpeedGain(600f, 100f, 1100f, 1f, 3f, 1f);
        var delayed = CurveMath.SpeedGain(600f, 100f, 1100f, 1f, 3f, 2f);

        Assert.Equal(2f, linear);
        Assert.Equal(1.5f, delayed);
    }

    [Fact]
    public void SpeedGainHandlesEqualThresholds()
    {
        var atThreshold = CurveMath.SpeedGain(100f, 100f, 100f, 1f, 3f, 1f);
        var aboveThreshold = CurveMath.SpeedGain(101f, 100f, 100f, 1f, 3f, 1f);

        Assert.Equal(1f, atThreshold);
        Assert.Equal(3f, aboveThreshold);
    }

    [Fact]
    public void TabletSpaceMapDoesNothingWhenNeutral()
    {
        var position = new Vector2(1200f, 4800f);

        var mapped = TabletSpaceWarpMath.Map(
            position, 12000f, 8000f, 0.5f, 0.5f, 1f, 1f,
            new float[TabletSpaceWarpMath.GridValueCount],
            new float[TabletSpaceWarpMath.GridValueCount]);

        Assert.Equal(position, mapped);
    }

    [Fact]
    public void TabletSpaceMapScalesAroundItsConfiguredCenter()
    {
        var position = new Vector2(9000f, 2000f);

        var mapped = TabletSpaceWarpMath.Map(
            position, 12000f, 8000f, 0.5f, 0.5f, 1.5f, 0.5f,
            new float[TabletSpaceWarpMath.GridValueCount],
            new float[TabletSpaceWarpMath.GridValueCount]);

        Assert.Equal(new Vector2(10500f, 3000f), mapped);
    }

    [Fact]
    public void TabletSpaceMapCanCancelAnNShapedHorizontalPath()
    {
        var position = new Vector2(6000f, 4000f);
        var nShapeCorrection = new[] { 0f, -0.02f, 0.04f, -0.02f, 0f };
        var verticalGrid = Enumerable.Range(0, TabletSpaceWarpMath.GridSide)
            .SelectMany(_ => nShapeCorrection)
            .ToArray();

        var mapped = TabletSpaceWarpMath.Map(
            position, 12000f, 8000f, 0.5f, 0.5f, 1f, 1f,
            new float[TabletSpaceWarpMath.GridValueCount], verticalGrid);

        Assert.Equal(new Vector2(6000f, 4320f), mapped);
    }

    [Fact]
    public void TabletSpaceMapInterpolatesBetweenGridSamples()
    {
        var horizontalGrid = Enumerable.Range(0, TabletSpaceWarpMath.GridSide)
            .SelectMany(_ => new[] { 0f, 0.04f, 0f, 0f, 0f })
            .ToArray();

        var mapped = TabletSpaceWarpMath.Map(
            new Vector2(1500f, 4000f), 12000f, 8000f, 0.5f, 0.5f, 1f, 1f,
            horizontalGrid, new float[TabletSpaceWarpMath.GridValueCount]);

        Assert.Equal(1740f, mapped.X);
    }

    [Fact]
    public void InvalidCorrectionGridFallsBackToNeutral()
    {
        var samples = TabletSpaceWarpMath.ParseCorrectionSamples("0, 0, not-a-number");

        Assert.Equal(TabletSpaceWarpMath.GridValueCount, samples.Length);
        Assert.All(samples, sample => Assert.Equal(0f, sample));
    }

    [Fact]
    public void CorrectionGridClampsEachSample()
    {
        var samples = TabletSpaceWarpMath.ParseCorrectionSamples(
            string.Join(",", Enumerable.Repeat("0.5", TabletSpaceWarpMath.GridValueCount)));

        Assert.All(samples, sample => Assert.Equal(TabletSpaceWarpMath.MaximumCorrectionFraction, sample));
    }

    [Fact]
    public void CalibrationAveragesRepeatedNShapedSwipes()
    {
        var strokes = new IReadOnlyList<Vector2>[]
        {
            CreateNShapedSwipe(1f),
            CreateNShapedSwipe(0.8f),
            CreateNShapedSwipe(1.2f)
        };

        var succeeded = TabletSpaceWarpMath.TryCalibrateHorizontalSwipes(
            strokes, 50800f, 30500f, 3, out var corrections);

        Assert.True(succeeded);
        var expectedByColumn = new[] { 0f, -0.02f, 0.03f, -0.02f, 0f };
        for (var row = 0; row < TabletSpaceWarpMath.GridSide; row++)
        {
            for (var column = 0; column < TabletSpaceWarpMath.GridSide; column++)
            {
                Assert.Equal(expectedByColumn[column], corrections[row * TabletSpaceWarpMath.GridSide + column], precision: 4);
            }
        }
    }

    [Fact]
    public void CalibrationRejectsTooFewValidSwipes()
    {
        var strokes = new IReadOnlyList<Vector2>[] { CreateNShapedSwipe(1f) };

        var succeeded = TabletSpaceWarpMath.TryCalibrateHorizontalSwipes(
            strokes, 50800f, 30500f, 3, out var corrections);

        Assert.False(succeeded);
        Assert.All(corrections, correction => Assert.Equal(0f, correction));
    }

    [Fact]
    public void PenButtonCalibratesAndAppliesTheVerticalGrid()
    {
        var tabletName = $"SpatialAim calibration test {Guid.NewGuid():N}";
        var identifiers = new[] { new DeviceIdentifier { VendorID = 0x1234, ProductID = 0x5678 } };
        var tablet = new TabletReference(
            new TabletConfiguration
            {
                Name = tabletName,
                Specifications = null!,
                DigitizerIdentifiers = identifiers.ToList()
            },
            identifiers);
        var bindingTablet = new TabletReference(tablet.Properties, identifiers);
        var tabletKey = TabletMappingCalibrationStore.GetTabletKey(tablet);
        var binding = new TabletMappingCalibrationBinding();
        try
        {
            var filter = new TabletSpaceWarpFilter
            {
                CalibrationSwipeCount = 3f,
                DigitizerMaximumX = 50800f,
                DigitizerMaximumY = 30500f,
                Tablet = tablet
            };
            binding.Press(bindingTablet, new TestTabletReport(Vector2.Zero, 0u));
            filter.Consume(new TestTabletReport(Vector2.Zero, 0u));

            foreach (var scale in new[] { 1f, 0.8f, 1.2f })
            {
                foreach (var point in CreateNShapedSwipe(scale))
                    filter.Consume(new TestTabletReport(point, 100u));

                filter.Consume(new TestTabletReport(CreateNShapedSwipe(scale)[^1], 0u));
            }

            binding.Press(bindingTablet, new TestTabletReport(Vector2.Zero, 0u));
            filter.Consume(new TestTabletReport(Vector2.Zero, 0u));

            Assert.True(TabletMappingCalibrationStore.TryLoad(tabletKey, out var savedGrid));
            var corrections = TabletSpaceWarpMath.ParseCorrectionSamples(savedGrid);
            Assert.Equal(-0.02f, corrections[1], precision: 4);
            Assert.Equal(0.03f, corrections[2], precision: 4);
            Assert.Equal(-0.02f, corrections[3], precision: 4);

            var reconstructedFilter = new TabletSpaceWarpFilter
            {
                DigitizerMaximumX = 50800f,
                DigitizerMaximumY = 30500f,
                Tablet = tablet
            };
            var output = new TestTabletReport(new Vector2(25400f, 15250f), 0u);
            reconstructedFilter.Consume(output);
            Assert.Equal(16165f, output.Position.Y, precision: 1);
        }
        finally
        {
            Assert.True(TabletMappingCalibrationStore.TryClear(tabletKey));
        }
    }

    private static IReadOnlyList<Vector2> CreateNShapedSwipe(float deviationScale)
    {
        var offsets = new[] { 0f, 0.02f, -0.03f, 0.02f, 0f };
        var points = new List<Vector2>();
        for (var sample = 0; sample <= 40; sample++)
        {
            var progress = sample / 40f;
            var gridPosition = progress * (offsets.Length - 1);
            var left = (int)gridPosition;
            var right = Math.Min(left + 1, offsets.Length - 1);
            var blend = gridPosition - left;
            var deviation = offsets[left] + (offsets[right] - offsets[left]) * blend;
            points.Add(new Vector2(progress * 50800f, (0.5f + deviation * deviationScale) * 30500f));
        }

        return points;
    }

    private sealed class TestTabletReport(Vector2 position, uint pressure) : ITabletReport
    {
        public Vector2 Position { get; set; } = position;
        public uint Pressure { get; set; } = pressure;
        public bool[] PenButtons { get; set; } = new bool[1];
        public byte[] Raw { get; set; } = Array.Empty<byte>();
    }
}