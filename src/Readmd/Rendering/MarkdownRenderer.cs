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
                  color-scheme: light dark;
                  --page: #f5f7fa;
                  --surface: #ffffff;
                  --text: #1f2933;
                  --muted: #64717f;
                  --border: #dce3ea;
                  --border-strong: #c8d2dc;
                  --accent: #2f7d67;
                  --accent-soft: #e1f1ec;
                  --link: #1f6f8b;
                  --code-bg: #f3f5f7;
                  --code-border: #dce5ec;
                  --heading: #17212b;
                  --body-text: #283440;
                  --inset: #f7faf9;
                  --table-heading: #eef5f2;
                  --table-stripe: #f7f9fb;
                  --page-edge: #e8edf2;
                }

                @media (prefers-color-scheme: dark) {
                  :root {
                    --page: #12161b;
                    --surface: #14191e;
                    --text: #e2e8f0;
                    --muted: #9eabb8;
                    --border: #37404a;
                    --border-strong: #4a5662;
                    --accent: #5bb89b;
                    --accent-soft: #1f453b;
                    --link: #76bdd5;
                    --code-bg: #20262d;
                    --code-border: #3a4550;
                    --heading: #f0f4f8;
                    --body-text: #d2dae3;
                    --inset: #1b2423;
                    --table-heading: #20312d;
                    --table-stripe: #1a2026;
                    --page-edge: #303943;
                  }
                }

                html, body {
                  margin: 0;
                  min-height: 100%;
                  background: var(--page);
                  color: var(--text);
                  font-family: "Segoe UI", system-ui, -apple-system, BlinkMacSystemFont, sans-serif;
                  font-size: 15px;
                  line-height: 1.62;
                }

                main {
                  box-sizing: border-box;
                  max-width: 980px;
                  min-height: 100vh;
                  margin: 0 auto;
                  padding: 44px 54px 72px;
                  background: var(--surface);
                  border-left: 1px solid var(--page-edge);
                  border-right: 1px solid var(--page-edge);
                }

                h1, h2, h3, h4, h5, h6 {
                  margin: 1.35em 0 0.55em;
                  line-height: 1.25;
                  font-weight: 600;
                  letter-spacing: 0;
                  color: var(--heading);
                }

                h1:first-child,
                h2:first-child,
                h3:first-child {
                  margin-top: 0;
                }

                h1 {
                  padding-bottom: 0.35em;
                  border-bottom: 1px solid var(--border-strong);
                  font-size: 2.15em;
                }

                h2 {
                  padding-bottom: 0.3em;
                  border-bottom: 1px solid var(--border);
                  font-size: 1.5em;
                }

                h3 {
                  font-size: 1.22em;
                }

                p, blockquote, ul, ol, table, pre {
                  margin-top: 0;
                  margin-bottom: 16px;
                }

                p {
                  color: var(--body-text);
                }

                a {
                  color: var(--link);
                  text-decoration: none;
                  text-underline-offset: 0.18em;
                }

                a:hover {
                  text-decoration: underline;
                }

                ul, ol {
                  padding-left: 1.6em;
                }

                li + li {
                  margin-top: 0.25em;
                }

                blockquote {
                  margin-left: 0;
                  padding: 0.75em 1em;
                  color: var(--muted);
                  background: var(--inset);
                  border-left: 4px solid var(--accent);
                  border-radius: 0 6px 6px 0;
                }

                code, kbd, pre {
                  font-family: Consolas, "Cascadia Mono", "Courier New", monospace;
                  font-size: 0.93em;
                }

                code {
                  padding: 0.15em 0.35em;
                  background: var(--code-bg);
                  border: 1px solid var(--code-border);
                  border-radius: 4px;
                }

                pre {
                  overflow: auto;
                  padding: 16px 18px;
                  background: var(--code-bg);
                  border: 1px solid var(--code-border);
                  border-radius: 6px;
                }

                pre code {
                  padding: 0;
                  background: transparent;
                  border: 0;
                  border-radius: 0;
                }

                table {
                  display: block;
                  width: max-content;
                  max-width: 100%;
                  overflow: auto;
                  border-collapse: separate;
                  border-spacing: 0;
                  border: 1px solid var(--border);
                  border-radius: 6px;
                }

                th, td {
                  padding: 8px 13px;
                  border-right: 1px solid var(--border);
                  border-bottom: 1px solid var(--border);
                }

                th {
                  background: var(--table-heading);
                  font-weight: 600;
                }

                td:last-child,
                th:last-child {
                  border-right: 0;
                }

                tr:last-child td {
                  border-bottom: 0;
                }

                tr:nth-child(2n) td {
                  background: var(--table-stripe);
                }

                img {
                  max-width: 100%;
                  height: auto;
                  border-radius: 6px;
                }

                hr {
                  height: 1px;
                  padding: 0;
                  margin: 28px 0;
                  background: var(--border);
                  border: 0;
                }

                .task-list-item {
                  list-style-type: none;
                }

                .task-list-item input {
                  margin: 0 0.45em 0.25em -1.6em;
                  vertical-align: middle;
                  accent-color: var(--accent);
                }

                .mermaid {
                  margin: 20px 0;
                  padding: 18px;
                  overflow: auto;
                  background: var(--inset);
                  border: 1px solid var(--border);
                  border-radius: 6px;
                }

                @media (max-width: 720px) {
                  main {
                    padding: 28px 24px 56px;
                    border-left: 0;
                    border-right: 0;
                  }

                  h1 {
                    font-size: 1.75em;
                  }
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
                  const darkMode = window.matchMedia("(prefers-color-scheme: dark)").matches;
                  window.mermaid.initialize({ startOnLoad: true, securityLevel: "strict", theme: darkMode ? "dark" : "default" });
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
