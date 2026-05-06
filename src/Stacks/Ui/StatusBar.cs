using Spectre.Console;
using Spectre.Console.Rendering;

namespace Stacks.Ui;

internal sealed class StatusBar
{
    public string? Message { get; private set; }
    public StatusKind Kind { get; private set; } = StatusKind.None;

    public void ShowInfo(string message)
    {
        Message = message;
        Kind = StatusKind.Info;
    }

    public void ShowError(string message)
    {
        Message = message;
        Kind = StatusKind.Error;
    }

    public void Clear()
    {
        Message = null;
        Kind = StatusKind.None;
    }

    public IRenderable BuildFooter()
    {
        var hints = "[grey]Enter[/] details  [grey]I[/] install  [grey]L[/] launch  [grey]O[/] folder  [grey]R[/] refresh  [grey]Q[/] quit";
        if (Message is null)
        {
            return new Markup(hints);
        }

        var color = Kind == StatusKind.Error ? "red" : "yellow";
        var safe = Markup.Escape(Message);
        return new Markup($"{hints}\n[{color}]{safe}[/]");
    }
}

internal enum StatusKind
{
    None,
    Info,
    Error,
}
