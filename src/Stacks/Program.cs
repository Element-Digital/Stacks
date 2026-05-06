using Spectre.Console;
using Stacks.State;
using Stacks.Ui;

namespace Stacks;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootDir = ResolveRootDirectory();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            // Ctrl+C arrives via the AppShell key loop while running; this handler
            // covers cancellation during startup before TreatControlCAsInput is set.
            if (!cts.IsCancellationRequested)
            {
                e.Cancel = true;
                cts.Cancel();
            }
        };

        var state = new StateStore(rootDir);
        var shell = new AppShell(rootDir, state);

        try
        {
            await shell.RunAsync(cts.Token).ConfigureAwait(false);
            return 0;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (Exception ex)
        {
            // Avoid AnsiConsole.WriteException — it requires dynamic code under AOT.
            AnsiConsole.Reset();
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("Stacks crashed:");
            AnsiConsole.WriteLine(ex.ToString());
            return 1;
        }
    }

    private static string ResolveRootDirectory()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(processPath))
        {
            var dir = Path.GetDirectoryName(processPath);
            if (!string.IsNullOrEmpty(dir)) return dir;
        }
        return AppContext.BaseDirectory;
    }
}
