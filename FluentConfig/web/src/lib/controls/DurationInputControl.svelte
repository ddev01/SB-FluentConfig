<script lang="ts">
  import type { DurationInputNode } from '../../protocol';
  import { appStore } from '../../store/app.svelte';
  import { formatDuration, parseDuration } from '../duration';
  import Checkbox from '../Checkbox.svelte';
  import FieldShell from './FieldShell.svelte';
  import SelectMenu from './SelectMenu.svelte';

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

  let stored = $derived(
    String(appStore.getValue(node.saveKey) ?? node.defaultValue ?? '30seconds'),
  );
  let parsed = $derived(parseDuration(stored));

  function write(amount: number, unit: string, permanent: boolean): void {
    appStore.setValue(node.saveKey, formatDuration(amount, unit, permanent));
  }
</script>

<FieldShell label={node.label} hint={node.hint} forId={fieldId}>
  <div class="flex flex-wrap items-center gap-2">
    {#if node.permanentOption}
      <Checkbox
        checked={parsed.permanent}
        onchange={(on) => write(parsed.amount || 30, parsed.unit, on)}
      >
        Permanent
      </Checkbox>
    {/if}
    {#if !parsed.permanent}
      <input
        id={fieldId}
        type="number"
        min="0"
        class="fc-input w-24 tabular-nums"
        value={parsed.amount}
        oninput={(e) => {
          const n = Number((e.currentTarget as HTMLInputElement).value);
          if (!Number.isNaN(n)) write(n, parsed.unit, false);
        }}
      />
      <div class="w-32">
        <SelectMenu
          value={parsed.unit}
          options={units.map((u) => ({ value: u.suffix, display: u.label }))}
          ariaLabel="Duration unit"
          onchange={(unit) => write(parsed.amount || 0, unit, false)}
        />
      </div>
    {/if}
  </div>
</FieldShell>
