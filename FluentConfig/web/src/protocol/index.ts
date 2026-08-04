/**
 * FluentConfig host↔web message protocol (TypeScript side).
 *
 * This folder mirrors `FluentConfig/Host/Protocol/` and must stay in sync
 * manually for now. Codegen from a single schema is a reasonable future
 * improvement — do not build it as part of Phase 0.
 *
 * Wire format is camelCase JSON on both sides. See `FluentConfig/PROTOCOL.md`.
 */

export * from './schema';
export * from './messages';
export * from './rpc';
