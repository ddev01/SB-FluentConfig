import { describe, expect, it } from 'vitest';
import type { SchemaNode } from '../protocol';
import { patchDropdownOptionsInTree, patchPillItemsInTree } from './schemaWalk';

function pill(saveKey: string, items: { name: string; children: SchemaNode[] }[] = []): SchemaNode {
  return { type: 'pill-input', label: 'Pills', saveKey, items };
}

function dropdown(saveKey: string): SchemaNode {
  return { type: 'dropdown', label: 'Pick', saveKey, options: [] };
}

function group(children: SchemaNode[]): SchemaNode {
  return { type: 'group', children };
}

describe('patchPillItemsInTree', () => {
  const items = [{ name: 'alpha', children: [] as SchemaNode[] }];

  it('patches a top-level pill', () => {
    const tree = [pill('pills')];
    expect(patchPillItemsInTree(tree, 'pills', items)).toBe(true);
    expect(tree[0]!.type === 'pill-input' && tree[0].items).toEqual(items);
  });

  it('recurses through groups (Grid / WithVisibility)', () => {
    const tree = [group([pill('nested')])];
    expect(patchPillItemsInTree(tree, 'nested', items)).toBe(true);
    const child = tree[0]!.type === 'group' ? tree[0].children[0] : null;
    expect(child?.type === 'pill-input' && child.items).toEqual(items);
  });

  it('recurses through pill item children', () => {
    const tree = [
      pill('outer', [{ name: 'host', children: [pill('inner')] }]),
    ];
    expect(patchPillItemsInTree(tree, 'inner', items)).toBe(true);
    const outer = tree[0]!;
    const inner =
      outer.type === 'pill-input' ? outer.items?.[0]?.children[0] : undefined;
    expect(inner?.type === 'pill-input' && inner.items).toEqual(items);
  });

  it('recurses through repeatable-rows schemas', () => {
    const tree: SchemaNode[] = [
      { type: 'repeatable-rows', saveKey: 'rows', rowSchema: [pill('rowPill')] },
    ];
    expect(patchPillItemsInTree(tree, 'rowPill', items)).toBe(true);
    const row = tree[0]!.type === 'repeatable-rows' ? tree[0].rowSchema[0] : undefined;
    expect(row?.type === 'pill-input' && row.items).toEqual(items);
  });

  it('returns false when the saveKey is absent', () => {
    expect(patchPillItemsInTree([pill('pills')], 'missing', items)).toBe(false);
  });
});

describe('patchDropdownOptionsInTree', () => {
  const options = [{ value: 'a', display: 'A' }];

  it('patches a dropdown nested in a group', () => {
    const tree = [group([dropdown('pick')])];
    expect(patchDropdownOptionsInTree(tree, 'pick', options)).toBe(true);
    const child = tree[0]!.type === 'group' ? tree[0].children[0] : null;
    expect(child?.type === 'dropdown' && child.options).toEqual(options);
  });
});
