<script lang="ts">
  import type { NumberInputNode } from '../../protocol';
  import { appStore } from '../../store/app.svelte';
  import FieldShell from './FieldShell.svelte';

  interface Props {
    node: NumberInputNode;
  }

  let { node }: Props = $props();
  const fieldId = $derived(node.id ?? node.saveKey);
  const step = $derived(node.step ?? (node.valueType === 'int' ? 1 : 0.1));

  let value = $derived(
    appStore.getValue(node.saveKey) ?? node.defaultValue ?? 0,
  );

  /** Decimal places implied by step (e.g. 0.1 → 1) so 0.1+0.2 stays 0.3. */
  function stepDecimals(s: number): number {
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

  function roundToStep(raw: number): number {
    const s = step;
    if (!Number.isFinite(raw) || !Number.isFinite(s) || s <= 0) return raw;
    const rounded = Math.round(raw / s) * s;
    return Number(rounded.toFixed(stepDecimals(s)));
  }

  function coerce(raw: number): string | number {
    let n = roundToStep(raw);
    if (node.min !== undefined) n = Math.max(node.min, n);
    if (node.max !== undefined) n = Math.min(node.max, n);
    n = roundToStep(n);
    if (node.valueType === 'string') return String(n);
    if (node.valueType === 'int') return Math.round(n);
    return n;
  }

  function setFromNumber(n: number): void {
    appStore.setValue(node.saveKey, coerce(n));
  }

  function onInput(e: Event): void {
    const t = e.currentTarget as HTMLInputElement;
    const n = Number(t.value);
    if (Number.isNaN(n)) return;
    setFromNumber(n);
  }

  function bump(delta: number): void {
    const current = Number(value);
    setFromNumber((Number.isNaN(current) ? 0 : current) + delta);
  }
</script>

<FieldShell label={node.label} hint={node.hint} forId={fieldId}>
  <div class="flex items-center gap-2">
    {#if node.stepper}
      <button
        type="button"
        class="fc-btn flex h-9 w-9 items-center justify-center"
        aria-label="Decrease"
        onclick={() => bump(-step)}
      >
        −
      </button>
    {/if}
    <input
      id={fieldId}
      type="number"
      class="fc-input tabular-nums"
      class:fc-input-stepper={node.stepper}
      min={node.min}
      max={node.max}
      step={step}
      value={value}
      oninput={onInput}
    />
    {#if node.stepper}
      <button
        type="button"
        class="fc-btn flex h-9 w-9 items-center justify-center"
        aria-label="Increase"
        onclick={() => bump(step)}
      >
        +
      </button>
    {/if}
  </div>
</FieldShell>
