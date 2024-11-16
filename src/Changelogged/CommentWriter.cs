using Markdig.Syntax;

namespace Changelogged;

internal sealed class CommentWriter : IDisposable
{
    private readonly TextWriter _writer;

    private string? _section;
    private bool _inSection;

    public bool HasErrors { get; private set; }

    public CommentWriter()
    {
        _writer = Console.Error;
    }

    private void TryWriteSection()
    {
        if (_section is not null)
        {
            if (_inSection)
            {
                _writer.Write("::endgroup::");
                _inSection = false;
            }

            _writer.Write("::group::");
            _writer.WriteLine(_section);
            _section = null;
            _inSection = true;
        }
    }

    public void Section(string name)
    {
        _section = name;
    }

    private void WriteInfo(string message)
    {
        TryWriteSection();
        _writer.Write("::notice::");
        _writer.Write(message);
    }

    public void Info(string message)
    {
        WriteInfo(message);
        _writer.WriteLine();
    }

    public void Info(string message, SourceSpan span)
    {
        WriteInfo(message);
        WriteSpanSpaced(span);
    }

    private void WriteDebug(string message)
    {
        TryWriteSection();
        Console.WriteLine(message);
    }

    public void Debug(string message)
    {
        WriteDebug(message);
        _writer.WriteLine();
    }

    public void Debug(string message, SourceSpan span)
    {
        WriteDebug(message);
        WriteSpanSpaced(span);
    }

    private void WriteWarn(string message)
    {
        TryWriteSection();
        _writer.Write("::warning::");
        _writer.Write(message);
    }

    public void Warn(string message)
    {
        WriteWarn(message);
        _writer.WriteLine();
    }

    public void Warn(string message, SourceSpan span)
    {
        WriteWarn(message);
        WriteSpanSpaced(span);
    }

    private void WriteError(string message)
    {
        TryWriteSection();
        HasErrors = true;
        _writer.Write("::error::");
        _writer.Write(message);
    }

    public void Error(string message)
    {
        WriteError(message);
        _writer.WriteLine();
    }

    public void Error(string message, SourceSpan span)
    {
        WriteError(message);
        WriteSpanSpaced(span);
    }

    private void WriteSpanSpaced(SourceSpan span)
    {
        _writer.Write(' ');
        WriteSpan(span);
    }

    private void WriteSpan(SourceSpan span)
    {
        _writer.Write("(");
        _writer.Write(span.Start);
        _writer.Write(":");
        _writer.Write(span.End);
        _writer.Write(")");
    }

    public void Dispose()
    {
        if (_inSection)
        {
            _writer.Write("::endgroup::");
            _inSection = false;
        }
    }
}
