using System.Collections.Concurrent;
using System.Diagnostics;
using Stacks.Manifest;

namespace Stacks.Processes;

public static class ProcessRunner
{
    private const string DefaultInstalledMarkerName = ".installed";

    private static readonly ConcurrentDictionary<string, Process> RunningGames = new(StringComparer.OrdinalIgnoreCase);

    public static async Task<InstallResult> RunInstallAsync(
        GameManifest manifest,
        string folder,
        IProgress<string> output,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(manifest.Installer))
        {
            return InstallResult.Fail("Manifest does not declare an 'installer'.");
        }

        var installerPath = Path.GetFullPath(Path.Combine(folder, manifest.Installer));
        if (!File.Exists(installerPath))
        {
            return InstallResult.Fail($"Installer not found: {installerPath}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = installerPath,
            Arguments = manifest.InstallArgs ?? string.Empty,
            WorkingDirectory = folder,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) output.Report(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) output.Report(e.Data); };

        try
        {
            if (!process.Start())
            {
                return InstallResult.Fail("Installer failed to start.");
            }
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return InstallResult.Fail($"Installer failed to start: {ex.Message}");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
            return InstallResult.Fail("Installation was cancelled.");
        }

        if (process.ExitCode != 0)
        {
            return InstallResult.Fail(process.ExitCode, $"Installer exited with code {process.ExitCode}.");
        }

        var markerName = string.IsNullOrWhiteSpace(manifest.InstalledMarker)
            ? DefaultInstalledMarkerName
            : manifest.InstalledMarker!;
        var markerPath = Path.Combine(folder, markerName);
        try
        {
            await File.WriteAllTextAsync(markerPath, DateTimeOffset.UtcNow.ToString("O"), ct).ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            return InstallResult.Fail(process.ExitCode, $"Installer succeeded but marker write failed: {ex.Message}");
        }

        return InstallResult.Ok(process.ExitCode);
    }

    public static LaunchResult LaunchDetached(
        GameManifest manifest,
        string folderName,
        string folderPath,
        Func<Task> onExitAsync)
    {
        if (string.IsNullOrWhiteSpace(manifest.Launch))
        {
            return LaunchResult.Fail("Manifest does not declare a 'launch' target.");
        }

        var launchPath = Path.GetFullPath(Path.Combine(folderPath, manifest.Launch));
        if (!File.Exists(launchPath))
        {
            return LaunchResult.Fail($"Launch target not found: {launchPath}");
        }

        var workingDir = string.IsNullOrWhiteSpace(manifest.WorkingDir)
            ? folderPath
            : Path.GetFullPath(Path.Combine(folderPath, manifest.WorkingDir));

        var startInfo = new ProcessStartInfo
        {
            FileName = launchPath,
            Arguments = manifest.LaunchArgs ?? string.Empty,
            WorkingDirectory = workingDir,
            UseShellExecute = true,
        };

        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        process.Exited += async (_, _) =>
        {
            try
            {
                if (RunningGames.TryRemove(folderName, out var p)) p.Dispose();
                await onExitAsync().ConfigureAwait(false);
            }
            catch
            {
                // Swallow — exit handler must never crash the host.
            }
        };

        // Register before Start so the Exited handler (which may fire on a thread pool
        // thread the moment Start returns for a fast-exiting process) can find the entry.
        RunningGames[folderName] = process;

        try
        {
            if (!process.Start())
            {
                RunningGames.TryRemove(folderName, out _);
                process.Dispose();
                return LaunchResult.Fail("Game failed to start.");
            }
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            RunningGames.TryRemove(folderName, out _);
            process.Dispose();
            return LaunchResult.Fail($"Game failed to start: {ex.Message}");
        }

        return LaunchResult.Ok();
    }

    public static bool IsRunning(string folderName) => RunningGames.ContainsKey(folderName);

    public static void OpenFolder(string folderPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = folderPath,
            UseShellExecute = true,
        };
        try
        {
            Process.Start(startInfo)?.Dispose();
        }
        catch
        {
            // Best-effort; the caller surfaces failure via status bar by checking the result.
        }
    }
}
