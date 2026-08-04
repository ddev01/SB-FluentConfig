<script lang="ts">
  import type { DurationInputNode } from '../../protocol';
  import { appStore } from '../../store/app.svelte';
  import FieldShell from './FieldShell.svelte';

  interface Props {
    node: DurationInputNode;
  }

  let { node }: Props = $props();
  const fieldId = $derived(node.id ?? node.saveKey);

  const units = [
    { id: 'seconds', suffix: 'seconds', label: 'Seconds' },
    { id: 'minutes', suffix: 'minutes', label: 'Minutes' },
    { id: 'hours', suffix: 'hours', label: 'Hours' },
    { id: 'days', suffix: 'days', label: 'Days' },
  ] as const;

  function parse(raw: string): { amount: number; unit: string; permanent: boolean } {
    if (raw === 'permanent') return { amount: 0, unit: 'seconds', permanent: true };
    const m = /^(\d+)(seconds|minutes|hours|days)$/.exec(raw);
    if (m) return { amount: Number(m[1]), unit: m[2]!, permanent: false };
    return { amount: 30, unit: 'seconds', permanent: false };
  }

  let stored = $derived(
    String(appStore.getValue(node.saveKey) ?? node.defaultValue ?? '30seconds'),
  );
  let parsed = $derived(parse(stored));

  function write(amount: number, unit: string, permanent: boolean): void {
    if (permanent) {
      appStore.setValue(node.saveKey, 'permanent');
      return;
    }
    const n = Math.max(0, Math.floor(amount));
    appStore.setValue(node.saveKey, `${n}${unit}`);
  }
</script>

<FieldShell label={node.label} hint={node.hint} forId={fieldId}>
  <div class="flex flex-wrap items-center gap-2">
    {#if node.permanentOption}
      <label class="flex items-center gap-2 text-sm text-zinc-300">
        <input
          type="checkbox"
          class="rounded border-zinc-600 bg-zinc-900 text-sky-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-500"
          checked={parsed.permanent}
          onchange={(e) => {
            const on = (e.currentTarget as HTMLInputElement).checked;
            write(parsed.amount || 30, parsed.unit, on);
          }}
        />
        Permanent
      </label>
    {/if}
    <input
      id={fieldId}
      type="number"
      min="0"
      class="fc-input w-24 tabular-nums"
      disabled={parsed.permanent}
      value={parsed.permanent ? '' : parsed.amount}
      oninput={(e) => {
        const n = Number((e.currentTarget as HTMLInputElement).value);
        if (!Number.isNaN(n)) write(n, parsed.unit, false);
      }}
    />
    <select
      class="fc-input w-auto"
      disabled={parsed.permanent}
      value={parsed.unit}
      onchange={(e) => {
        write(parsed.amount || 0, (e.currentTarget as HTMLSelectElement).value, false);
      }}
    >
      {#each units as u (u.id)}
        <option value={u.suffix}>{u.label}</option>
      {/each}
    </select>
  </div>
</FieldShell>
