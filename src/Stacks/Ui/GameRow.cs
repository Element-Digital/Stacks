using System.ComponentModel;
using System.Runtime.CompilerServices;
using Stacks.Discovery;
using Stacks.State;

namespace Stacks.Ui;

public sealed class GameRow : INotifyPropertyChanged
{
    private DiscoveredGame _game;
    private GameState _state;
    private DateTimeOffset _now;

    public GameRow(DiscoveredGame game, GameState state, DateTimeOffset now)
    {
        _game = game;
        _state = state;
        _now = now;
    }

    public DiscoveredGame Game => _game;

    public string DisplayName => _game.DisplayName;
    public string Version => _game.Manifest?.Version ?? string.Empty;
    public string FolderName => _game.FolderName;
    public string FolderPath => _game.FolderPath;
    public bool IsInstalled => _game.IsInstalled;
    public bool IsValid => _game.IsValid;
    public string? LoadError => _game.Error;
    public string? Notes => _game.Manifest?.Notes;

    public string LastPlayedRelative => RelativeTime.Format(_state.LastPlayed, _now);
    public int PlayCount => _state.PlayCount;

    public string StatusGlyph => !_game.IsValid
        ? "!"
        : _game.IsInstalled ? "●" : "○";

    public string StatusTooltip => !_game.IsValid
        ? (_game.Error ?? "Manifest invalid")
        : _game.IsInstalled ? "Installed" : "Not installed";

    public void Update(DiscoveredGame game, GameState state, DateTimeOffset now)
    {
        _game = game;
        _state = state;
        _now = now;
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(Version));
        OnPropertyChanged(nameof(IsInstalled));
        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(LoadError));
        OnPropertyChanged(nameof(Notes));
        OnPropertyChanged(nameof(LastPlayedRelative));
        OnPropertyChanged(nameof(PlayCount));
        OnPropertyChanged(nameof(StatusGlyph));
        OnPropertyChanged(nameof(StatusTooltip));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
