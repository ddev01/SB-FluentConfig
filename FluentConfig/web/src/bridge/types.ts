/**
 * Minimal WebView2 host object surface used by the UI.
 * Real host: `window.chrome.webview`. Dev: mock implementing the same shape.
 */

export interface WebViewMessageEvent {
  data: unknown;
}

export type WebViewMessageListener = (event: WebViewMessageEvent) => void;

export interface ChromeWebView {
  postMessage(message: unknown): void;
  addEventListener(type: 'message', listener: WebViewMessageListener): void;
  removeEventListener(type: 'message', listener: WebViewMessageListener): void;
}

declare global {
  interface Window {
    chrome?: {
      webview?: ChromeWebView;
    };
  }
}

export {};
