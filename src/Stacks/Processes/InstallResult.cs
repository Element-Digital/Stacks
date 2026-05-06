namespace Stacks.Processes;

public readonly record struct InstallResult(bool Success, int ExitCode, string? Error)
{
    public static InstallResult Ok(int exitCode) => new(true, exitCode, null);
    public static InstallResult Fail(string error) => new(false, -1, error);
    public static InstallResult Fail(int exitCode, string error) => new(false, exitCode, error);
}
