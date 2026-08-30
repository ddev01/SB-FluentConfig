/**
 * Schema-tree walk shared by dropdown option patches and pill item patches.
 * Recurses groups, pill items / templates, and repeatable-row schemas.
 */
import type { DropdownOption, PillItemSchema, SchemaNode } from '../protocol';

/** Depth-first. `visitor` returning true stops the walk (found). */
export function visitSchemaNodes(
  nodes: SchemaNode[] | undefined,
  visitor: (node: SchemaNode) => boolean,
): boolean {
  if (!nodes) return false;
  for (const node of nodes) {
    if (visitor(node)) return true;
    if (node.type === 'group' && visitSchemaNodes(node.children, visitor)) return true;
    if (node.type === 'pill-input') {
      if (node.items) {
        for (const item of node.items) {
          if (visitSchemaNodes(item.children, visitor)) return true;
        }
      }
      if (node.itemTemplate && visitSchemaNodes(node.itemTemplate, visitor)) return true;
    }
    if (node.type === 'repeatable-rows' && visitSchemaNodes(node.rowSchema, visitor)) {
      return true;
    }
  }
  return false;
}

export function patchDropdownOptionsInTree(
  nodes: SchemaNode[],
  saveKey: string,
  options: DropdownOption[],
): boolean {
  return visitSchemaNodes(nodes, (node) => {
    if (node.type === 'dropdown' && node.saveKey === saveKey) {
      node.options = options;
      return true;
    }
    return false;
  });
}

export function patchPillItemsInTree(
  nodes: SchemaNode[],
  saveKey: string,
  items: PillItemSchema[],
): boolean {
  return visitSchemaNodes(nodes, (node) => {
    if (node.type === 'pill-input' && node.saveKey === saveKey) {
      node.items = items;
      return true;
    }
    return false;
  });
}
