/** Step-aware rounding so 0.1 + 0.2 lands on 0.3 rather than 0.30000000000000004. */

export function stepDecimals(s: number): number {
  if (!Number.isFinite(s) || s <= 0) return 0;
  const text = String(s);
  if (text.includes('e') || text.includes('E')) {
    const fixed = s.toFixed(10).replace(/\.?0+$/, '');
    const dot = fixed.indexOf('.');
    return dot === -1 ? 0 : fixed.length - dot - 1;
  }
  const dot = text.indexOf('.');
  return dot === -1 ? 0 : text.length - dot - 1;
}

export function roundToStep(raw: number, step: number): number {
  if (!Number.isFinite(raw) || !Number.isFinite(step) || step <= 0) return raw;
  const rounded = Math.round(raw / step) * step;
  return Number(rounded.toFixed(stepDecimals(step)));
}
