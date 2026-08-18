<script lang="ts">
  import type { ButtonNode } from '../../protocol';
  import { RpcMethods } from '../../protocol';
  import { appStore } from '../../store/app.svelte';
  import FieldShell from './FieldShell.svelte';

  interface Props {
    node: ButtonNode;
  }

  let { node }: Props = $props();
  let busy = $state(false);

  const caption = $derived(node.text ?? node.label ?? 'Button');
  const shellLabel = $derived(
    node.label && node.text && node.label !== node.text ? node.label : undefined,
  );
  const compact = $derived(!shellLabel);

  async function click(): Promise<void> {
    busy = true;
    try {
      const result = await appStore.client().request<{ toast?: string }>(
        RpcMethods.ButtonClick,
        { buttonId: node.id, values: appStore.values },
      );
      if (result?.toast) appStore.pushToast(result.toast);
    } catch (err) {
      appStore.pushToast(err instanceof Error ? err.message : 'Action failed');
    } finally {
      busy = false;
    }
  }
</script>

<FieldShell label={shellLabel} hint={compact ? undefined : node.hint}>
  <button
    type="button"
    class="rounded-fc px-4 py-2 text-sm font-semibold text-white transition-[filter] duration-150 hover:brightness-110 active:brightness-95 disabled:pointer-events-none disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-fc-ring/50 focus-visible:ring-offset-2 focus-visible:ring-offset-fc-bg"
    style:background-color={node.color ?? '#3b82f6'}
    title={node.hint}
    disabled={busy}
    onclick={click}
  >
    {busy ? 'Working…' : caption}
  </button>
</FieldShell>
