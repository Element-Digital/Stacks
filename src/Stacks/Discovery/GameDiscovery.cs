using Stacks.Manifest;

namespace Stacks.Discovery;

public static class GameDiscovery
{
    public static IReadOnlyList<DiscoveredGame> Scan(string rootDirectory)
    {
        var results = new List<DiscoveredGame>();
        if (!Directory.Exists(rootDirectory)) return results;

        foreach (var dir in Directory.EnumerateDirectories(rootDirectory))
        {
            DirectoryInfo info;
            try
            {
                info = new DirectoryInfo(dir);
            }
            catch (IOException)
            {
                continue;
            }

            if (ShouldSkip(info)) continue;

            var manifestPath = Path.Combine(dir, ManifestLoader.FileName);
            if (!File.Exists(manifestPath)) continue;

            var loadResult = ManifestLoader.TryLoad(manifestPath);
            if (!loadResult.IsSuccess)
            {
                results.Add(new DiscoveredGame(info.Name, dir, null, loadResult.Error, false));
                continue;
            }

            var manifest = loadResult.Manifest!;
            var isInstalled = ComputeInstalled(dir, manifest);
            results.Add(new DiscoveredGame(info.Name, dir, manifest, null, isInstalled));
        }

        results.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
        return results;
    }

    private static bool ShouldSkip(DirectoryInfo info)
    {
        if (info.Name.Length == 0) return true;
        if (info.Name[0] == '.' || info.Name[0] == '_') return true;

        FileAttributes attrs;
        try { attrs = info.Attributes; }
        catch (IOException) { return true; }

        if ((attrs & FileAttributes.Hidden) != 0) return true;
        if ((attrs & FileAttributes.System) != 0) return true;
        if ((attrs & FileAttributes.ReparsePoint) != 0) return true;
        return false;
    }

    private static bool ComputeInstalled(string folder, GameManifest manifest)
    {
        if (!string.IsNullOrWhiteSpace(manifest.InstalledMarker))
        {
            return File.Exists(Path.Combine(folder, manifest.InstalledMarker));
        }
        return File.Exists(Path.Combine(folder, manifest.Launch));
    }
}
