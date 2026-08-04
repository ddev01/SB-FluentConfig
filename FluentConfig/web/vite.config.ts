import { defineConfig, type Plugin } from 'vite'
import { svelte } from '@sveltejs/vite-plugin-svelte'
import tailwindcss from '@tailwindcss/vite'
import { viteSingleFile } from 'vite-plugin-singlefile'

/**
 * Production CSP: page is fully self-contained; no remote fetch/XHR/WS.
 * Skipped during `vite` dev so HMR websockets still work.
 * Scripts/styles are inlined by vite-plugin-singlefile → unsafe-inline required.
 */
function cspMetaPlugin(): Plugin {
  const csp = [
    "default-src 'none'",
    "script-src 'unsafe-inline'",
    "style-src 'unsafe-inline'",
    "img-src data: blob:",
    "font-src data:",
    "connect-src 'none'",
    "frame-src 'none'",
    "object-src 'none'",
    "base-uri 'none'",
    "form-action 'none'",
  ].join('; ')

  return {
    name: 'fluentconfig-csp',
    transformIndexHtml: {
      order: 'post',
      handler(html, ctx) {
        if (ctx.server) return html
        if (html.includes('Content-Security-Policy')) return html
        return html.replace(
          /<head>/i,
          `<head>\n    <meta http-equiv="Content-Security-Policy" content="${csp}" />`,
        )
      },
    },
  }
}

export default defineConfig({
  plugins: [tailwindcss(), svelte(), viteSingleFile(), cspMetaPlugin()],
})
