using System.Globalization;
using Spectre.Console;
using Stacks.Discovery;
using Stacks.State;

namespace Stacks.Ui;

internal static class GameTableView
{
    public static Table Build(
        IReadOnlyList<DiscoveredGame> games,
        int selectedIndex,
        StateStore state,
        DateTimeOffset now)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Stacks[/]")
            .Caption(games.Count == 0 ? "[grey](no games found)[/]" : string.Empty)
            .Expand();

        table.AddColumn(new TableColumn("[bold]Name[/]"));
        table.AddColumn(new TableColumn("[bold]Version[/]"));
        table.AddColumn(new TableColumn("[bold]Installed[/]"));
        table.AddColumn(new TableColumn("[bold]Last Played[/]"));
        table.AddColumn(new TableColumn("[bold]Plays[/]") { Alignment = Justify.Right });

        for (var i = 0; i < games.Count; i++)
        {
            var game = games[i];
            var st = state.Get(game.FolderName);
            var selected = i == selectedIndex;

            string name;
            string version;
            string installed;
            string lastPlayed;
            string plays;

            if (game.IsValid)
            {
                name = Markup.Escape(game.DisplayName);
                version = Markup.Escape(game.Manifest!.Version ?? "—");
                installed = game.IsInstalled ? "[green]yes[/]" : "[red]no[/]";
                lastPlayed = Markup.Escape(RelativeTime.Format(st.LastPlayed, now));
                plays = st.PlayCount.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                name = $"[red]{Markup.Escape(game.FolderName)}[/]";
                version = "—";
                installed = "[red]error[/]";
                lastPlayed = "—";
                plays = "—";
            }

            if (selected)
            {
                name = $"[invert]{name}[/]";
                version = $"[invert]{version}[/]";
                installed = $"[invert]{installed}[/]";
                lastPlayed = $"[invert]{lastPlayed}[/]";
                plays = $"[invert]{plays}[/]";
            }

            table.AddRow(name, version, installed, lastPlayed, plays);
        }

        return table;
    }
}
