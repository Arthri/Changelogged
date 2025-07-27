using Markdig.Syntax;

namespace Changelogged;

internal sealed class CommentWriter : IDisposable
{
    private readonly TextWriter _writer;

    // Empty sections are not desired, so this field stores the section
    // that will be written to the output when any log message is written.
    // If this field has a non-null value and it gets overwritten, then it
    // means that the section was empty
    private string? _deferredSection;
    // This field tracks whether a section was previously written.
    // GitHub Actions only allows one-level deep groups and therefore
    // older sections must be closed before beginning a new one
    private bool _inSection;

    public bool HasErrors { get; private set; }

    public CommentWriter()
    {
        _writer = Console.Error;
    }

    private void TryWriteSection()
    {
        if (_deferredSection is not null)
        {
            if (_inSection)
            {
                _writer.WriteLine("::endgroup::");
                _inSection = false;
            }

            _writer.Write("::group::");
            _writer.WriteLine(_deferredSection);
            _deferredSection = null;
            _inSection = true;
        }
    }

    public void Section(string name)
    {
        _deferredSection = name;
    }

    public void EndSection()
    {
        if (_deferredSection is not null)
        {
            _deferredSection = null;
        }

        if (_inSection)
        {
            _writer.WriteLine("::endgroup::");
            _inSection = false;
        }
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
        WriteSpan(span);
        _writer.WriteLine();
    }

    private void WriteDebug(string message)
    {
        TryWriteSection();
        _writer.Write(message);
    }

    public void Debug(string message)
    {
        WriteDebug(message);
        _writer.WriteLine();
    }

    public void Debug(string message, SourceSpan span)
    {
        WriteDebug(message);
        WriteSpan(span);
        _writer.WriteLine();
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
        WriteSpan(span);
        _writer.WriteLine();
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
        WriteSpan(span);
        _writer.WriteLine();
    }

    private void WriteSpan(SourceSpan span)
    {
        _writer.Write(" (");
        _writer.Write(span.Start);
        _writer.Write(":");
        _writer.Write(span.End);
        _writer.Write(")");
    }

    public void Dispose()
    {
        if (_inSection)
        {
            _writer.WriteLine("::endgroup::");
            _inSection = false;
        }
    }
}
