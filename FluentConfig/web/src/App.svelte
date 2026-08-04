<script lang="ts">
  import { onMount } from 'svelte';
  import { getBridge, isMockBridge } from './bridge';
  import { RpcClient } from './rpc/client';
  import { appStore } from './store/app.svelte';
  import FormSection from './lib/FormSection.svelte';
  import ProgressOverlay from './lib/ProgressOverlay.svelte';
  import { applyColorScheme } from './lib/theme';

  // Dark default before bootstrap arrives (WebView2 / mock both send colorScheme).
  applyColorScheme('dark');

  let rpc: RpcClient | null = null;

  onMount(() => {
    const bridge = getBridge();
    rpc = new RpcClient(bridge);
    appStore.bind(rpc, isMockBridge());
    return () => {
      appStore.dispose();
      rpc?.dispose();
    };
  });

  let doc = $derived(appStore.document);
  let sections = $derived(doc?.sections ?? []);
  let activeId = $derived(appStore.activeSectionId);
  let activeSection = $derived(
    sections.find((s) => s.id === activeId) ?? sections[0] ?? null,
  );
</script>

{#if !appStore.ready || !doc || !activeSection}
  <div class="flex min-h-svh items-center justify-center text-sm text-zinc-500">
    Loading…
  </div>
{:else}
  <div class="mx-auto flex min-h-svh max-w-3xl flex-col">
    <header
      class="sticky top-0 z-10 border-b border-zinc-200 bg-zinc-50/95 px-4 py-3 backdrop-blur dark:border-zinc-800 dark:bg-zinc-950/95"
    >
      <div class="flex flex-wrap items-center justify-between gap-3">
        <div class="min-w-0">
          <h1 class="truncate text-lg font-semibold text-zinc-900 dark:text-zinc-50">
            {doc.title}
          </h1>
          <p class="text-xs text-zinc-500">
            v{doc.version}
            {#if appStore.usingMock}
              <span
                class="ml-2 rounded bg-amber-500/15 px-1.5 py-0.5 font-medium text-amber-700 dark:text-amber-300"
                >mock bridge</span
              >
            {/if}
          </p>
        </div>
        <div class="flex items-center gap-2">
          {#if appStore.saveMessage}
            <span class="text-xs text-emerald-600 dark:text-emerald-400"
              >{appStore.saveMessage}</span
            >
          {/if}
          <button
            type="button"
            class="rounded-md bg-sky-600 px-4 py-2 text-sm font-medium text-white hover:bg-sky-500 disabled:opacity-50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-400"
            disabled={appStore.saving}
            onclick={() => void appStore.save()}
          >
            {appStore.saving ? 'Saving…' : 'Save'}
          </button>
        </div>
      </div>

      {#if sections.length > 1}
        <nav class="mt-3 flex gap-1 overflow-x-auto" aria-label="Sections">
          {#each sections as section (section.id)}
            <button
              type="button"
              class="rounded-md px-3 py-1.5 text-sm whitespace-nowrap transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-500
                {activeId === section.id
                ? 'bg-zinc-200 font-medium text-zinc-900 dark:bg-zinc-800 dark:text-zinc-50'
                : 'text-zinc-600 hover:bg-zinc-100 dark:text-zinc-400 dark:hover:bg-zinc-900'}"
              onclick={() => (appStore.activeSectionId = section.id)}
            >
              {section.title}
            </button>
          {/each}
        </nav>
      {/if}
    </header>

    <main class="flex-1 px-4 py-4">
      <FormSection section={activeSection} />
    </main>
  </div>
{/if}

<ProgressOverlay />

{#if appStore.toasts.length > 0}
  <div class="pointer-events-none fixed right-4 bottom-4 z-50 flex flex-col gap-2">
    {#each appStore.toasts as toast (toast.id)}
      <div
        class="pointer-events-auto rounded-md border border-zinc-700 bg-zinc-900 px-4 py-2 text-sm text-zinc-100 shadow-lg"
        role="status"
      >
        {toast.message}
      </div>
    {/each}
  </div>
{/if}
