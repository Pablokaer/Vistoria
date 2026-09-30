# 0003 — PropertyRoom vs InspectionRoom snapshot

**Context.** Rooms are configurable (`PropertyRoom`, any number, any type). If an inspection pointed at live rooms, renaming "Bedroom 2" to "Office" would silently rewrite old inspections.

**Decision.** When an inspection is **published**, every active `PropertyRoom` is copied into an `InspectionRoom` (type, name, sequence) keeping only `OriginalPropertyRoomId` as a link. All findings (photos, AI text, final text, defects, comparison) attach to `InspectionRoom`. `PropertyRoom`s are archived, never deleted, so the original id keeps matching future inspections.

**Consequences.** Property changes affect only future publications (covered by `Published_inspection_rooms_are_a_snapshot...`). Drafts do not snapshot, so a company can still fix rooms before publishing. Choosing a subset of rooms per inspection is not in the MVP (all active rooms are included; `IsRequired` is prepared).
