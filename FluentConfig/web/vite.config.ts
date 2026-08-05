import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { defineConfig, type Plugin } from 'vite'
import { svelte } from '@sveltejs/vite-plugin-svelte'
import tailwindcss from '@tailwindcss/vite'
import { viteSingleFile } from 'vite-plugin-singlefile'

const root = path.dirname(fileURLToPath(import.meta.url))

/** Informal Release gate: single-file HTML should stay near ~150 KB after mock exclusion. */
const BUNDLE_SIZE_SOFT_MAX = 158_000

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

/** Fail the production build if the inlined bundle grows past the informal soft gate. */
function bundleSizeGatePlugin(): Plugin {
  return {
    name: 'fluentconfig-bundle-size-gate',
    apply: 'build',
    // After vite-plugin-singlefile writes the inlined HTML.
    async closeBundle() {
      const { readFile } = await import('node:fs/promises')
      const htmlPath = path.resolve(root, 'dist/index.html')
      let bytes: number
      try {
        const html = await readFile(htmlPath, 'utf8')
        bytes = Buffer.byteLength(html, 'utf8')
        if (html.includes('FluentConfig Dev Preview')) {
          throw new Error(
            '[FluentConfig] dist HTML still contains mock bridge strings (FluentConfig Dev Preview).',
          )
        }
      } catch (err) {
        if (err && typeof err === 'object' && 'code' in err && err.code === 'ENOENT') {
          throw new Error('[FluentConfig] dist/index.html missing after build')
        }
        throw err
      }
      if (bytes > BUNDLE_SIZE_SOFT_MAX) {
        throw new Error(
          `[FluentConfig] dist HTML is ${bytes} bytes (soft max ${BUNDLE_SIZE_SOFT_MAX}). ` +
            'Mock bridge should be prod-excluded; investigate unexpected growth.',
        )
      }
      console.log(
        `[FluentConfig] bundle size: ${bytes} bytes (soft max ${BUNDLE_SIZE_SOFT_MAX})`,
      )
    },
  }
}

export default defineConfig(({ mode }) => ({
  plugins: [
    tailwindcss(),
    svelte(),
    viteSingleFile(),
    cspMetaPlugin(),
    bundleSizeGatePlugin(),
  ],
  resolve: {
    alias:
      mode === 'production'
        ? {
            // Drop mock bridge/document from the Release module graph.
            [path.resolve(root, 'src/bridge/devMock.ts')]: path.resolve(
              root,
              'src/bridge/devMock.empty.ts',
            ),
          }
        : {},
  },
}))
