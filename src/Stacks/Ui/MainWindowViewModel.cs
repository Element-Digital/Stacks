using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using Stacks.Discovery;
using Stacks.Processes;
using Stacks.State;

namespace Stacks.Ui;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly string _rootDir;
    private readonly StateStore _state;

    private GameRow? _selectedGame;
    private string _statusMessage = string.Empty;
    private bool _isError;
    private bool _isBusy;

    public MainWindowViewModel(string rootDir, StateStore state)
    {
        _rootDir = rootDir;
        _state = state;

        InstallCommand = new RelayCommand(InstallAsync, () => SelectedGame is { IsValid: true } g
            && !string.IsNullOrWhiteSpace(g.Game.Manifest!.Installer));
        LaunchCommand = new RelayCommand(LaunchAsync, () => SelectedGame is { IsValid: true });
        OpenFolderCommand = new RelayCommand(OpenFolder, () => SelectedGame is not null);
        RescanCommand = new RelayCommand(Rescan);
    }

    public string RootDirectory => _rootDir;

    public ObservableCollection<GameRow> Games { get; } = new();

    public GameRow? SelectedGame
    {
        get => _selectedGame;
        set
        {
            if (!ReferenceEquals(_selectedGame, value))
            {
                _selectedGame = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelection));
                ((RelayCommand)InstallCommand).RaiseCanExecuteChanged();
                ((RelayCommand)LaunchCommand).RaiseCanExecuteChanged();
                ((RelayCommand)OpenFolderCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool HasSelection => _selectedGame is not null;

    public string StatusMessage
    {
        get => _statusMessage;
        private set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(); } }
    }

    public bool IsError
    {
        get => _isError;
        private set { if (_isError != value) { _isError = value; OnPropertyChanged(); } }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set { if (_isBusy != value) { _isBusy = value; OnPropertyChanged(); } }
    }

    public System.Windows.Input.ICommand InstallCommand { get; }
    public System.Windows.Input.ICommand LaunchCommand { get; }
    public System.Windows.Input.ICommand OpenFolderCommand { get; }
    public System.Windows.Input.ICommand RescanCommand { get; }

    public async Task InitializeAsync()
    {
        try
        {
            await _state.LoadAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load state: {ex.Message}");
        }
        Rescan();
    }

    public void Rescan()
    {
        var discovered = GameDiscovery.Scan(_rootDir);
        var now = DateTimeOffset.UtcNow;

        var byFolder = new Dictionary<string, GameRow>(Games.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var row in Games) byFolder[row.FolderName] = row;

        var prevSelected = SelectedGame?.FolderName;

        Games.Clear();
        foreach (var g in discovered)
        {
            var s = _state.Get(g.FolderName);
            if (byFolder.TryGetValue(g.FolderName, out var existing))
            {
                existing.Update(g, s, now);
                Games.Add(existing);
            }
            else
            {
                Games.Add(new GameRow(g, s, now));
            }
        }

        if (prevSelected is not null)
        {
            SelectedGame = Games.FirstOrDefault(r => string.Equals(r.FolderName, prevSelected, StringComparison.OrdinalIgnoreCase));
        }
        if (SelectedGame is null && Games.Count > 0)
        {
            SelectedGame = Games[0];
        }

        ShowInfo(Games.Count == 0 ? "No games found." : $"{Games.Count} game(s).");
    }

    private async Task InstallAsync()
    {
        var row = SelectedGame;
        if (row is null || !row.IsValid) return;
        var manifest = row.Game.Manifest!;
        if (string.IsNullOrWhiteSpace(manifest.Installer))
        {
            ShowInfo("This game has no installer defined.");
            return;
        }

        IsBusy = true;
        ShowInfo($"Installing {row.DisplayName}…");
        try
        {
            var result = await ProcessRunner.RunInstallAsync(manifest, row.FolderPath).ConfigureAwait(true);
            if (result.Success) ShowInfo($"Installed {row.DisplayName}.");
            else ShowError($"Install failed: {result.Error}");
        }
        catch (Exception ex)
        {
            ShowError($"Install failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            Rescan();
        }
    }

    private async Task LaunchAsync()
    {
        var row = SelectedGame;
        if (row is null || !row.IsValid) return;

        if (ProcessRunner.IsRunning(row.FolderName))
        {
            ShowInfo($"{row.DisplayName} is already running.");
            return;
        }

        var folderName = row.FolderName;
        var manifest = row.Game.Manifest!;

        var result = ProcessRunner.LaunchDetached(manifest, folderName, row.FolderPath, async () =>
        {
            try { await _state.TouchExitAsync(folderName).ConfigureAwait(false); }
            catch { /* ignore */ }
            await Dispatcher.UIThread.InvokeAsync(Rescan).GetTask().ConfigureAwait(false);
        });

        if (result.Success)
        {
            try { await _state.RecordLaunchAsync(folderName).ConfigureAwait(true); }
            catch (Exception ex) { ShowError($"State write failed: {ex.Message}"); return; }
            ShowInfo($"Launched {row.DisplayName}.");
            Rescan();
        }
        else
        {
            ShowError($"Launch failed: {result.Error}");
        }
    }

    private void OpenFolder()
    {
        var row = SelectedGame;
        if (row is null) return;
        ProcessRunner.OpenFolder(row.FolderPath);
        ShowInfo($"Opened {row.FolderPath}.");
    }

    private void ShowInfo(string msg)
    {
        StatusMessage = msg;
        IsError = false;
    }

    private void ShowError(string msg)
    {
        StatusMessage = msg;
        IsError = true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
