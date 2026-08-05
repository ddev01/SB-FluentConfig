<script lang="ts">
  import { onMount } from 'svelte';

  interface Node {
    x: number;
    y: number;
    vx: number;
    vy: number;
    r: number;
  }

  /**
   * Defer canvas work until after first paint / idle so cold-open stays responsive.
   * Respects prefers-reduced-motion (fewer/slower nodes — still draws, never freezes).
   */
  let armed = $state(false);

  onMount(() => {
    let cancelled = false;
    let idleId = 0;
    let timeoutId = 0;
    let raf1 = 0;
    let raf2 = 0;

    const arm = (): void => {
      if (!cancelled) armed = true;
    };

    // Wait two frames so the shell can paint before the canvas starts.
    raf1 = requestAnimationFrame(() => {
      raf2 = requestAnimationFrame(() => {
        const ric = window.requestIdleCallback;
        if (typeof ric === 'function') {
          idleId = ric.call(window, arm, { timeout: 1200 });
        } else {
          timeoutId = window.setTimeout(arm, 0);
        }
      });
    });

    return () => {
      cancelled = true;
      cancelAnimationFrame(raf1);
      cancelAnimationFrame(raf2);
      if (idleId && typeof window.cancelIdleCallback === 'function') {
        window.cancelIdleCallback(idleId);
      }
      if (timeoutId) window.clearTimeout(timeoutId);
    };
  });

  /**
   * Particle network canvas (from fngg-scraper bg-network.js).
   * Must stay at z-index >= 0 inside #app — negative z-index children of a
   * stacking context are painted behind that context and become invisible.
   */
  function network(surface: HTMLCanvasElement) {
    const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const ctx = surface.getContext('2d');
    if (!ctx) return {};
    const context = ctx;

    const accentRgb =
      getComputedStyle(document.documentElement).getPropertyValue('--fc-network-rgb').trim() ||
      '0, 255, 163';

    // Always animate unless reduced-motion — use slower speed, not a freeze.
    const NODE_COUNT = reduced ? 28 : 56;
    const LINK_DIST = 160;
    const SPEED = reduced ? 0.12 : 0.45;
    const nodes: Node[] = [];
    let w = 0;
    let h = 0;
    let raf = 0;
    let running = true;
    let resizeTimer = 0;

    function resize(): void {
      const dpr = Math.min(window.devicePixelRatio || 1, 2);
      w = window.innerWidth;
      h = window.innerHeight;
      surface.width = Math.floor(w * dpr);
      surface.height = Math.floor(h * dpr);
      surface.style.width = `${w}px`;
      surface.style.height = `${h}px`;
      context.setTransform(dpr, 0, 0, dpr, 0, 0);
    }

    function initNodes(): void {
      nodes.length = 0;
      for (let i = 0; i < NODE_COUNT; i++) {
        nodes.push({
          x: Math.random() * w,
          y: Math.random() * h,
          vx: (Math.random() - 0.5) * SPEED,
          vy: (Math.random() - 0.5) * SPEED,
          r: Math.random() * 1.8 + 1.1,
        });
      }
    }

    function clampNodes(): void {
      for (const n of nodes) {
        n.x = Math.max(0, Math.min(w, n.x));
        n.y = Math.max(0, Math.min(h, n.y));
      }
    }

    function tick(): void {
      if (!running) return;
      context.clearRect(0, 0, w, h);

      for (const n of nodes) {
        n.x += n.vx;
        n.y += n.vy;
        if (n.x < 0 || n.x > w) n.vx *= -1;
        if (n.y < 0 || n.y > h) n.vy *= -1;
        n.x = Math.max(0, Math.min(w, n.x));
        n.y = Math.max(0, Math.min(h, n.y));
      }

      for (let i = 0; i < nodes.length; i++) {
        for (let j = i + 1; j < nodes.length; j++) {
          const a = nodes[i]!;
          const b = nodes[j]!;
          const dx = a.x - b.x;
          const dy = a.y - b.y;
          const dist = Math.hypot(dx, dy);
          if (dist > LINK_DIST) continue;
          const alpha = (1 - dist / LINK_DIST) * 0.38;
          context.strokeStyle = `rgba(${accentRgb}, ${alpha})`;
          context.lineWidth = 0.9;
          context.beginPath();
          context.moveTo(a.x, a.y);
          context.lineTo(b.x, b.y);
          context.stroke();
        }
      }

      for (const n of nodes) {
        context.fillStyle = `rgba(${accentRgb}, 0.85)`;
        context.beginPath();
        context.arc(n.x, n.y, n.r, 0, Math.PI * 2);
        context.fill();
        context.fillStyle = `rgba(${accentRgb}, 0.2)`;
        context.beginPath();
        context.arc(n.x, n.y, n.r * 3.2, 0, Math.PI * 2);
        context.fill();
      }

      raf = requestAnimationFrame(tick);
    }

    const onResize = (): void => {
      w = window.innerWidth;
      h = window.innerHeight;
      clampNodes();

      if (resizeTimer) window.clearTimeout(resizeTimer);
      resizeTimer = window.setTimeout(() => {
        resizeTimer = 0;
        resize();
        clampNodes();
      }, 120);
    };

    resize();
    initNodes();
    raf = requestAnimationFrame(tick);

    window.addEventListener('resize', onResize);

    const onVisibility = (): void => {
      if (document.hidden) {
        running = false;
        cancelAnimationFrame(raf);
      } else {
        running = true;
        raf = requestAnimationFrame(tick);
      }
    };
    document.addEventListener('visibilitychange', onVisibility);

    return {
      destroy() {
        running = false;
        cancelAnimationFrame(raf);
        if (resizeTimer) window.clearTimeout(resizeTimer);
        window.removeEventListener('resize', onResize);
        document.removeEventListener('visibilitychange', onVisibility);
      },
    };
  }
</script>

{#if armed}
  <canvas use:network class="bg-network" aria-hidden="true"></canvas>
{/if}
