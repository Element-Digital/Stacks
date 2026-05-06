using Stacks.Discovery;
using Stacks.Tests.TestHelpers;
using Xunit;

namespace Stacks.Tests.Discovery;

public sealed class GameDiscoveryTests
{
    [Fact]
    public void Skips_folder_without_manifest()
    {
        using var temp = new TempDirectory();
        temp.CreateSubDir("Empty");

        var games = GameDiscovery.Scan(temp.Path);

        Assert.Empty(games);
    }

    [Fact]
    public void Returns_valid_game_with_manifest()
    {
        using var temp = new TempDirectory();
        temp.WriteFile(Path.Combine("MyGame", "stacks.json"),
            """{ "name": "Alpha", "launch": "alpha.exe" }""");

        var games = GameDiscovery.Scan(temp.Path);

        var game = Assert.Single(games);
        Assert.True(game.IsValid);
        Assert.Equal("Alpha", game.DisplayName);
        Assert.Equal("MyGame", game.FolderName);
        Assert.Null(game.Error);
    }

    [Fact]
    public void Returns_warning_entry_for_malformed_manifest()
    {
        using var temp = new TempDirectory();
        temp.WriteFile(Path.Combine("Broken", "stacks.json"), "{ not valid");

        var games = GameDiscovery.Scan(temp.Path);

        var game = Assert.Single(games);
        Assert.False(game.IsValid);
        Assert.Equal("Broken", game.DisplayName);
        Assert.NotNull(game.Error);
    }

    [Fact]
    public void Skips_dot_and_underscore_prefixed_folders()
    {
        using var temp = new TempDirectory();
        temp.WriteFile(Path.Combine(".hidden", "stacks.json"),
            """{ "name": "Hidden", "launch": "x.exe" }""");
        temp.WriteFile(Path.Combine("_internal", "stacks.json"),
            """{ "name": "Internal", "launch": "x.exe" }""");
        temp.WriteFile(Path.Combine("Visible", "stacks.json"),
            """{ "name": "Visible", "launch": "x.exe" }""");

        var games = GameDiscovery.Scan(temp.Path);

        var game = Assert.Single(games);
        Assert.Equal("Visible", game.DisplayName);
    }

    [Fact]
    public void Skips_hidden_attribute_folders()
    {
        using var temp = new TempDirectory();
        var hidden = temp.CreateSubDir("Stash");
        temp.WriteFile(Path.Combine("Stash", "stacks.json"),
            """{ "name": "Stash", "launch": "x.exe" }""");
        File.SetAttributes(hidden, File.GetAttributes(hidden) | FileAttributes.Hidden);
        try
        {
            var games = GameDiscovery.Scan(temp.Path);
            Assert.Empty(games);
        }
        finally
        {
            File.SetAttributes(hidden, File.GetAttributes(hidden) & ~FileAttributes.Hidden);
        }
    }

    [Fact]
    public void Sorts_by_display_name_case_insensitive()
    {
        using var temp = new TempDirectory();
        temp.WriteFile(Path.Combine("z-folder", "stacks.json"),
            """{ "name": "alpha", "launch": "x.exe" }""");
        temp.WriteFile(Path.Combine("a-folder", "stacks.json"),
            """{ "name": "Bravo", "launch": "x.exe" }""");

        var games = GameDiscovery.Scan(temp.Path);

        Assert.Collection(games,
            g => Assert.Equal("alpha", g.DisplayName),
            g => Assert.Equal("Bravo", g.DisplayName));
    }

    [Fact]
    public void Marks_installed_when_marker_file_exists()
    {
        using var temp = new TempDirectory();
        temp.WriteFile(Path.Combine("Game", "stacks.json"),
            """{ "name": "Game", "launch": "x.exe", "installedMarker": ".installed" }""");
        temp.WriteFile(Path.Combine("Game", ".installed"), "");

        var games = GameDiscovery.Scan(temp.Path);

        Assert.True(games[0].IsInstalled);
    }

    [Fact]
    public void Marks_not_installed_when_marker_missing()
    {
        using var temp = new TempDirectory();
        temp.WriteFile(Path.Combine("Game", "stacks.json"),
            """{ "name": "Game", "launch": "x.exe", "installedMarker": ".installed" }""");

        var games = GameDiscovery.Scan(temp.Path);

        Assert.False(games[0].IsInstalled);
    }

    [Fact]
    public void Falls_back_to_launch_target_when_no_marker_specified()
    {
        using var temp = new TempDirectory();
        temp.WriteFile(Path.Combine("Game", "stacks.json"),
            """{ "name": "Game", "launch": "game.exe" }""");
        temp.WriteFile(Path.Combine("Game", "game.exe"), "");

        var games = GameDiscovery.Scan(temp.Path);

        Assert.True(games[0].IsInstalled);
    }

    [Fact]
    public void Returns_empty_when_root_does_not_exist()
    {
        var games = GameDiscovery.Scan(Path.Combine(Path.GetTempPath(), "definitely-not-a-real-stacks-root-" + Guid.NewGuid().ToString("N")));

        Assert.Empty(games);
    }
}
