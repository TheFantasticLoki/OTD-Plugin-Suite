using System.Security.Cryptography;
using System.Text;
using OpenTabletDriver.Plugin.Tablet;

namespace SpatialAim.Filters.Curves;

public static class TabletMappingCalibrationStore
{
    public static string GetStoragePath(string tabletKey, string? dataDirectory = null) =>
        GetPath(tabletKey, dataDirectory);

    public static string GetTabletKey(TabletReference tablet)
    {
        var identifiers = tablet.Identifiers
            .OrderBy(identifier => identifier.VendorID)
            .ThenBy(identifier => identifier.ProductID)
            .ThenBy(identifier => identifier.ReportParser)
            .Select(identifier => $"{identifier.VendorID:X4}:{identifier.ProductID:X4}:{identifier.ReportParser}");
        var identity = $"{tablet.Properties.Name}\n{string.Join("\n", identifiers)}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool TryLoad(string tabletKey, out string correctionGrid, string? dataDirectory = null)
    {
        correctionGrid = string.Empty;
        try
        {
            var path = GetPath(tabletKey, dataDirectory);
            if (!File.Exists(path))
                return false;

            var storedGrid = File.ReadAllText(path);
            if (!TabletSpaceWarpMath.TryParseCorrectionSamples(storedGrid, out _))
                return false;

            correctionGrid = storedGrid;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            OpenTabletDriver.Plugin.Log.Write(
                "SpatialAim Tablet Mapping",
                $"Could not read automatic calibration: {exception.Message}",
                OpenTabletDriver.Plugin.LogLevel.Warning);
            return false;
        }
    }

    public static bool TrySave(string tabletKey, string correctionGrid, string? dataDirectory = null)
    {
        if (!TabletSpaceWarpMath.TryParseCorrectionSamples(correctionGrid, out _))
            return false;

        string? temporaryPath = null;
        try
        {
            var path = GetPath(tabletKey, dataDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
            File.WriteAllText(temporaryPath, correctionGrid);
            File.Move(temporaryPath, path, true);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            OpenTabletDriver.Plugin.Log.Write(
                "SpatialAim Tablet Mapping",
                $"Could not save automatic calibration: {exception.Message}",
                OpenTabletDriver.Plugin.LogLevel.Warning);
            return false;
        }
        finally
        {
            if (temporaryPath != null && File.Exists(temporaryPath))
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
    }

    public static bool TryClear(string tabletKey, string? dataDirectory = null)
    {
        try
        {
            var path = GetPath(tabletKey, dataDirectory);
            if (File.Exists(path))
                File.Delete(path);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            OpenTabletDriver.Plugin.Log.Write(
                "SpatialAim Tablet Mapping",
                $"Could not clear automatic calibration: {exception.Message}",
                OpenTabletDriver.Plugin.LogLevel.Warning);
            return false;
        }
    }

    private static string GetPath(string tabletKey, string? dataDirectory)
    {
        if (string.IsNullOrWhiteSpace(tabletKey) || tabletKey.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("Tablet key is not a valid file name.", nameof(tabletKey));

        var root = dataDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("No application data directory is available.", nameof(dataDirectory));

        return Path.Combine(root, "SpatialAim", "Calibration", $"{tabletKey}.txt");
    }
}