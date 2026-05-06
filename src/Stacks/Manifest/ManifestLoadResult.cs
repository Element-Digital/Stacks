namespace Stacks.Manifest;

public readonly record struct ManifestLoadResult(GameManifest? Manifest, string? Error)
{
    public bool IsSuccess => Manifest is not null;

    public static ManifestLoadResult Ok(GameManifest manifest) => new(manifest, null);

    public static ManifestLoadResult Fail(string error) => new(null, error);
}
