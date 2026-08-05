/**
 * Static + runtime smoke: mock document covers every SchemaNodeType,
 * and SchemaNodeView dispatches each type.
 */
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { createMockDocument } from '../src/bridge/mockDocument.ts';
import type { SchemaNode, SchemaNodeType } from '../src/protocol/schema.ts';

const ALL_TYPES: SchemaNodeType[] = [
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
];

function collectTypes(nodes: SchemaNode[], into: Set<string>): void {
  for (const n of nodes) {
    into.add(n.type);
    if (n.type === 'group') collectTypes(n.children, into);
    if (n.type === 'pill-input') {
      if (n.itemTemplate) collectTypes(n.itemTemplate, into);
      if (n.items) for (const item of n.items) collectTypes(item.children, into);
    }
    if (n.type === 'repeatable-rows') collectTypes(n.rowSchema, into);
  }
}

const doc = createMockDocument();
const found = new Set<string>();
for (const s of doc.sections) collectTypes(s.children, found);

const missing = ALL_TYPES.filter((t) => !found.has(t));
if (missing.length) {
  console.error('Mock document missing types:', missing);
  process.exit(1);
}

const viewSrc = readFileSync(
  resolve(import.meta.dir, '../src/lib/controls/SchemaNodeView.svelte'),
  'utf8',
);
const undispatched = ALL_TYPES.filter((t) => !viewSrc.includes(`'${t}'`));
if (undispatched.length) {
  console.error('SchemaNodeView missing dispatch for:', undispatched);
  process.exit(1);
}

console.log('OK — mock document + SchemaNodeView cover all', ALL_TYPES.length, 'types:');
console.log([...found].sort().join(', '));
console.log('Sections:', doc.sections.map((s) => s.id).join(', '));
console.log('colorScheme:', doc.colorScheme);
