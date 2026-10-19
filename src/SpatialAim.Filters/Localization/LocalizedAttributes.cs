using System.Reflection;
using System.Text.Json;
using OpenTabletDriver.Plugin.Attributes;

namespace SpatialAim.Filters.Localization;

internal static class LocalizedText
{
    private const string ResourceName = "SpatialAim.Filters.Localization.catalog.json";
    private static readonly Lazy<IReadOnlyDictionary<string, Dictionary<string, string>>> catalog =
        new(LoadCatalog);

    public static string Get(string key) => Get(key, BuildLocale);

    internal static string Get(string key, string locale)
    {
        if (catalog.Value.TryGetValue(locale, out var translations) && translations.TryGetValue(key, out var value))
            return value;

        return catalog.Value["en"].GetValueOrDefault(key, key);
    }

    private static string BuildLocale
    {
        get
        {
#if SPATIALAIM_LOCALE_ES
            return "es";
#elif SPATIALAIM_LOCALE_JA
            return "ja";
#elif SPATIALAIM_LOCALE_ZH_CN
            return "zh-CN";
#elif SPATIALAIM_LOCALE_KO
            return "ko";
#elif SPATIALAIM_LOCALE_DE
            return "de";
#elif SPATIALAIM_LOCALE_PL
            return "pl";
#elif SPATIALAIM_LOCALE_FR
            return "fr";
#else
            return "en";
#endif
        }
    }

    private static IReadOnlyDictionary<string, Dictionary<string, string>> LoadCatalog()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Missing localization resource '{ResourceName}'.");

        return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(stream)
            ?? throw new InvalidOperationException("The localization catalog is empty.");
    }
}

internal static class LocalizedPluginNames
{
#if SPATIALAIM_LOCALE_ES
    public const string Pressure = "SpatialAim Sensibilidad a la presión";
    public const string Epp = "SpatialAim Aproximación de EPP de Windows";
    public const string Flick = "SpatialAim Aceleración de giro rápido";
    public const string Mapping = "SpatialAim Mapeo de tableta";
    public const string Calibration = "SpatialAim Alternar calibración";
#elif SPATIALAIM_LOCALE_JA
    public const string Pressure = "SpatialAim 筆圧感度";
    public const string Epp = "SpatialAim Windows EPP 近似";
    public const string Flick = "SpatialAim フリック加速";
    public const string Mapping = "SpatialAim タブレットマッピング";
    public const string Calibration = "SpatialAim キャリブレーション切替";
#elif SPATIALAIM_LOCALE_ZH_CN
    public const string Pressure = "SpatialAim 压力灵敏度";
    public const string Epp = "SpatialAim Windows EPP 近似";
    public const string Flick = "SpatialAim 快速转向加速";
    public const string Mapping = "SpatialAim 数位板映射";
    public const string Calibration = "SpatialAim 切换校准";
#elif SPATIALAIM_LOCALE_KO
    public const string Pressure = "SpatialAim 필압 감도";
    public const string Epp = "SpatialAim Windows EPP 근사";
    public const string Flick = "SpatialAim 빠른 회전 가속";
    public const string Mapping = "SpatialAim 태블릿 매핑";
    public const string Calibration = "SpatialAim 보정 전환";
#elif SPATIALAIM_LOCALE_DE
    public const string Pressure = "SpatialAim Druckempfindlichkeit";
    public const string Epp = "SpatialAim Windows-EPP-Annäherung";
    public const string Flick = "SpatialAim Flick-Beschleunigung";
    public const string Mapping = "SpatialAim Tablet-Zuordnung";
    public const string Calibration = "SpatialAim Kalibrierung umschalten";
#elif SPATIALAIM_LOCALE_PL
    public const string Pressure = "SpatialAim Czułość nacisku";
    public const string Epp = "SpatialAim Przybliżenie Windows EPP";
    public const string Flick = "SpatialAim Przyspieszenie szybkiego obrotu";
    public const string Mapping = "SpatialAim Mapowanie tabletu";
    public const string Calibration = "SpatialAim Przełącz kalibrację";
#elif SPATIALAIM_LOCALE_FR
    public const string Pressure = "SpatialAim Sensibilité à la pression";
    public const string Epp = "SpatialAim Approximation EPP de Windows";
    public const string Flick = "SpatialAim Accélération des mouvements rapides";
    public const string Mapping = "SpatialAim Mappage de la tablette";
    public const string Calibration = "SpatialAim Basculer la calibration";
#else
    public const string Pressure = "SpatialAim Pressure Sensitivity";
    public const string Epp = "SpatialAim Windows EPP Approximation";
    public const string Flick = "SpatialAim Flick Acceleration";
    public const string Mapping = "SpatialAim Tablet Mapping";
    public const string Calibration = "SpatialAim Calibration Toggle";
#endif
}

internal sealed class LocalizedSliderPropertyAttribute : SliderPropertyAttribute
{
    public LocalizedSliderPropertyAttribute(string key, float min, float max, float defaultValue = 0f)
        : base(LocalizedText.Get(key), min, max, defaultValue)
    {
    }
}

internal sealed class LocalizedPropertyAttribute : PropertyAttribute
{
    public LocalizedPropertyAttribute(string key) : base(LocalizedText.Get(key))
    {
    }
}

internal sealed class LocalizedBooleanPropertyAttribute : BooleanPropertyAttribute
{
    public LocalizedBooleanPropertyAttribute(string key, string description) : base(LocalizedText.Get(key), description)
    {
    }
}

internal sealed class LocalizedToolTipAttribute : ToolTipAttribute
{
    public LocalizedToolTipAttribute(string key) : base(LocalizedText.Get(key))
    {
    }
}