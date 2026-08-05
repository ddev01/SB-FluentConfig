using System;

namespace FluentConfig
{
    /// <summary>
    /// Release-build HTML payload: Vite single-file bundle embedded at build time.
    /// Debug builds navigate WebView2 to http://localhost:5173 instead.
    /// </summary>
    public static partial class EmbeddedHtml
    {
        private static readonly string Placeholder =
            "<!DOCTYPE html><html><head><meta charset=\"utf-8\"/>" +
            "<meta http-equiv=\"Content-Security-Policy\" content=\"default-src 'none'; style-src 'unsafe-inline'; script-src 'unsafe-inline';\"/>" +
            "<title>FluentConfig</title>" +
            "<style>body{font-family:Segoe UI,sans-serif;background:#1e1e1e;color:#eee;margin:2rem}" +
            "code{background:#333;padding:2px 6px;border-radius:4px}</style></head><body>" +
            "<h1>FluentConfig</h1>" +
            "<p>Release HTML bundle is not embedded. Build with <code>-c Release</code> after " +
            "<code>bun install</code> in <code>FluentConfig/web</code>, or run a DEBUG build against " +
            "<code>http://localhost:5173</code>.</p>" +
            "<script>" +
            "function postReady(){" +
            "  try{ window.chrome.webview.postMessage(JSON.stringify({kind:'event',event:'web.ready'})); }catch(e){}" +
            "}" +
            "if(window.chrome&&window.chrome.webview){ postReady(); }" +
            "</script></body></html>";

        /// <summary>
        /// Full HTML document for <c>NavigateToString</c>. Prefer the embedded Vite bundle when present.
        /// </summary>
        public static string Content { get; private set; } = CreateContent();

        /// <summary>True when the Vite single-file bundle was compiled into this assembly.</summary>
        public static bool IsBundleEmbedded { get; private set; }

        private static string CreateContent()
        {
            string content = null;
            LoadEmbeddedBundle(ref content);
            if (!string.IsNullOrWhiteSpace(content))
            {
                IsBundleEmbedded = true;
                return content;
            }
            IsBundleEmbedded = false;
            return Placeholder;
        }

        /// <summary>
        /// Release builds supply a generated partial that assigns the Vite HTML.
        /// Debug builds leave this no-op so Content stays the placeholder (unused — DEBUG navigates to Vite).
        /// </summary>
        static partial void LoadEmbeddedBundle(ref string content);
    }
}
