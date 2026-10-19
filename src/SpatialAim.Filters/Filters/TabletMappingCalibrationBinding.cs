using System.Collections.Concurrent;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Tablet;
using SpatialAim.Filters.Curves;
using SpatialAim.Filters.Localization;

namespace SpatialAim.Filters.Filters;

[PluginName(LocalizedPluginNames.Calibration)]
public sealed class TabletMappingCalibrationBinding : IStateBinding
{
    public void Press(TabletReference tablet, IDeviceReport report)
    {
        TabletMappingCalibrationSignal.WriteConsoleDiagnostic(
            $"Auxiliary calibration binding pressed for tablet '{tablet.Properties.Name}'.");
        TabletMappingCalibrationSignal.RequestToggle(tablet);
    }

    public void Release(TabletReference tablet, IDeviceReport report)
    {
    }
}

internal static class TabletMappingCalibrationSignal
{
    private static readonly ConcurrentDictionary<string, int> pendingToggles = new();

    public static void RequestToggle(TabletReference tablet) =>
        pendingToggles.AddOrUpdate(TabletMappingCalibrationStore.GetTabletKey(tablet), 1, static (_, count) => count + 1);

    public static bool TryTakeToggle(TabletReference tablet)
    {
        if (!pendingToggles.TryRemove(TabletMappingCalibrationStore.GetTabletKey(tablet), out var count))
            return false;

        return count % 2 == 1;
    }

    public static void WriteConsoleDiagnostic(string message)
    {
        try
        {
            Console.WriteLine($"[SpatialAim] {message}");
        }
        catch (IOException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }
}