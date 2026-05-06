namespace Stacks.Processes;

public readonly record struct LaunchResult(bool Success, string? Error)
{
    public static LaunchResult Ok() => new(true, null);
    public static LaunchResult Fail(string error) => new(false, error);
}
