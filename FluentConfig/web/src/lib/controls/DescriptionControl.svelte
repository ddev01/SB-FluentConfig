<script lang="ts">
  import type { DescriptionNode } from '../../protocol';
  import { renderRichText } from '../richText';
  import { appStore } from '../../store/app.svelte';

  interface Props {
    node: DescriptionNode;
  }

  let { node }: Props = $props();

  const html = $derived(renderRichText(node.text));

  function onClick(e: MouseEvent): void {
    const target = e.target;
    if (!(target instanceof Element)) return;
    const anchor = target.closest('a[data-fc-url]');
    if (!(anchor instanceof HTMLAnchorElement)) return;
    e.preventDefault();
    const url = anchor.dataset.fcUrl;
    if (url) appStore.openUrl(url);
  }
</script>

<!-- svelte-ignore a11y_click_events_have_key_events a11y_no_static_element_interactions -->
<!-- Links use data-fc-url + click handler; content is escaped by renderRichText. -->
<div
  class="fc-rich-text py-1 text-sm leading-relaxed whitespace-pre-wrap text-fc-text-muted"
  role="presentation"
  onclick={onClick}
>
  {@html html}
</div>

<style>
  :global(.fc-rich-text .fc-rich-h1) {
    margin: 0.5rem 0 0.25rem;
    font-size: 1.125rem;
    font-weight: 600;
    color: var(--color-fc-text);
  }
  :global(.fc-rich-text .fc-rich-h2) {
    margin: 0.4rem 0 0.2rem;
    font-size: 1rem;
    font-weight: 600;
    color: var(--color-fc-text);
  }
  :global(.fc-rich-text .fc-rich-h3) {
    margin: 0.35rem 0 0.15rem;
    font-size: 0.9375rem;
    font-weight: 600;
    color: var(--color-fc-text);
  }
  :global(.fc-rich-text .fc-rich-p) {
    margin: 0.15rem 0;
    white-space: pre-wrap;
  }
  :global(.fc-rich-text .fc-rich-ul),
  :global(.fc-rich-text .fc-rich-ol) {
    margin: 0.25rem 0;
    padding-left: 1.25rem;
  }
  :global(.fc-rich-text .fc-rich-ul) {
    list-style: disc;
  }
  :global(.fc-rich-text .fc-rich-ol) {
    list-style: decimal;
  }
  :global(.fc-rich-text .fc-rich-pre) {
    margin: 0.35rem 0;
    overflow-x: auto;
    border-radius: 0.375rem;
    border: 1px solid color-mix(in srgb, var(--color-fc-border) 80%, transparent);
    background: color-mix(in srgb, var(--color-fc-bg) 70%, transparent);
    padding: 0.5rem 0.75rem;
    font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
    font-size: 0.8125rem;
    color: var(--color-fc-text);
    white-space: pre;
  }
  :global(.fc-rich-text .fc-rich-code) {
    border-radius: 0.25rem;
    background: color-mix(in srgb, var(--color-fc-elevated) 80%, transparent);
    padding: 0.05rem 0.3rem;
    font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
    font-size: 0.8125rem;
  }
  :global(.fc-rich-text .fc-rich-link) {
    color: var(--color-fc-accent);
    text-decoration: underline;
    text-underline-offset: 2px;
    cursor: pointer;
  }
  :global(.fc-rich-text .fc-rich-link:hover) {
    filter: brightness(1.1);
  }
</style>
