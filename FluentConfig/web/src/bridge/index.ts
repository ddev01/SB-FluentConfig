import type { ChromeWebView } from './types';
import { tryGetMockWebView } from './devMock';

/**
 * Prefer the real WebView2 host object when present.
 * In production Vite aliases `./devMock` to an empty stub so `mockBridge` /
 * `mockDocument` are never pulled into the Release bundle.
 */
export function getBridge(): ChromeWebView {
  const real = window.chrome?.webview;
  if (real) return real;

  if (import.meta.env.DEV) {
    const mock = tryGetMockWebView();
    if (mock) return mock;
  }

  throw new Error(
    '[FluentConfig] chrome.webview host bridge is required in production builds',
  );
}

export function isMockBridge(): boolean {
  return import.meta.env.DEV && !window.chrome?.webview;
}
