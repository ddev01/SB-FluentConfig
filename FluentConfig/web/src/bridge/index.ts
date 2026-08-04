import type { ChromeWebView } from './types';
import { getMockWebView } from './mockBridge';

/**
 * Prefer the real WebView2 host object when present; otherwise install the mock.
 * Runtime detection only — same production bundle works in both contexts.
 */
export function getBridge(): ChromeWebView {
  const real = window.chrome?.webview;
  if (real) return real;
  return getMockWebView();
}

export function isMockBridge(): boolean {
  return !window.chrome?.webview;
}
