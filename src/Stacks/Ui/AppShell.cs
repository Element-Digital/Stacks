using Spectre.Console;
using Stacks.Discovery;
using Stacks.Processes;
using Stacks.State;

namespace Stacks.Ui;

internal sealed class AppShell
{
    private readonly string _rootDir;
    private readonly StateStore _state;
    private readonly StatusBar _status = new();

    private IReadOnlyList<DiscoveredGame> _games = Array.Empty<DiscoveredGame>();
    private int _selected;
    private volatile bool _refreshPending;

    public AppShell(string rootDir, StateStore state)
    {
        _rootDir = rootDir;
        _state = state;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        await _state.LoadAsync(ct).ConfigureAwait(false);
        Rescan();

        var prevCursor = TryGetCursorVisible();
        TrySetCursorVisible(false);
        var prevTreatCtrlC = Console.TreatControlCAsInput;
        Console.TreatControlCAsInput = true;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                Render();
                var key = await ReadKeyOrRefreshAsync(ct).ConfigureAwait(false);
                if (key is null)
                {
                    continue;
                }
                if (await DispatchAsync(key.Value, ct).ConfigureAwait(false))
                {
                    return;
                }
            }
        }
        finally
        {
            Console.TreatControlCAsInput = prevTreatCtrlC;
            if (prevCursor is { } pc) TrySetCursorVisible(pc);
            AnsiConsole.Cursor.Show();
        }
    }

    private void Render()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[grey]{Markup.Escape(_rootDir)}[/]");
        AnsiConsole.Write(GameTableView.Build(_games, _selected, _state, DateTimeOffset.UtcNow));
        AnsiConsole.Write(_status.BuildFooter());
        AnsiConsole.WriteLine();
    }

    private async Task<ConsoleKeyInfo?> ReadKeyOrRefreshAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (_refreshPending)
            {
                _refreshPending = false;
                return null;
            }
            if (Console.KeyAvailable)
            {
                return Console.ReadKey(intercept: true);
            }
            try
            {
                await Task.Delay(50, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }
        return null;
    }

    private async Task<bool> DispatchAsync(ConsoleKeyInfo key, CancellationToken ct)
    {
        if (key.Modifiers.HasFlag(ConsoleModifiers.Control) && key.Key == ConsoleKey.C)
        {
            return true;
        }

        _status.Clear();
        try
        {
            switch (key.Key)
            {
                case ConsoleKey.UpArrow: ChangeSelection(-1); break;
                case ConsoleKey.DownArrow: ChangeSelection(1); break;
                case ConsoleKey.PageUp: ChangeSelection(-10); break;
                case ConsoleKey.PageDown: ChangeSelection(10); break;
                case ConsoleKey.Home: _selected = 0; break;
                case ConsoleKey.End: _selected = Math.Max(0, _games.Count - 1); break;
                case ConsoleKey.Enter: HandleDetail(); break;
                case ConsoleKey.I: await HandleInstallAsync(ct).ConfigureAwait(false); break;
                case ConsoleKey.L: await HandleLaunchAsync(ct).ConfigureAwait(false); break;
                case ConsoleKey.O: HandleOpenFolder(); break;
                case ConsoleKey.R: HandleRefresh(); break;
                case ConsoleKey.Q: return true;
                case ConsoleKey.Escape: return true;
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return true;
        }
        catch (Exception ex)
        {
            _status.ShowError(ex.Message);
        }
        return false;
    }

    private void ChangeSelection(int delta)
    {
        if (_games.Count == 0) return;
        _selected = Math.Clamp(_selected + delta, 0, _games.Count - 1);
    }

    private DiscoveredGame? Current() => _games.Count == 0 ? null : _games[_selected];

    private void HandleDetail()
    {
        var g = Current();
        if (g is null) return;
        DetailView.Render(g, _state);
    }

    private async Task HandleInstallAsync(CancellationToken ct)
    {
        var g = Current();
        if (g is null) return;
        if (!g.IsValid)
        {
            _status.ShowError("Cannot install: manifest is invalid.");
            return;
        }
        if (string.IsNullOrWhiteSpace(g.Manifest!.Installer))
        {
            _status.ShowInfo("This game has no installer defined.");
            return;
        }

        var result = await InstallView.RunAsync(g, ct).ConfigureAwait(false);
        if (result.Success) _status.ShowInfo($"Installed {g.DisplayName}.");
        else _status.ShowError($"Install failed: {result.Error}");

        Rescan();
    }

    private async Task HandleLaunchAsync(CancellationToken ct)
    {
        var g = Current();
        if (g is null) return;
        if (!g.IsValid)
        {
            _status.ShowError("Cannot launch: manifest is invalid.");
            return;
        }
        if (ProcessRunner.IsRunning(g.FolderName))
        {
            _status.ShowInfo($"{g.DisplayName} is already running.");
            return;
        }

        var folderName = g.FolderName;
        var result = ProcessRunner.LaunchDetached(g.Manifest!, folderName, g.FolderPath, async () =>
        {
            try { await _state.TouchExitAsync(folderName).ConfigureAwait(false); }
            catch { /* ignore */ }
            _refreshPending = true;
        });

        if (result.Success)
        {
            try { await _state.RecordLaunchAsync(g.FolderName, ct).ConfigureAwait(false); }
            catch (Exception ex) { _status.ShowError($"State write failed: {ex.Message}"); return; }
            _status.ShowInfo($"Launched {g.DisplayName}.");
        }
        else
        {
            _status.ShowError($"Launch failed: {result.Error}");
        }
    }

    private void HandleOpenFolder()
    {
        var g = Current();
        if (g is null) return;
        ProcessRunner.OpenFolder(g.FolderPath);
        _status.ShowInfo($"Opened {g.FolderPath}.");
    }

    private void HandleRefresh()
    {
        Rescan();
        _status.ShowInfo("Rescanned.");
    }

    private void Rescan()
    {
        _games = GameDiscovery.Scan(_rootDir);
        if (_games.Count == 0) _selected = 0;
        else if (_selected >= _games.Count) _selected = _games.Count - 1;
        else if (_selected < 0) _selected = 0;
    }

    private static bool? TryGetCursorVisible()
    {
        try { return Console.CursorVisible; }
        catch { return null; }
    }

    private static void TrySetCursorVisible(bool value)
    {
        try { Console.CursorVisible = value; }
        catch { /* ignore on platforms that disallow */ }
    }
}
