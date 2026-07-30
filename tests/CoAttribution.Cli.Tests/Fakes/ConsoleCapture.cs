namespace CoAttribution.Cli.Tests.Fakes;

/// <summary>
/// Captures <see cref="Console.Out"/> and <see cref="Console.Error"/> into in-memory
/// buffers while a test is running, then restores the original writers in
/// <see cref="Dispose"/>. Use inside a TUnit <c>[Before(Test)]</c> / <c>[After(Test)]</c>
/// pair (or a <c>using</c> scope inside a single test) to keep output deterministic.
/// </summary>
public sealed class ConsoleCapture : IDisposable
{
    private readonly TextWriter _originalOut;
    private readonly TextWriter _originalError;
    private readonly StringWriter _outWriter = new();
    private readonly StringWriter _errorWriter = new();

    public ConsoleCapture()
    {
        _originalOut = Console.Out;
        _originalError = Console.Error;
        Console.SetOut(_outWriter);
        Console.SetError(_errorWriter);
    }

    /// <summary>Text written to <see cref="Console.Out"/> while captured.</summary>
    public string StandardOutput => _outWriter.ToString();

    /// <summary>Text written to <see cref="Console.Error"/> while captured.</summary>
    public string StandardError => _errorWriter.ToString();

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        Console.SetError(_originalError);
        _outWriter.Dispose();
        _errorWriter.Dispose();
    }
}
