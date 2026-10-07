using System.Text;

namespace YtPlaylist;

sealed class FileWriter(string? filename = null) : IDisposable
{
    public string? Filename { get; set; } = filename;

    readonly StringBuilder _builder = new();

    public void Write(string v) => _builder.Append(v);
    public void Write(object v) => _builder.Append(v);
    public void WriteLine(string v) => _builder.AppendLine(v);
    public void WriteLine() => _builder.AppendLine();

    public void Dispose()
    {
        if (Filename is null) return;
        File.WriteAllText(Filename, _builder.ToString());
    }
}