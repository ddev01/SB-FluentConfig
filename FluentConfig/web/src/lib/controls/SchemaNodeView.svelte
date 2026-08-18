<script lang="ts">
  import { setContext } from 'svelte';
  import type { SchemaNode } from '../../protocol';
  import { isVisible } from '../visibility';
  import { appStore } from '../../store/app.svelte';
  import { FIT_WIDTH_CONTEXT } from '../layout';
  import ToggleControl from './ToggleControl.svelte';
  import TextboxControl from './TextboxControl.svelte';
  import SliderControl from './SliderControl.svelte';
  import NumberInputControl from './NumberInputControl.svelte';
  import DropdownControl from './DropdownControl.svelte';
  import ColorPickerControl from './ColorPickerControl.svelte';
  import DurationInputControl from './DurationInputControl.svelte';
  import FilepathControl from './FilepathControl.svelte';
  import PillInputControl from './PillInputControl.svelte';
  import DynamicTextboxesControl from './DynamicTextboxesControl.svelte';
  import RepeatableRowsControl from './RepeatableRowsControl.svelte';
  import ButtonControl from './ButtonControl.svelte';
  import DescriptionControl from './DescriptionControl.svelte';
  import TitleControl from './TitleControl.svelte';
  import SeparatorControl from './SeparatorControl.svelte';
  import UpdateNoticeControl from './UpdateNoticeControl.svelte';
  import ConnectionStatusControl from './ConnectionStatusControl.svelte';
  import GroupControl from './GroupControl.svelte';

  interface Props {
    node: SchemaNode;
  }

  let { node }: Props = $props();

  const noFieldShellTypes = new Set([
    'group',
    'title',
    'separator',
    'update-notice',
    'description',
  ]);

  setContext(FIT_WIDTH_CONTEXT, {
    get style() {
      const layout = node.layout;
      if (!layout || layout.width !== 'fit-content') return undefined;
      if (noFieldShellTypes.has(node.type)) return undefined;

      const parts = ['width: fit-content'];
      if (layout.minWidth) parts.push(`min-width: ${layout.minWidth}`);
      if (layout.maxWidth) parts.push(`max-width: ${layout.maxWidth}`);
      return parts.join('; ');
    },
  });

  // Read values so visibility re-evaluates reactively when settings change.
  let visible = $derived(
    node
      ? isVisible(node.visibility, appStore.values, undefined, appStore.document?.sections)
      : false,
  );

  let layoutStyle = $derived.by(() => {
    const layout = node.layout;
    if (!layout) return '';

    const parts: string[] = [];
    const fitOnFieldShell =
      layout.width === 'fit-content' && !noFieldShellTypes.has(node.type);

    if (layout.span != null) {
      parts.push(`grid-column: span ${layout.span}`);
    }

    // fit-content (+ its min/max) on field-shelled controls goes to FieldShell input row.
    if (layout.width && !fitOnFieldShell) {
      parts.push(`width: ${layout.width}`);
    }

    if (layout.minWidth && !fitOnFieldShell) {
      parts.push(`min-width: ${layout.minWidth}`);
    }

    if (layout.maxWidth && !fitOnFieldShell) {
      parts.push(`max-width: ${layout.maxWidth}`);
    }

    if (layout.grow != null) {
      parts.push(`flex-grow: ${layout.grow ? 1 : 0}`);
    }

    if (layout.shrink != null) {
      parts.push(`flex-shrink: ${layout.shrink ? 1 : 0}`);
    }

    return parts.join('; ');
  });

  $effect(() => {
    if (
      import.meta.env.DEV &&
      visible &&
      node &&
      ![
        'toggle',
        'textbox',
        'slider',
        'number-input',
        'dropdown',
        'color-picker',
        'duration-input',
        'filepath',
        'pill-input',
        'dynamic-textboxes',
        'repeatable-rows',
        'button',
        'description',
        'title',
        'separator',
        'update-notice',
        'connection-status',
        'group',
      ].includes(node.type)
    ) {
      console.warn('[FluentConfig] unrecognized schema node type:', node.type, node);
    }
  });
</script>

{#snippet control()}
  {#if node.type === 'toggle'}
    <ToggleControl {node} />
  {:else if node.type === 'textbox'}
    <TextboxControl {node} />
  {:else if node.type === 'slider'}
    <SliderControl {node} />
  {:else if node.type === 'number-input'}
    <NumberInputControl {node} />
  {:else if node.type === 'dropdown'}
    <DropdownControl {node} />
  {:else if node.type === 'color-picker'}
    <ColorPickerControl {node} />
  {:else if node.type === 'duration-input'}
    <DurationInputControl {node} />
  {:else if node.type === 'filepath'}
    <FilepathControl {node} />
  {:else if node.type === 'pill-input'}
    <PillInputControl {node} />
  {:else if node.type === 'dynamic-textboxes'}
    <DynamicTextboxesControl {node} />
  {:else if node.type === 'repeatable-rows'}
    <RepeatableRowsControl {node} />
  {:else if node.type === 'button'}
    <ButtonControl {node} />
  {:else if node.type === 'description'}
    <DescriptionControl {node} />
  {:else if node.type === 'title'}
    <TitleControl {node} />
  {:else if node.type === 'separator'}
    <SeparatorControl {node} />
  {:else if node.type === 'update-notice'}
    <UpdateNoticeControl {node} />
  {:else if node.type === 'connection-status'}
    <ConnectionStatusControl {node} />
  {:else if node.type === 'group'}
    <GroupControl {node} />
  {:else}
    {@const unknownType = (node as { type: string }).type}
    <div
      class="my-2 rounded-fc border border-dashed border-fc-warning/50 bg-fc-warning/10 px-3 py-2 text-sm text-fc-warning"
      role="status"
    >
      Unsupported control type: {unknownType}
    </div>
  {/if}
{/snippet}

{#if visible && node}
  {#if node.layout}
    <div style={layoutStyle}>
      {@render control()}
    </div>
  {:else}
    {@render control()}
  {/if}
{/if}
