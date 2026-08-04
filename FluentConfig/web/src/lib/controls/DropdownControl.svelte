<script lang="ts">
  import type { DropdownNode } from '../../protocol';
  import { RpcMethods } from '../../protocol';
  import type { DropdownRefreshResult } from '../../protocol';
  import { appStore } from '../../store/app.svelte';
  import FieldShell from './FieldShell.svelte';
  import SelectMenu from './SelectMenu.svelte';

  interface Props {
    node: DropdownNode;
  }

  let { node }: Props = $props();
  const fieldId = $derived(node.id ?? node.saveKey);

  let refreshing = $state(false);

  let selectedValue = $derived.by(() => {
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
    if (node.defaultIndex !== undefined && node.options?.[node.defaultIndex]) {
      return node.options[node.defaultIndex]!.value;
    }
    return node.options?.[0]?.value ?? '';
  });

  function onChange(newValue: string): void {
    const opt = node.options?.find((o) => o.value === newValue);
    if (!opt) return;
    if (node.valueSaveKey) {
      appStore.setValue(node.saveKey, opt.display);
      appStore.setValue(node.valueSaveKey, opt.value);
    } else {
      appStore.setValue(node.saveKey, opt.value);
    }
  }

  async function refresh(): Promise<void> {
    refreshing = true;
    try {
      const result = await appStore.client().request<DropdownRefreshResult>(
        RpcMethods.DropdownRefresh,
        { saveKey: node.saveKey },
      );
      appStore.patchDropdownOptions(node.saveKey, result.options);
    } catch (err) {
      appStore.pushToast(err instanceof Error ? err.message : 'Refresh failed');
    } finally {
      refreshing = false;
    }
  }
</script>

<FieldShell label={node.label} hint={node.hint} forId={fieldId}>
  <div class="flex gap-2">
    <SelectMenu
      id={fieldId}
      value={selectedValue}
      options={node.options ?? []}
      ariaLabel={node.label}
      onchange={onChange}
    />
    {#if node.refreshable}
      <button type="button" class="fc-btn" disabled={refreshing} onclick={refresh}>
        {refreshing ? '…' : 'Refresh'}
      </button>
    {/if}
  </div>
</FieldShell>
