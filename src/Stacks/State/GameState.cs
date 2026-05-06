namespace Stacks.State;

public sealed record GameState(DateTimeOffset? LastPlayed, int PlayCount)
{
    public static GameState Empty { get; } = new(null, 0);
}
