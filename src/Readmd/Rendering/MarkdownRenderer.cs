using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using Markdig;

namespace Readmd.Rendering;

public sealed partial class MarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    public string RenderDocument(string markdown, string? sourcePath = null)
    {
        var body = Markdown.ToHtml(markdown, Pipeline);
        body = MermaidFenceRegex().Replace(body, match => $"<div class=\"mermaid\">{match.Groups["diagram"].Value}</div>");
        return RenderHtmlShell(body, sourcePath);
    }

    public static string RenderErrorDocument(string heading, string detail)
    {
        var body = $"""
            <h1>{WebUtility.HtmlEncode(heading)}</h1>
            <pre><code>{WebUtility.HtmlEncode(detail)}</code></pre>
            """;
        return RenderHtmlShell(body, null);
    }

    private static string RenderHtmlShell(string body, string? sourcePath)
    {
        var baseHref = GetBaseHref(sourcePath);
        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src file: data: https:; style-src 'unsafe-inline'; script-src https://cdn.jsdelivr.net 'unsafe-inline'; connect-src https://cdn.jsdelivr.net; font-src data:;">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              {{baseHref}}
              <style>
                :root {
                  color-scheme: light;
                  --border: #d0d7de;
                  --muted: #57606a;
                  --code-bg: #f6f8fa;
                }

                html, body {
                  margin: 0;
                  min-height: 100%;
                  background: #ffffff;
                  color: #1f2328;
                  font-family: "Segoe UI", system-ui, -apple-system, BlinkMacSystemFont, sans-serif;
                  font-size: 15px;
                  line-height: 1.55;
                }

                main {
                  box-sizing: border-box;
                  max-width: 1040px;
                  min-height: 100vh;
                  padding: 28px 38px 56px;
                }

                h1, h2, h3, h4, h5, h6 {
                  margin: 1.2em 0 0.55em;
                  line-height: 1.25;
                  font-weight: 600;
                }

                h1 {
                  padding-bottom: 0.3em;
                  border-bottom: 1px solid var(--border);
                  font-size: 2em;
                }

                h2 {
                  padding-bottom: 0.25em;
                  border-bottom: 1px solid var(--border);
                  font-size: 1.45em;
                }

                h3 {
                  font-size: 1.2em;
                }

                p, blockquote, ul, ol, table, pre {
                  margin-top: 0;
                  margin-bottom: 16px;
                }

                a {
                  color: #0969da;
                  text-decoration: none;
                }

                a:hover {
                  text-decoration: underline;
                }

                blockquote {
                  margin-left: 0;
                  padding: 0 1em;
                  color: var(--muted);
                  border-left: 4px solid var(--border);
                }

                code, kbd, pre {
                  font-family: Consolas, "Cascadia Mono", "Courier New", monospace;
                  font-size: 0.94em;
                }

                code {
                  padding: 0.15em 0.35em;
                  background: var(--code-bg);
                  border-radius: 4px;
                }

                pre {
                  overflow: auto;
                  padding: 14px 16px;
                  background: var(--code-bg);
                  border: 1px solid #eaeef2;
                  border-radius: 6px;
                }

                pre code {
                  padding: 0;
                  background: transparent;
                  border-radius: 0;
                }

                table {
                  display: block;
                  width: max-content;
                  max-width: 100%;
                  overflow: auto;
                  border-collapse: collapse;
                }

                th, td {
                  padding: 6px 13px;
                  border: 1px solid var(--border);
                }

                tr:nth-child(2n) {
                  background: #f6f8fa;
                }

                img {
                  max-width: 100%;
                  height: auto;
                }

                hr {
                  height: 0.25em;
                  padding: 0;
                  margin: 24px 0;
                  background: #d8dee4;
                  border: 0;
                }

                .task-list-item {
                  list-style-type: none;
                }

                .task-list-item input {
                  margin: 0 0.45em 0.25em -1.6em;
                  vertical-align: middle;
                }

                .mermaid {
                  margin: 18px 0;
                  padding: 16px;
                  overflow: auto;
                  background: #fbfbfb;
                  border: 1px solid var(--border);
                  border-radius: 6px;
                }
              </style>
            </head>
            <body>
              <main>
                {{body}}
              </main>
              <script src="https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js"></script>
              <script>
                if (window.mermaid) {
                  window.mermaid.initialize({ startOnLoad: true, securityLevel: "strict" });
                }
              </script>
            </body>
            </html>
            """;
    }

    private static string GetBaseHref(string? sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return string.Empty;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(sourcePath));
        if (string.IsNullOrWhiteSpace(directory))
        {
            return string.Empty;
        }

        var uri = new Uri(directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar);
        return $"""<base href="{WebUtility.HtmlEncode(uri.AbsoluteUri)}">""";
    }

    [GeneratedRegex("<pre><code class=\"language-mermaid\">(?<diagram>[\\s\\S]*?)</code></pre>", RegexOptions.IgnoreCase)]
    private static partial Regex MermaidFenceRegex();
}
