using System.Reflection;
using OpenTabletDriver.Plugin.Attributes;
using SpatialAim.Filters.Filters;
using SpatialAim.Filters.Localization;
using Xunit;

namespace SpatialAim.Filters.Tests;

public sealed class LocalizationTests
{
    [Theory]
    [InlineData("en", "Pressure start")]
    [InlineData("es", "Presión inicial")]
    [InlineData("ja", "筆圧開始値")]
    [InlineData("zh-CN", "压力起始值")]
    [InlineData("ko", "필압 시작값")]
    [InlineData("de", "Druckbeginn")]
    [InlineData("pl", "Początek nacisku")]
    [InlineData("fr", "Début de pression")]
    public void CatalogReturnsPressureLabelForEachLocale(string locale, string expected)
    {
        Assert.Equal(expected, LocalizedText.Get("pressure.start", locale));
    }

    [Fact]
    public void LocalizedAttributesRemainVisibleThroughOtdBaseTypes()
    {
        var property = typeof(PressureSensitivityFilter).GetProperty(nameof(PressureSensitivityFilter.PressureStart));
        var slider = property?.GetCustomAttribute<SliderPropertyAttribute>();
        var toolTip = property?.GetCustomAttribute<ToolTipAttribute>();

        Assert.Equal(LocalizedText.Get("pressure.start"), slider?.DisplayName);
        Assert.Equal(LocalizedText.Get("pressure.start.tip"), toolTip?.ToolTip);
    }

    [Fact]
    public void MissingLocaleFallsBackToEnglish()
    {
        Assert.Equal("Pressure start", LocalizedText.Get("pressure.start", "unknown"));
    }
}