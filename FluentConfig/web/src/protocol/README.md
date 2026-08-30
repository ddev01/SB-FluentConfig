# Protocol types (TypeScript)

Mirror of [`FluentConfig/Host/Protocol/`](../../Host/Protocol/).

These interfaces are the web half of the shared host↔UI contract. Field names,
casing (camelCase), and optionality must match the C# DTOs' JSON-serialized form.

**Keep in sync manually.** Generating both sides from one schema is a
reasonable future improvement; it is not in this tree. A snapshot contract test
(`ProtocolContractTests`) is the cheap guard against drift.

See [PROTOCOL.md](../../PROTOCOL.md) for message flow and examples.
