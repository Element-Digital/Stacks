using System.Globalization;
using Spectre.Console;
using Stacks.Discovery;
using Stacks.State;

namespace Stacks.Ui;

internal static class DetailView
{
    public static void Render(DiscoveredGame game, StateStore state)
    {
        AnsiConsole.Clear();

        if (!game.IsValid)
        {
            var errorPanel = new Panel(new Markup($"[red]{Markup.Escape(game.Error ?? "Unknown error")}[/]"))
                .Header($"[bold]{Markup.Escape(game.FolderName)}[/]  [grey](malformed manifest)[/]")
                .Border(BoxBorder.Rounded);
            AnsiConsole.Write(errorPanel);
            AnsiConsole.MarkupLine("\n[grey]Press any key to return…[/]");
            Console.ReadKey(intercept: true);
            return;
        }

        var manifest = game.Manifest!;
        var st = state.Get(game.FolderName);

        var grid = new Grid()
            .AddColumn(new GridColumn().NoWrap().PadRight(2))
            .AddColumn();

        AddRow(grid, "Name", manifest.Name);
        AddRow(grid, "Folder", game.FolderPath);
        AddRow(grid, "Version", manifest.Version ?? "—");
        AddRow(grid, "Installer", manifest.Installer ?? "—");
        AddRow(grid, "Install args", manifest.InstallArgs ?? "—");
        AddRow(grid, "Launch", manifest.Launch);
        AddRow(grid, "Launch args", manifest.LaunchArgs ?? "—");
        AddRow(grid, "Working dir", manifest.WorkingDir ?? "(folder root)");
        AddRow(grid, "Installed marker", manifest.InstalledMarker ?? "(none)");
        AddRow(grid, "Installed", game.IsInstalled ? "yes" : "no");
        AddRow(grid, "Play count", st.PlayCount.ToString(CultureInfo.InvariantCulture));
        AddRow(grid, "Last played",
            st.LastPlayed is { } lp
                ? lp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                : "—");

        var panel = new Panel(grid)
            .Header($"[bold]{Markup.Escape(manifest.Name)}[/]")
            .Border(BoxBorder.Rounded)
            .Expand();
        AnsiConsole.Write(panel);

        if (!string.IsNullOrWhiteSpace(manifest.Notes))
        {
            var notes = new Panel(new Markup(Markup.Escape(manifest.Notes)))
                .Header("[bold]Notes[/]")
                .Border(BoxBorder.Rounded)
                .Expand();
            AnsiConsole.Write(notes);
        }

        AnsiConsole.MarkupLine("\n[grey]Press any key to return…[/]");
        Console.ReadKey(intercept: true);
    }

    private static void AddRow(Grid grid, string label, string value)
    {
        grid.AddRow($"[grey]{label}[/]", Markup.Escape(value));
    }
}
