using Spectre.Console;
using Stacks.Discovery;
using Stacks.Processes;

namespace Stacks.Ui;

internal static class InstallView
{
    private const int MaxVisibleLines = 25;
    private const int MaxBufferedLines = 500;

    public static async Task<InstallResult> RunAsync(DiscoveredGame game, CancellationToken outerCt)
    {
        if (!game.IsValid)
        {
            return InstallResult.Fail("Cannot install: manifest is invalid.");
        }
        if (string.IsNullOrWhiteSpace(game.Manifest!.Installer))
        {
            return InstallResult.Fail("Manifest does not declare an 'installer'.");
        }

        AnsiConsole.Clear();

        var log = new List<string>();
        var lockObj = new object();
        var progress = new Progress<string>(line =>
        {
            lock (lockObj)
            {
                log.Add(line);
                if (log.Count > MaxBufferedLines)
                {
                    log.RemoveRange(0, log.Count - MaxBufferedLines);
                }
            }
        });

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(outerCt);
        var installTask = ProcessRunner.RunInstallAsync(game.Manifest!, game.FolderPath, progress, cts.Token);

        InstallResult result = default;

        await AnsiConsole.Live(BuildPanel(game, log, lockObj))
            .AutoClear(true)
            .StartAsync(async ctx =>
            {
                while (!installTask.IsCompleted)
                {
                    ctx.UpdateTarget(BuildPanel(game, log, lockObj));
                    ctx.Refresh();

                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(intercept: true);
                        if (key.Key == ConsoleKey.Escape)
                        {
                            cts.Cancel();
                        }
                    }

                    try { await Task.Delay(150, outerCt).ConfigureAwait(false); }
                    catch (OperationCanceledException) { cts.Cancel(); }
                }

                ctx.UpdateTarget(BuildPanel(game, log, lockObj));
                ctx.Refresh();
                result = await installTask.ConfigureAwait(false);
            });

        return result;
    }

    private static Panel BuildPanel(DiscoveredGame game, List<string> log, object gate)
    {
        string[] visible;
        lock (gate)
        {
            var start = Math.Max(0, log.Count - MaxVisibleLines);
            visible = new string[log.Count - start];
            for (var i = 0; i < visible.Length; i++)
            {
                visible[i] = log[start + i];
            }
        }

        var body = visible.Length == 0
            ? new Markup("[grey](installer running…)[/]")
            : (Spectre.Console.Rendering.IRenderable)new Rows(
                visible.Select(l => new Markup(Markup.Escape(l))).ToArray());

        return new Panel(body)
            .Header($"[bold]Installing {Markup.Escape(game.DisplayName)}[/]  [grey](Esc to cancel)[/]")
            .Border(BoxBorder.Rounded)
            .Expand();
    }
}
