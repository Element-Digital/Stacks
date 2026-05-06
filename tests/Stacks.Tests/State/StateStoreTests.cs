using Stacks.State;
using Stacks.Tests.TestHelpers;
using Xunit;

namespace Stacks.Tests.State;

public sealed class StateStoreTests
{
    [Fact]
    public async Task Load_returns_empty_when_file_missing()
    {
        var ct = TestContext.Current.CancellationToken;
        using var temp = new TempDirectory();
        var store = new StateStore(temp.Path);

        await store.LoadAsync(ct);

        Assert.Equal(GameState.Empty, store.Get("Anything"));
    }

    [Fact]
    public async Task Load_returns_empty_on_corrupt_file_without_throwing()
    {
        var ct = TestContext.Current.CancellationToken;
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, StateStore.FileName), "{ this is not valid");

        var store = new StateStore(temp.Path);
        await store.LoadAsync(ct);

        Assert.Equal(GameState.Empty, store.Get("Anything"));
    }

    [Fact]
    public async Task Round_trips_recorded_launches()
    {
        var ct = TestContext.Current.CancellationToken;
        using var temp = new TempDirectory();

        var store1 = new StateStore(temp.Path);
        await store1.LoadAsync(ct);
        await store1.RecordLaunchAsync("MyGame", ct);
        await store1.RecordLaunchAsync("MyGame", ct);
        await store1.RecordLaunchAsync("OtherGame", ct);

        var store2 = new StateStore(temp.Path);
        await store2.LoadAsync(ct);

        Assert.Equal(2, store2.Get("MyGame").PlayCount);
        Assert.Equal(1, store2.Get("OtherGame").PlayCount);
        Assert.NotNull(store2.Get("MyGame").LastPlayed);
    }

    [Fact]
    public async Task Get_is_case_insensitive_after_round_trip()
    {
        var ct = TestContext.Current.CancellationToken;
        using var temp = new TempDirectory();

        var store1 = new StateStore(temp.Path);
        await store1.LoadAsync(ct);
        await store1.RecordLaunchAsync("MyGame", ct);

        var store2 = new StateStore(temp.Path);
        await store2.LoadAsync(ct);

        Assert.Equal(1, store2.Get("mygame").PlayCount);
        Assert.Equal(1, store2.Get("MYGAME").PlayCount);
    }

    [Fact]
    public async Task Concurrent_record_launches_count_correctly()
    {
        var ct = TestContext.Current.CancellationToken;
        using var temp = new TempDirectory();
        var store = new StateStore(temp.Path);
        await store.LoadAsync(ct);

        const int iterations = 50;
        var tasks = Enumerable.Range(0, iterations)
            .Select(_ => Task.Run(() => store.RecordLaunchAsync("Race", ct), ct))
            .ToArray();
        await Task.WhenAll(tasks);

        Assert.Equal(iterations, store.Get("Race").PlayCount);

        var reloaded = new StateStore(temp.Path);
        await reloaded.LoadAsync(ct);
        Assert.Equal(iterations, reloaded.Get("Race").PlayCount);
    }
}
