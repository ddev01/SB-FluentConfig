/** Context key: when set, FieldShell applies sizing to the input row only (label stays full width). */
export const FIT_WIDTH_CONTEXT = 'fc-fit-width';

/** Digit count for a finite number (includes sign). Falls back when undefined. */
export function digitCount(n: number | undefined, fallback = 4): number {
  if (n === undefined || !Number.isFinite(n)) return fallback;
  return String(Math.trunc(n)).length;
}
