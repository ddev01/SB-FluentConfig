import type { ColorScheme } from '../protocol';

const DARK_QUERY = '(prefers-color-scheme: dark)';

function systemIsDark(): boolean {
  return typeof window !== 'undefined' && window.matchMedia(DARK_QUERY).matches;
}

/** Apply Tailwind `dark:` via `class="dark"` on `<html>`, driven by bootstrap colorScheme. */
export function applyColorScheme(scheme: ColorScheme | undefined): void {
  const resolved = scheme ?? 'dark';
  const dark =
    resolved === 'dark' || (resolved === 'system' && systemIsDark());

  document.documentElement.classList.toggle('dark', dark);
  document.documentElement.style.colorScheme = dark ? 'dark' : 'light';
}

/** Listen for OS scheme changes when bootstrap asked for `system`. */
export function watchSystemScheme(scheme: ColorScheme | undefined): () => void {
  if (scheme !== 'system' || typeof window === 'undefined') {
    return () => {};
  }
  const mq = window.matchMedia(DARK_QUERY);
  const onChange = () => applyColorScheme('system');
  mq.addEventListener('change', onChange);
  return () => mq.removeEventListener('change', onChange);
}
