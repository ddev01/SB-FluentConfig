/**
 * Hand-rolled rich-description parser.
 * Supports: **bold**, `code`, {#RRGGBB|text}, #/##/### headings,
 * - / 1. lists, [text](url) links, and fenced ``` code blocks.
 * No markdown library — output is escaped HTML safe for {@html}.
 */

function escapeHtml(text: string): string {
  return text
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;');
}

const INLINE_RE =
  /(\*\*(.+?)\*\*|`([^`]+)`|\{(#[0-9A-Fa-f]{6})\|([^}]*)\}|\[([^\]]+)\]\(([^)]+)\))/g;

/** Parse inline markup within a single line (already block-split). */
export function parseInline(text: string): string {
  let out = '';
  let last = 0;
  INLINE_RE.lastIndex = 0;
  let m: RegExpExecArray | null;
  while ((m = INLINE_RE.exec(text)) !== null) {
    out += escapeHtml(text.slice(last, m.index));
    if (m[2] !== undefined) {
      out += `<strong>${escapeHtml(m[2])}</strong>`;
    } else if (m[3] !== undefined) {
      out += `<code class="fc-rich-code">${escapeHtml(m[3])}</code>`;
    } else if (m[4] !== undefined && m[5] !== undefined) {
      const color = m[4];
      out += `<span style="color:${escapeHtml(color)}">${escapeHtml(m[5])}</span>`;
    } else if (m[6] !== undefined && m[7] !== undefined) {
      const label = escapeHtml(m[6]);
      const url = m[7].trim();
      const safeHref = /^(https?:\/\/|mailto:)/i.test(url) ? escapeHtml(url) : '#';
      out += `<a href="${safeHref}" class="fc-rich-link" data-fc-url="${escapeHtml(url)}">${label}</a>`;
    }
    last = m.index + m[0].length;
  }
  out += escapeHtml(text.slice(last));
  return out;
}

type Block =
  | { kind: 'heading'; level: 1 | 2 | 3; text: string }
  | { kind: 'ul'; items: string[] }
  | { kind: 'ol'; items: string[] }
  | { kind: 'code'; text: string }
  | { kind: 'p'; text: string };

function flushParagraph(lines: string[], blocks: Block[]): void {
  const text = lines.join('\n').trimEnd();
  if (text.length) blocks.push({ kind: 'p', text });
  lines.length = 0;
}

function parseBlocks(source: string): Block[] {
  const lines = source.replaceAll('\r\n', '\n').replaceAll('\r', '\n').split('\n');
  const blocks: Block[] = [];
  let para: string[] = [];
  let i = 0;

  while (i < lines.length) {
    const line = lines[i]!;

    if (line.startsWith('```')) {
      flushParagraph(para, blocks);
      i++;
      const codeLines: string[] = [];
      while (i < lines.length && !lines[i]!.startsWith('```')) {
        codeLines.push(lines[i]!);
        i++;
      }
      if (i < lines.length) i++; // closing fence
      blocks.push({ kind: 'code', text: codeLines.join('\n') });
      continue;
    }

    const heading = /^(#{1,3})\s+(.+)$/.exec(line);
    if (heading) {
      flushParagraph(para, blocks);
      const level = heading[1]!.length as 1 | 2 | 3;
      blocks.push({ kind: 'heading', level, text: heading[2]! });
      i++;
      continue;
    }

    const ul = /^[-*]\s+(.+)$/.exec(line);
    if (ul) {
      flushParagraph(para, blocks);
      const items: string[] = [];
      while (i < lines.length) {
        const item = /^[-*]\s+(.+)$/.exec(lines[i]!);
        if (!item) break;
        items.push(item[1]!);
        i++;
      }
      blocks.push({ kind: 'ul', items });
      continue;
    }

    const ol = /^\d+\.\s+(.+)$/.exec(line);
    if (ol) {
      flushParagraph(para, blocks);
      const items: string[] = [];
      while (i < lines.length) {
        const item = /^\d+\.\s+(.+)$/.exec(lines[i]!);
        if (!item) break;
        items.push(item[1]!);
        i++;
      }
      blocks.push({ kind: 'ol', items });
      continue;
    }

    if (line.trim() === '') {
      flushParagraph(para, blocks);
      i++;
      continue;
    }

    para.push(line);
    i++;
  }

  flushParagraph(para, blocks);
  return blocks;
}

/** Convert rich-description source to escaped HTML. */
export function renderRichText(source: string): string {
  if (!source) return '';
  const blocks = parseBlocks(source);
  const parts: string[] = [];

  for (const block of blocks) {
    switch (block.kind) {
      case 'heading': {
        const tag = `h${block.level}`;
        parts.push(
          `<${tag} class="fc-rich-h${block.level}">${parseInline(block.text)}</${tag}>`,
        );
        break;
      }
      case 'ul':
        parts.push(
          `<ul class="fc-rich-ul">${block.items.map((it) => `<li>${parseInline(it)}</li>`).join('')}</ul>`,
        );
        break;
      case 'ol':
        parts.push(
          `<ol class="fc-rich-ol">${block.items.map((it) => `<li>${parseInline(it)}</li>`).join('')}</ol>`,
        );
        break;
      case 'code':
        parts.push(
          `<pre class="fc-rich-pre"><code>${escapeHtml(block.text)}</code></pre>`,
        );
        break;
      case 'p':
        parts.push(`<p class="fc-rich-p">${parseInline(block.text)}</p>`);
        break;
    }
  }

  return parts.join('');
}
