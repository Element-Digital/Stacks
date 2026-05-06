using System.Text.Json;

namespace Stacks.Manifest;

public static class ManifestLoader
{
    public const string FileName = "stacks.json";

    public static ManifestLoadResult TryLoad(string path)
    {
        GameManifest? manifest;
        try
        {
            using var stream = File.OpenRead(path);
            manifest = JsonSerializer.Deserialize(stream, ManifestJsonContext.Default.GameManifest);
        }
        catch (JsonException ex)
        {
            return ManifestLoadResult.Fail($"Invalid JSON: {ex.Message}");
        }
        catch (IOException ex)
        {
            return ManifestLoadResult.Fail($"Could not read manifest: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return ManifestLoadResult.Fail($"Access denied reading manifest: {ex.Message}");
        }

        if (manifest is null)
        {
            return ManifestLoadResult.Fail("Manifest file is empty.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Name))
        {
            return ManifestLoadResult.Fail("Missing required field 'name'.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Launch))
        {
            return ManifestLoadResult.Fail("Missing required field 'launch'.");
        }

        return ManifestLoadResult.Ok(manifest);
    }
}
