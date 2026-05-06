namespace Stacks.Manifest;

public sealed record GameManifest(
    string Name,
    string? Version,
    string? Installer,
    string? InstallArgs,
    string Launch,
    string? LaunchArgs,
    string? WorkingDir,
    string? InstalledMarker,
    string? Notes);
