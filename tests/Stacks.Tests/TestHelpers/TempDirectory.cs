namespace Stacks.Tests.TestHelpers;

public sealed class TempDirectory : IDisposable
{
    public string Path { get; }

    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "stacks-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string CreateSubDir(string name)
    {
        var sub = System.IO.Path.Combine(Path, name);
        Directory.CreateDirectory(sub);
        return sub;
    }

    public string WriteFile(string relativePath, string contents)
    {
        var full = System.IO.Path.Combine(Path, relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        File.WriteAllText(full, contents);
        return full;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // best-effort cleanup
        }
        catch (UnauthorizedAccessException)
        {
            // best-effort cleanup
        }
    }
}
