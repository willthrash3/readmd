using System.IO;
using Readmd.Rendering;

namespace Readmd.Documents;

public sealed record RenderedDocument(
    string Title,
    string FilePath,
    string Html,
    string StatusText,
    bool IsBlank)
{
    public static RenderedDocument Blank(string title, string html)
    {
        return new RenderedDocument(title, string.Empty, html, "Read-only", true);
    }

    public static RenderedDocument Load(string path, MarkdownRenderer renderer)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var markdown = SharedFileReader.ReadAllText(fullPath);
            var html = renderer.RenderDocument(markdown, fullPath);
            var title = Path.GetFileName(fullPath);
            return new RenderedDocument(title, fullPath, html, $"{fullPath} | Read-only", false);
        }
        catch (Exception ex)
        {
            var title = string.IsNullOrWhiteSpace(path) ? "Open error" : Path.GetFileName(path);
            var html = MarkdownRenderer.RenderErrorDocument($"Could not open {title}.", ex.Message);
            return new RenderedDocument(title, path, html, $"{path} | Open failed", false);
        }
    }
}
