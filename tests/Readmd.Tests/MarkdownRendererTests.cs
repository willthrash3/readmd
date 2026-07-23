using Readmd.Documents;
using Readmd.Rendering;

namespace Readmd.Tests;

public sealed class MarkdownRendererTests
{
    private readonly MarkdownRenderer _renderer = new();

    [Fact]
    public void RendersCommonMarkdownSyntax()
    {
        var markdown = """
            # Heading 1

            Paragraph with **strong**, *emphasis*, ~~strike~~, `inline code`, and [a link](https://example.com).

            > Quoted text

            - item one
            - [x] complete task
            - [ ] incomplete task

            1. first
            2. second

            | Name | Value |
            | --- | ---: |
            | alpha | 1 |

            ```csharp
            Console.WriteLine("hello");
            ```

            ---

            ![alt text](images/example.png)
            """;

        var html = _renderer.RenderDocument(markdown, @"C:\docs\sample.md");

        Assert.Contains("<h1", html);
        Assert.Contains("<strong>strong</strong>", html);
        Assert.Contains("<em>emphasis</em>", html);
        Assert.Contains("<del>strike</del>", html);
        Assert.Contains("<blockquote>", html);
        Assert.Contains("<ul", html);
        Assert.Contains("type=\"checkbox\"", html);
        Assert.Contains("checked", html);
        Assert.Contains("disabled", html);
        Assert.Contains("<ol>", html);
        Assert.Contains("<table>", html);
        Assert.Contains("class=\"language-csharp\"", html);
        Assert.Contains("<hr", html);
        Assert.Contains("src=\"images/example.png\"", html);
        Assert.Contains("<base href=\"file:///C:/docs/\">", html);
    }

    [Fact]
    public void RendersMermaidCodeFenceAsDiagramContainer()
    {
        var html = _renderer.RenderDocument("""
            ```mermaid
            flowchart LR
                A[Markdown] --> B[HTML]
            ```
            """);

        Assert.Contains("""<div class="mermaid">""", html);
        Assert.Contains("flowchart LR", html);
        Assert.DoesNotContain("class=\"language-mermaid\"", html);
    }

    [Fact]
    public void RenderedDocumentFollowsTheSystemColorScheme()
    {
        var html = _renderer.RenderDocument("# Theme");

        Assert.Contains("color-scheme: light dark", html);
        Assert.Contains("@media (prefers-color-scheme: dark)", html);
        Assert.Contains("""matchMedia("(prefers-color-scheme: dark)")""", html);
        Assert.Contains("theme: darkMode ? \"dark\" : \"default\"", html);
    }

    [Fact]
    public void RendersAdvancedMarkdownExtensions()
    {
        var html = _renderer.RenderDocument("""
            Setext heading
            ==============

            Term
            :   Definition text

            A footnote reference.[^note]

            [^note]: Footnote detail

            Autolink: <https://example.com/path>.
            """);

        Assert.Contains("<h1", html);
        Assert.Contains("<dl>", html);
        Assert.Contains("<dt>Term</dt>", html);
        Assert.Contains("<dd>Definition text</dd>", html);
        Assert.Contains("Footnote detail", html);
        Assert.Contains("https://example.com/path", html);
    }

    [Fact]
    public void DoesNotRenderRawHtmlFromMarkdownFiles()
    {
        var html = _renderer.RenderDocument("""
            <script>window.bad = true;</script>

            <div>raw html</div>
            """);

        Assert.DoesNotContain("<script>window.bad = true;</script>", html);
        Assert.DoesNotContain("<div>raw html</div>", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("&lt;div&gt;raw html&lt;/div&gt;", html);
    }

    [Fact]
    public void LoadsFileWithoutExclusiveLocking()
    {
        var directory = Path.Combine(Path.GetTempPath(), "readmd-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "shared.md");
        File.WriteAllText(path, "# Shared");

        using var writer = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
        var document = RenderedDocument.Load(path, _renderer);

        Assert.Contains("Shared", document.Html);
        Assert.Equal("shared.md", document.Title);
    }
}
