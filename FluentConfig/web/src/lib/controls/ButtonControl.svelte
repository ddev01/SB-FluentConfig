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

  const label = $derived(node.text ?? node.label ?? 'Button');

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

<FieldShell label={node.label && node.text ? node.label : undefined} hint={node.hint}>
  <button
    type="button"
    class="rounded-md px-4 py-2 text-sm font-medium text-white transition-opacity hover:opacity-90 disabled:opacity-50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-500"
    style:background-color={node.color ?? '#3b82f6'}
    disabled={busy}
    onclick={click}
  >
    {busy ? 'Working…' : label}
  </button>
</FieldShell>
