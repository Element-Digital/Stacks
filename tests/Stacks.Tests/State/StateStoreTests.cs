using Stacks.State;
using Stacks.Tests.TestHelpers;
using Xunit;

namespace Stacks.Tests.State;

public sealed class StateStoreTests
{
    [Fact]
    public async Task Load_returns_empty_when_file_missing()
    {
        using var temp = new TempDirectory();
        var store = new StateStore(temp.Path);

        await store.LoadAsync();

        Assert.Equal(GameState.Empty, store.Get("Anything"));
    }

    [Fact]
    public async Task Load_returns_empty_on_corrupt_file_without_throwing()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, StateStore.FileName), "{ this is not valid");

        var store = new StateStore(temp.Path);
        await store.LoadAsync();

        Assert.Equal(GameState.Empty, store.Get("Anything"));
    }

    [Fact]
    public async Task Round_trips_recorded_launches()
    {
        using var temp = new TempDirectory();

        var store1 = new StateStore(temp.Path);
        await store1.LoadAsync();
        await store1.RecordLaunchAsync("MyGame");
        await store1.RecordLaunchAsync("MyGame");
        await store1.RecordLaunchAsync("OtherGame");

        var store2 = new StateStore(temp.Path);
        await store2.LoadAsync();

        Assert.Equal(2, store2.Get("MyGame").PlayCount);
        Assert.Equal(1, store2.Get("OtherGame").PlayCount);
        Assert.NotNull(store2.Get("MyGame").LastPlayed);
    }

    [Fact]
    public async Task Get_is_case_insensitive_after_round_trip()
    {
        using var temp = new TempDirectory();

        var store1 = new StateStore(temp.Path);
        await store1.LoadAsync();
        await store1.RecordLaunchAsync("MyGame");

        var store2 = new StateStore(temp.Path);
        await store2.LoadAsync();

        Assert.Equal(1, store2.Get("mygame").PlayCount);
        Assert.Equal(1, store2.Get("MYGAME").PlayCount);
    }

    [Fact]
    public async Task Concurrent_record_launches_count_correctly()
    {
        using var temp = new TempDirectory();
        var store = new StateStore(temp.Path);
        await store.LoadAsync();

        const int iterations = 50;
        var tasks = Enumerable.Range(0, iterations)
            .Select(_ => Task.Run(() => store.RecordLaunchAsync("Race")))
            .ToArray();
        await Task.WhenAll(tasks);

        Assert.Equal(iterations, store.Get("Race").PlayCount);

        var reloaded = new StateStore(temp.Path);
        await reloaded.LoadAsync();
        Assert.Equal(iterations, reloaded.Get("Race").PlayCount);
    }
}
