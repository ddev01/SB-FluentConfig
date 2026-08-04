/**
 * Motion helpers (Workstream C).
 *
 * Motion One (`motion` / `animate`) pushes the single-file embed over the
 * 150 KB raw gate (~+62 KB for `animate` alone). Same public API is provided
 * via Svelte CSS transitions (compiled to @keyframes), so call sites stay stable.
 */
import type { TransitionConfig } from 'svelte/transition';
import { cubicOut } from 'svelte/easing';

export type MotionParams = {
  /** Duration in seconds (Motion One convention). Default varies by helper. */
  duration?: number;
  /** Delay in seconds. */
  delay?: number;
  /** Slide distance in px for `slideY` (positive = from below). */
  y?: number;
};

function ms(seconds: number): number {
  return seconds * 1000;
}

/** Fade opacity 0→1 (in) / 1→0 (out). */
export function fadeIn(
  _node: Element,
  { duration = 0.2, delay = 0 }: MotionParams = {},
): TransitionConfig {
  return {
    duration: ms(duration),
    delay: ms(delay),
    easing: cubicOut,
    css: (t) => `opacity: ${t}`,
  };
}

/** Slide along Y with fade. */
export function slideY(
  _node: Element,
  { duration = 0.22, delay = 0, y = 8 }: MotionParams = {},
): TransitionConfig {
  return {
    duration: ms(duration),
    delay: ms(delay),
    easing: cubicOut,
    css: (t) => {
      const offset = (1 - t) * y;
      return `opacity: ${t}; transform: translateY(${offset}px)`;
    },
  };
}

/** Scale up with fade. */
export function scaleIn(
  _node: Element,
  { duration = 0.2, delay = 0 }: MotionParams = {},
): TransitionConfig {
  return {
    duration: ms(duration),
    delay: ms(delay),
    easing: cubicOut,
    css: (t) => {
      const s = 0.96 + 0.04 * t;
      return `opacity: ${t}; transform: scale(${s})`;
    },
  };
}
