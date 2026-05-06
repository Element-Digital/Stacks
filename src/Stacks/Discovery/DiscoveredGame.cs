using Stacks.Manifest;

namespace Stacks.Discovery;

public sealed record DiscoveredGame(
    string FolderName,
    string FolderPath,
    GameManifest? Manifest,
    string? Error,
    bool IsInstalled)
{
    public bool IsValid => Manifest is not null;

    public string DisplayName => Manifest?.Name ?? FolderName;
}
