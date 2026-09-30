# Architecture Decision Records

Short records of *why* things are the way they are. Format: context → decision → consequences.

| # | Decision |
|---|----------|
| [0001](0001-modular-monolith.md) | Modular monolith, one deployable, one database with a schema per module |
| [0002](0002-property-vs-inspection.md) | Property (live, editable) is separate from Inspection (a point-in-time record) |
| [0003](0003-room-snapshot.md) | PropertyRoom vs InspectionRoom: rooms are snapshotted at publish time |
| [0004](0004-tenancy-model.md) | Tenancy between Property and Inspection; tenants join by invitation |
| [0005](0005-ai-abstraction.md) | AI behind `IImageAnalysisService`, structured output, human decides |
| [0006](0006-storage-abstraction.md) | Binary files behind `IStorageService`, signed URLs, never in PostgreSQL |
| [0007](0007-report-immutability.md) | Finalized reports are immutable, versioned snapshots |
| [0008](0008-move-in-move-out-comparison.md) | Move Out compares against the Move In *report snapshot* |
| [0009](0009-authentication.md) | ASP.NET Core Identity + JWT access tokens + rotating refresh tokens |
| [0010](0010-concurrency.md) | Optimistic concurrency (xmin) + row locks for critical transitions |
| [0011](0011-capture-then-describe.md) | Agents capture all photos first; every AI text is requested in one batch |
