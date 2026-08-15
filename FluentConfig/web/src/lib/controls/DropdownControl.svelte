<script lang="ts">
  import { onMount } from 'svelte';
  import type { DropdownNode } from '../../protocol';
  import { RpcMethods } from '../../protocol';
  import type { DropdownRefreshResult } from '../../protocol';
  import { appStore } from '../../store/app.svelte';
  import { resolveDropdownRefresh } from '../dropdownRefresh';
  import FieldShell from './FieldShell.svelte';
  import SelectMenu from './SelectMenu.svelte';

  interface Props {
    node: DropdownNode;
  }

  let { node }: Props = $props();
  const fieldId = $derived(node.id ?? node.saveKey);
  const searchable = $derived(!!node.searchable);
  const allowCustom = $derived(!!node.allowCustom);
  const multiple = $derived(!!node.multiple);

  let refreshing = $state(false);

  onMount(() => {
    if (node.refreshable && !(node.options?.length ?? 0)) {
      void refresh();
    }
  });

  let selectedValue = $derived.by(() => {
    if (multiple) return '';
    if (node.valueSaveKey) {
      const pair = appStore.getValue(node.valueSaveKey);
      if (pair != null && pair !== '') return String(pair);
    }
    const display = appStore.getValue(node.saveKey);
    if (display != null && display !== '') {
      const match = node.options?.find((o) => o.display === display || o.value === display);
      if (match) return match.value;
      return String(display);
    }
    if (node.defaultByValue) return node.defaultByValue;
    if (node.defaultValue) return node.defaultValue;
    if (node.defaultIndex !== undefined && node.options?.[node.defaultIndex]) {
      return node.options[node.defaultIndex]!.value;
    }
    return node.options?.[0]?.value ?? '';
  });

  let selectedValues = $derived.by(() => {
    if (!multiple) return [] as string[];
    const raw = appStore.getValue(node.saveKey);
    if (Array.isArray(raw)) return raw.map((x) => String(x));
    if (typeof raw === 'string' && raw) return [raw];
    if (node.defaultValues?.length) return [...node.defaultValues];
    return [];
  });

  function onChange(newValue: string): void {
    const opt = node.options?.find((o) => o.value === newValue);
    if (!opt && !allowCustom) return;
    if (node.valueSaveKey) {
      appStore.setValue(node.saveKey, opt?.display ?? newValue);
      appStore.setValue(node.valueSaveKey, opt?.value ?? newValue);
    } else {
      appStore.setValue(node.saveKey, opt?.value ?? newValue);
    }
  }

  function onChangeValues(next: string[]): void {
    appStore.setValue(node.saveKey, next);
  }

  async function refresh(): Promise<void> {
    refreshing = true;
    try {
      const result = await appStore.client().request<DropdownRefreshResult>(
        RpcMethods.DropdownRefresh,
        { saveKey: node.saveKey },
      );
      appStore.patchDropdownOptions(node.saveKey, result.options);

      const opts = result.options ?? [];
      const decision = resolveDropdownRefresh({
        allowCustom,
        multiple,
        current: multiple ? selectedValues : selectedValue,
        options: opts,
      });
      if (decision.action === 'keep') return;
      if (decision.action === 'keepValues') {
        appStore.setValue(node.saveKey, decision.values);
        return;
      }
      if (decision.action === 'snap') {
        const first = decision.option;
        if (node.valueSaveKey) {
          appStore.setValue(node.saveKey, first.display);
          appStore.setValue(node.valueSaveKey, first.value);
        } else {
          appStore.setValue(node.saveKey, first.value);
        }
        return;
      }
      appStore.setValue(node.saveKey, multiple ? [] : '');
      if (node.valueSaveKey) appStore.setValue(node.valueSaveKey, '');
    } catch (err) {
      appStore.pushToast(err instanceof Error ? err.message : 'Refresh failed');
    } finally {
      refreshing = false;
    }
  }
</script>

<FieldShell label={node.label} hint={node.hint} forId={fieldId}>
  <div class="flex items-end gap-2">
    <SelectMenu
      id={fieldId}
      value={selectedValue}
      values={selectedValues}
      options={node.options ?? []}
      ariaLabel={node.label}
      searchable={searchable}
      allowCustom={allowCustom}
      multiple={multiple}
      onchange={onChange}
      onchangeValues={onChangeValues}
    />
    {#if node.refreshable}
      <button type="button" class="fc-btn" disabled={refreshing} onclick={refresh}>
        {refreshing ? '…' : 'Refresh'}
      </button>
    {/if}
  </div>
</FieldShell>
