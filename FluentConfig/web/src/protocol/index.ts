/**
 * FluentConfig host↔web message protocol (TypeScript side).
 *
 * This folder mirrors `FluentConfig/Host/Protocol/` and must stay in sync
 * manually. Codegen from a single schema is a reasonable future
 * improvement; it is not in this tree.
 *
 * Wire format is camelCase JSON on both sides. See `FluentConfig/PROTOCOL.md`.
 */

export * from './schema';
export * from './messages';
export * from './rpc';
