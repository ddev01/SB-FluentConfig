# Protocol types (TypeScript)

Mirror of [`FluentConfig/Host/Protocol/`](../../Host/Protocol/).

These interfaces are the web half of the shared host↔UI contract. Field names,
casing (camelCase), and optionality must match the C# DTOs' JSON-serialized form.

**Keep in sync manually for now.** Generating both sides from one schema is a
reasonable future improvement — not in scope for Phase 0.

See [PROTOCOL.md](../../PROTOCOL.md) for message flow and examples.
