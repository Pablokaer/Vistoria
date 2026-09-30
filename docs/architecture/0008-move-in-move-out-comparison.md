# 0008 — Move In versus Move Out comparison

**Context.** Deposit discussions compare the end of a tenancy with its start. The comparison must use what was *reported* at Move In, and a machine must not decide who caused a difference.

**Decision.**
- A Move Out may reference a finalized Move In of the same property/tenancy (`ComparisonInspectionId`). At publish, each new room is mapped to the baseline room via `OriginalPropertyRoomId` and an `InspectionComparison` row is created.
- The agent sees the baseline from the **Move In report snapshot** (text, photos, defects) — never from live data — and the original inspection is never modified.
- Two aids: a deterministic text comparison (`BasicComparisonBuilder`: surface conditions, missing items, defects) and an optional AI visual comparison. Both are advisory.
- A room with a baseline cannot be completed until the agent records `AgentDecision` (NewDamage, PreExisting, NormalWear, Resolved, Unchanged, UnableToDetermine) and optional notes. The Move Out report includes a comparison summary.

**Consequences.** Rooms added after the Move In have no baseline (no comparison required); archived rooms still match. Determining financial responsibility is explicitly out of scope.
