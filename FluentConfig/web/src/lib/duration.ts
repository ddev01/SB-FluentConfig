export type DurationParsed = {
  amount: number;
  unit: string;
  permanent: boolean;
};

const UNIT_RE = /^(\d+)(seconds|minutes|hours|days)$/;

export function parseDuration(raw: string): DurationParsed {
  if (raw === 'permanent') return { amount: 0, unit: 'seconds', permanent: true };
  const m = UNIT_RE.exec(raw);
  if (m) return { amount: Number(m[1]), unit: m[2]!, permanent: false };
  return { amount: 30, unit: 'seconds', permanent: false };
}

export function formatDuration(amount: number, unit: string, permanent: boolean): string {
  if (permanent) return 'permanent';
  const n = Math.max(0, Math.floor(amount));
  return `${n}${unit}`;
}
