<script lang="ts">
  import { onMount } from 'svelte';
  import { getBridge, isMockBridge } from './bridge';
  import { RpcClient } from './rpc/client';
  import { appStore } from './store/app.svelte';
  import FormSection from './lib/FormSection.svelte';
  import ProgressOverlay from './lib/ProgressOverlay.svelte';
  import ConfirmDialog from './lib/dialogs/ConfirmDialog.svelte';
  import PopupDialog from './lib/dialogs/PopupDialog.svelte';
  import { applyColorScheme } from './lib/theme';
  import { fadeIn, slideY } from './lib/motion';
  import NetworkBackground from './lib/NetworkBackground.svelte';

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

<NetworkBackground />

<div class="relative z-1 min-h-svh">
  {#if !appStore.ready || !doc || !activeSection}
    <div class="flex min-h-svh items-center justify-center text-sm text-fc-text-muted">
      Loading…
    </div>
  {:else}
    <div class="mx-auto flex min-h-svh max-w-3xl flex-col">
      <div class="sticky top-3 z-10 mt-3 flex flex-col gap-2 px-4">
        <header
          class="fc-glass rounded-2xl border border-fc-border/60 px-4 py-3 shadow-[0_12px_32px_-16px_rgba(0,0,0,0.55)]"
        >
          <div class="flex flex-wrap items-center justify-between gap-3">
            <div class="flex min-w-0 flex-col gap-0.5">
              <h1 class="truncate text-lg font-semibold tracking-tight text-fc-text">
                {doc.title}
              </h1>
              <p class="text-xs text-fc-text-subtle">
                v{doc.version}
                {#if appStore.usingMock}
                  <span
                    class="ml-2 rounded-fc bg-fc-warning/15 px-1.5 py-0.5 font-medium text-fc-warning"
                    >mock bridge</span
                  >
                {/if}
              </p>
            </div>
            <div class="flex items-center gap-2">
              {#if appStore.saveMessage}
                <span class="text-xs text-fc-success">{appStore.saveMessage}</span>
              {/if}
              <button
                type="button"
                class="fc-btn-accent"
                disabled={appStore.saving}
                onclick={() => void appStore.save()}
              >
                {appStore.saving ? 'Saving…' : 'Save'}
              </button>
            </div>
          </div>
        </header>

        {#if sections.length > 1}
          <nav
            class="fc-glass flex gap-1 overflow-x-auto overflow-y-hidden rounded-2xl border border-fc-border/60 p-1.5"
            aria-label="Sections"
          >
            {#each sections as section (section.id)}
              <button
                type="button"
                role="tab"
                aria-selected={activeId === section.id}
                class="min-w-0 flex-1 rounded-xl px-3 py-2 text-sm font-medium whitespace-nowrap transition-colors duration-150 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-fc-ring/50
                  {activeId === section.id
                  ? 'bg-fc-accent text-fc-bg'
                  : 'text-fc-text-muted hover:bg-fc-elevated/70 hover:text-fc-text'}"
                onclick={() => (appStore.activeSectionId = section.id)}
              >
                {section.title}
              </button>
            {/each}
          </nav>
        {/if}
      </div>

      <main class="flex-1 px-4 py-4">
        {#key activeSection.id}
          <div in:fadeIn={{ duration: 0.18 }} out:fadeIn={{ duration: 0.12 }}>
            <FormSection section={activeSection} />
          </div>
        {/key}
      </main>
    </div>
  {/if}

  <ProgressOverlay />

  {#if appStore.toasts.length > 0}
    <div class="pointer-events-none fixed right-4 bottom-4 z-50 flex flex-col gap-2">
      {#each appStore.toasts as toast (toast.id)}
        <div
          class="fc-card pointer-events-auto bg-fc-elevated/90 px-4 py-2 text-sm text-fc-text shadow-fc-glow"
          role="status"
          transition:slideY={{ duration: 0.22, y: 12 }}
        >
          {toast.message}
        </div>
      {/each}
    </div>
  {/if}

  {#if appStore.confirmDialog}
    {@const d = appStore.confirmDialog}
    <ConfirmDialog
      title={d.title}
      message={d.message}
      confirmText={d.confirmText}
      cancelText={d.cancelText}
      onconfirm={() => appStore.resolveConfirm(true)}
      oncancel={() => appStore.resolveConfirm(false)}
    />
  {/if}

  {#if appStore.popupDialog}
    {@const d = appStore.popupDialog}
    <PopupDialog
      title={d.title}
      message={d.message}
      onclose={() => appStore.resolvePopup()}
    />
  {/if}
</div>
