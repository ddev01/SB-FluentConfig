<script lang="ts">
  import type { Snippet } from 'svelte';

  interface Props {
    checked?: boolean;
    disabled?: boolean;
    id?: string;
    onchange?: (checked: boolean) => void;
    children?: Snippet;
  }

  let {
    checked = $bindable(false),
    disabled = false,
    id,
    onchange,
    children,
  }: Props = $props();
</script>

<label
  class="inline-flex cursor-pointer items-center gap-2 text-sm text-fc-text-muted select-none
    {disabled
      ? 'cursor-not-allowed opacity-50'
      : 'transition-colors duration-150 hover:text-fc-text'}"
>
  <span class="relative inline-flex size-[1.125rem] shrink-0">
    <input
      {id}
      type="checkbox"
      class="peer absolute inset-0 z-10 m-0 size-full cursor-pointer opacity-0 disabled:cursor-not-allowed"
      bind:checked
      {disabled}
      onchange={(e) => onchange?.((e.currentTarget as HTMLInputElement).checked)}
    />
    <span
      class="pointer-events-none flex size-full items-center justify-center rounded-[0.3125rem] border border-fc-border-strong bg-fc-surface transition-[background-color,border-color,box-shadow,color] duration-150
        peer-hover:border-fc-text-subtle
        peer-checked:border-fc-accent peer-checked:bg-fc-accent peer-checked:text-fc-bg peer-checked:shadow-fc-glow
        peer-checked:[&_svg]:scale-100 peer-checked:[&_svg]:opacity-100
        peer-focus-visible:ring-2 peer-focus-visible:ring-fc-ring/50 peer-focus-visible:ring-offset-2 peer-focus-visible:ring-offset-fc-bg"
      aria-hidden="true"
    >
      <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 16 16"
        fill="none"
        class="size-3 origin-center scale-75 opacity-0 transition-[opacity,transform] duration-150"
      >
        <path
          d="M3.25 8.25 6.5 11.5 12.75 4.5"
          stroke="currentColor"
          stroke-width="2"
          stroke-linecap="round"
          stroke-linejoin="round"
        />
      </svg>
    </span>
  </span>
  {@render children?.()}
</label>
