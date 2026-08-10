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
  let scrollEl = $state<HTMLElement | null>(null);
  let chromeEl = $state<HTMLElement | null>(null);
  let chromeH = $state(0);
  let tabEls = $state<Record<string, HTMLButtonElement | undefined>>({});
  let underlineLeft = $state(0);
  let underlineWidth = $state(0);
  let canScrollLeft = $state(false);
  let canScrollRight = $state(false);

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
    if (scrollEl) scrollEl.scrollTop = 0;
    void tick().then(() => {
      tablistEl
        ?.querySelector<HTMLElement>(`[data-section-id="${CSS.escape(id)}"]`)
        ?.scrollIntoView({ inline: 'nearest', block: 'nearest', behavior: 'smooth' });
    });
  }

  function updateScrollFades(): void {
    const el = tablistEl;
    if (!el) {
      canScrollLeft = false;
      canScrollRight = false;
      return;
    }
    const { scrollLeft, scrollWidth, clientWidth } = el;
    canScrollLeft = scrollLeft > 1;
    canScrollRight = scrollLeft + clientWidth < scrollWidth - 1;
  }

  function updateUnderline(): void {
    const id = activeId;
    const btn = id ? tabEls[id] : undefined;
    if (!btn || !tablistEl) {
      underlineWidth = 0;
      return;
    }
    underlineLeft = btn.offsetLeft;
    underlineWidth = btn.offsetWidth;
  }

  $effect(() => {
    activeId;
    sections;
    tabEls;
    const raf = requestAnimationFrame(() => {
      updateUnderline();
      updateScrollFades();
    });
    return () => cancelAnimationFrame(raf);
  });

  $effect(() => {
    const el = tablistEl;
    if (!el || typeof ResizeObserver === 'undefined') return;
    const ro = new ResizeObserver(() => {
      updateUnderline();
      updateScrollFades();
    });
    ro.observe(el);
    return () => ro.disconnect();
  });

  $effect(() => {
    const el = chromeEl;
    if (!el || typeof ResizeObserver === 'undefined') return;
    const sync = () => {
      chromeH = Math.ceil(el.getBoundingClientRect().height);
    };
    const ro = new ResizeObserver(sync);
    ro.observe(el);
    sync();
    return () => ro.disconnect();
  });

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

  /** Forward vertical wheel from chrome so page scroll works without hovering main. */
  function onWindowWheel(e: WheelEvent): void {
    if (!scrollEl) return;
    if (Math.abs(e.deltaX) > Math.abs(e.deltaY)) return;
    const target = e.target;
    if (!(target instanceof Node)) return;
    if (scrollEl.contains(target)) return;
    if (target instanceof Element && target.closest('[role="dialog"], [aria-modal="true"]')) {
      return;
    }
    if (target instanceof Element && target.closest('.fc-tablist')) {
      const tabs = tablistEl;
      if (tabs && tabs.scrollWidth > tabs.clientWidth) return;
    }
    scrollEl.scrollTop += e.deltaY;
  }
</script>

<svelte:window onkeydown={onGlobalKeydown} onwheel={onWindowWheel} />

<NetworkBackground />

<div class="relative z-1 h-svh overflow-hidden">
  {#if !appStore.ready || !doc || !activeSection}
    <div class="flex h-full items-center justify-center text-sm text-fc-text-muted">
      Loading…
    </div>
  {:else}
    <div
      bind:this={scrollEl}
      class="fc-main-scroll absolute inset-0 overflow-x-hidden overflow-y-auto"
      style="--fc-chrome-h: {chromeH}px"
    >
      <div
        class="mx-auto flex min-h-full max-w-3xl flex-col"
        style="padding-top: var(--fc-chrome-h)"
      >
        <main class="flex-1 px-4 py-4">
          {#key activeSection.id}
            <div
              in:fadeIn={{ duration: 0.18 }}
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
    </div>

    <div bind:this={chromeEl} class="pointer-events-none absolute inset-x-0 top-0 z-10">
      <div class="pointer-events-auto mx-auto w-full max-w-3xl px-4 pt-3">
        <div class="flex flex-col gap-2">
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
              {#if canScrollLeft}
                <div
                  class="pointer-events-none absolute inset-y-0 left-0 z-1 w-8 bg-gradient-to-r from-fc-bg to-transparent"
                  aria-hidden="true"
                ></div>
              {/if}
              {#if canScrollRight}
                <div
                  class="pointer-events-none absolute inset-y-0 right-0 z-1 w-8 bg-gradient-to-l from-fc-bg to-transparent"
                  aria-hidden="true"
                ></div>
              {/if}

              <div
                bind:this={tablistEl}
                class="fc-tablist relative flex gap-6 overflow-x-auto overflow-y-hidden border-b border-fc-border/60"
                role="tablist"
                aria-label="Sections"
                tabindex="-1"
                onkeydown={onTabKeydown}
                onscroll={updateScrollFades}
              >
                {#each sections as section (section.id)}
                  <button
                    bind:this={tabEls[section.id]}
                    type="button"
                    role="tab"
                    id="fc-tab-{section.id}"
                    data-section-id={section.id}
                    aria-selected={activeId === section.id}
                    aria-controls="fc-panel-{section.id}"
                    tabindex={activeId === section.id ? 0 : -1}
                    class="relative shrink-0 px-1 py-2.5 text-sm whitespace-nowrap transition-colors duration-150 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-fc-ring/50
                      {activeId === section.id
                      ? 'font-semibold text-fc-accent'
                      : 'font-medium text-fc-text-muted hover:text-fc-text'}"
                    onclick={() => selectSection(section.id)}
                  >
                    {section.title}
                  </button>
                {/each}

                <span
                  class="pointer-events-none absolute bottom-0 h-0.5 rounded-full bg-fc-accent transition-[left,width] duration-200 ease-out"
                  style="left: {underlineLeft}px; width: {underlineWidth}px;"
                  aria-hidden="true"
                ></span>
              </div>
            </div>
          {/if}
        </div>
      </div>
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
