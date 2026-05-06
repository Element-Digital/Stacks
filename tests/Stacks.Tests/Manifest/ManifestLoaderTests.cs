using Stacks.Manifest;
using Stacks.Tests.TestHelpers;
using Xunit;

namespace Stacks.Tests.Manifest;

public sealed class ManifestLoaderTests
{
    [Fact]
    public void Loads_full_valid_manifest()
    {
        using var temp = new TempDirectory();
        var path = temp.WriteFile("stacks.json",
            """
            {
              "name": "Doom",
              "version": "1.9",
              "installer": "setup.exe",
              "installArgs": "/silent",
              "launch": "bin/doom.exe",
              "launchArgs": "-fast",
              "workingDir": "bin",
              "installedMarker": ".installed",
              "notes": "Rip and tear."
            }
            """);

        var result = ManifestLoader.TryLoad(path);

        Assert.True(result.IsSuccess);
        var m = result.Manifest!;
        Assert.Equal("Doom", m.Name);
        Assert.Equal("1.9", m.Version);
        Assert.Equal("setup.exe", m.Installer);
        Assert.Equal("/silent", m.InstallArgs);
        Assert.Equal("bin/doom.exe", m.Launch);
        Assert.Equal("-fast", m.LaunchArgs);
        Assert.Equal("bin", m.WorkingDir);
        Assert.Equal(".installed", m.InstalledMarker);
        Assert.Equal("Rip and tear.", m.Notes);
    }

    [Fact]
    public void Loads_manifest_with_only_required_fields()
    {
        using var temp = new TempDirectory();
        var path = temp.WriteFile("stacks.json",
            """{ "name": "Tiny", "launch": "tiny.exe" }""");

        var result = ManifestLoader.TryLoad(path);

        Assert.True(result.IsSuccess);
        Assert.Equal("Tiny", result.Manifest!.Name);
        Assert.Equal("tiny.exe", result.Manifest!.Launch);
        Assert.Null(result.Manifest!.Version);
        Assert.Null(result.Manifest!.Installer);
        Assert.Null(result.Manifest!.Notes);
    }

    [Fact]
    public void Fails_when_name_is_missing()
    {
        using var temp = new TempDirectory();
        var path = temp.WriteFile("stacks.json",
            """{ "launch": "tiny.exe" }""");

        var result = ManifestLoader.TryLoad(path);

        Assert.False(result.IsSuccess);
        Assert.Contains("name", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Fails_when_launch_is_missing()
    {
        using var temp = new TempDirectory();
        var path = temp.WriteFile("stacks.json",
            """{ "name": "Tiny" }""");

        var result = ManifestLoader.TryLoad(path);

        Assert.False(result.IsSuccess);
        Assert.Contains("launch", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Ignores_unknown_fields()
    {
        using var temp = new TempDirectory();
        var path = temp.WriteFile("stacks.json",
            """
            {
              "name": "Tiny",
              "launch": "tiny.exe",
              "futureFlag": true,
              "publisher": "Acme"
            }
            """);

        var result = ManifestLoader.TryLoad(path);

        Assert.True(result.IsSuccess);
        Assert.Equal("Tiny", result.Manifest!.Name);
    }

    [Fact]
    public void Fails_on_malformed_json_without_throwing()
    {
        using var temp = new TempDirectory();
        var path = temp.WriteFile("stacks.json", "{ not valid json");

        var result = ManifestLoader.TryLoad(path);

        Assert.False(result.IsSuccess);
        Assert.Contains("invalid json", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}
