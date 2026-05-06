using System.Text.Json;

namespace Stacks.State;

public sealed class StateStore
{
    public const string FileName = "stacks.state.json";

    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Dictionary<string, GameState> _state = new(StringComparer.OrdinalIgnoreCase);

    public StateStore(string directory)
    {
        _filePath = Path.Combine(directory, FileName);
    }

    public string FilePath => _filePath;

    public async Task LoadAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            _state = await ReadFromDiskAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public GameState Get(string folder)
    {
        lock (_state)
        {
            return _state.TryGetValue(folder, out var s) ? s : GameState.Empty;
        }
    }

    public async Task RecordLaunchAsync(string folder, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentException("Folder must be non-empty.", nameof(folder));

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var existing = _state.TryGetValue(folder, out var s) ? s : GameState.Empty;
            _state[folder] = existing with
            {
                LastPlayed = DateTimeOffset.UtcNow,
                PlayCount = existing.PlayCount + 1,
            };
            await SaveLockedAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task TouchExitAsync(string folder, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentException("Folder must be non-empty.", nameof(folder));

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var existing = _state.TryGetValue(folder, out var s) ? s : GameState.Empty;
            _state[folder] = existing with { LastPlayed = DateTimeOffset.UtcNow };
            await SaveLockedAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<Dictionary<string, GameState>> ReadFromDiskAsync(CancellationToken ct)
    {
        if (!File.Exists(_filePath))
        {
            return new Dictionary<string, GameState>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var loaded = await JsonSerializer.DeserializeAsync(
                stream,
                StateJsonContext.Default.DictionaryStringGameState,
                ct).ConfigureAwait(false);

            return loaded is null
                ? new Dictionary<string, GameState>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, GameState>(loaded, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, GameState>(StringComparer.OrdinalIgnoreCase);
        }
        catch (IOException)
        {
            return new Dictionary<string, GameState>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private async Task SaveLockedAsync(CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var tmp = _filePath + ".tmp";
        await using (var stream = File.Create(tmp))
        {
            await JsonSerializer.SerializeAsync(
                stream,
                _state,
                StateJsonContext.Default.DictionaryStringGameState,
                ct).ConfigureAwait(false);
        }
        File.Move(tmp, _filePath, overwrite: true);
    }
}
