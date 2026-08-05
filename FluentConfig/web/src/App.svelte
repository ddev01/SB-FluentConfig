<script lang="ts">
  import { onMount, tick } from 'svelte';
  import { getBridge, isMockBridge } from './bridge';
  import { RpcClient } from './rpc/client';
  import { appStore } from './store/app.svelte';
  import FormSection from './lib/FormSection.svelte';
  import ProgressOverlay from './lib/ProgressOverlay.svelte';
  import ConfirmDialog from './lib/dialogs/ConfirmDialog.svelte';
  import PopupDialog from './lib/dialogs/PopupDialog.svelte';
  import DiscardChangesDialog from './lib/dialogs/DiscardChangesDialog.svelte';
  import FooterBrand from './lib/FooterBrand.svelte';
  import { applyColorScheme } from './lib/theme';
  import { fadeIn, slideY } from './lib/motion';
  import NetworkBackground from './lib/NetworkBackground.svelte';
  import { mark } from './lib/perf';

  // Dark default before bootstrap arrives (WebView2 / mock both send colorScheme).
  applyColorScheme('dark');

  let rpc: RpcClient | null = null;
  let tablistEl = $state<HTMLElement | null>(null);

  onMount(() => {
    mark('svelte-mount');
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
  let dirty = $derived(appStore.isDirty);

  function onGlobalKeydown(e: KeyboardEvent): void {
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 's') {
      e.preventDefault();
      if (!appStore.saving) void appStore.save();
    }
  }

  function selectSection(id: string): void {
    appStore.activeSectionId = id;
    void tick().then(() => {
      tablistEl
        ?.querySelector<HTMLElement>(`[data-section-id="${CSS.escape(id)}"]`)
        ?.scrollIntoView({ inline: 'nearest', block: 'nearest', behavior: 'smooth' });
    });
  }

  function onTabKeydown(e: KeyboardEvent): void {
    if (sections.length < 2) return;
    const keys = ['ArrowLeft', 'ArrowRight', 'Home', 'End'];
    if (!keys.includes(e.key)) return;
    e.preventDefault();
    const idx = sections.findIndex((s) => s.id === activeId);
    const current = idx >= 0 ? idx : 0;
    let next = current;
    if (e.key === 'ArrowLeft') next = (current - 1 + sections.length) % sections.length;
    else if (e.key === 'ArrowRight') next = (current + 1) % sections.length;
    else if (e.key === 'Home') next = 0;
    else if (e.key === 'End') next = sections.length - 1;
    const section = sections[next];
    if (!section) return;
    selectSection(section.id);
    void tick().then(() => {
      const btn = tablistEl?.querySelector<HTMLElement>(
        `[data-section-id="${CSS.escape(section.id)}"]`,
      );
      btn?.focus();
    });
  }
</script>

<svelte:window onkeydown={onGlobalKeydown} />

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
              {#if dirty}
                <span class="text-xs text-fc-warning">Unsaved changes</span>
              {:else if appStore.saveMessage}
                <span class="text-xs text-fc-success">{appStore.saveMessage}</span>
              {/if}
              <button
                type="button"
                class="fc-btn"
                onclick={() => void appStore.exit()}
              >
                Exit
              </button>
              <button
                type="button"
                class="fc-btn-accent relative"
                disabled={appStore.saving}
                onclick={() => void appStore.save()}
              >
                {#if dirty}
                  <span
                    class="absolute -top-0.5 -right-0.5 size-2 rounded-full bg-fc-warning"
                    aria-hidden="true"
                  ></span>
                {/if}
                {appStore.saving ? 'Saving…' : 'Save'}
              </button>
            </div>
          </div>
        </header>

        {#if sections.length > 1}
          <div class="fc-tabstrip relative">
            <div
              bind:this={tablistEl}
              class="fc-glass fc-tablist flex gap-1 overflow-x-auto overflow-y-hidden rounded-2xl border border-fc-border/60 p-1.5"
              role="tablist"
              aria-label="Sections"
              tabindex="-1"
              onkeydown={onTabKeydown}
            >
              {#each sections as section (section.id)}
                <button
                  type="button"
                  role="tab"
                  id="fc-tab-{section.id}"
                  data-section-id={section.id}
                  aria-selected={activeId === section.id}
                  aria-controls="fc-panel-{section.id}"
                  tabindex={activeId === section.id ? 0 : -1}
                  class="shrink-0 rounded-xl px-3 py-2 text-sm font-medium whitespace-nowrap transition-colors duration-150 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-fc-ring/50
                    {activeId === section.id
                    ? 'bg-fc-accent text-fc-bg'
                    : 'text-fc-text-muted hover:bg-fc-elevated/70 hover:text-fc-text'}"
                  onclick={() => selectSection(section.id)}
                >
                  {section.title}
                </button>
              {/each}
            </div>
          </div>
        {/if}
      </div>

      <main class="flex-1 px-4 py-4">
        {#key activeSection.id}
          <div
            in:fadeIn={{ duration: 0.18 }}
            out:fadeIn={{ duration: 0.12 }}
            role="tabpanel"
            id="fc-panel-{activeSection.id}"
            aria-labelledby="fc-tab-{activeSection.id}"
            tabindex="0"
          >
            <FormSection section={activeSection} />
          </div>
        {/key}
      </main>

      <FooterBrand
        frameworkVersion={doc.frameworkVersion}
        repoUrl={doc.repoUrl}
      />
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

  {#if appStore.discardDialog}
    {@const d = appStore.discardDialog}
    <DiscardChangesDialog
      entries={d.entries}
      dontRemindInitial={d.dontRemindInitial}
      saving={appStore.saving}
      onkeep={(dontRemindAgain) =>
        appStore.resolveDiscard({ allowClose: false, dontRemindAgain })}
      onsaveexit={async (dontRemindAgain) => {
        const ok = await appStore.save();
        if (ok) {
          appStore.resolveDiscard({ allowClose: true, dontRemindAgain });
        } else {
          appStore.pushToast(appStore.saveMessage ?? 'Save failed');
        }
      }}
      ondiscard={(dontRemindAgain) =>
        appStore.resolveDiscard({ allowClose: true, dontRemindAgain })}
    />
  {/if}
</div>
