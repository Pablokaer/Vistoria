# 0011 — Capture all photos first, then request every AI text in one batch

**Context.** The first version had one page per room: the agent uploaded photos, pressed a button to get the AI description, edited it, recorded defects and marked the room complete before moving to the next. On site this meant switching between photographing and writing for every room, and waiting for the AI once per room and per defect.

**Decision.** Inspection execution has two frontend pages and no backend change:

1. **Capture** (`/agent/inspections/{id}/capture`) lists every room in sequence with its general photos and its defects (each defect is created empty and only receives photos). No text is entered.
2. **Continue to descriptions** validates that every room and defect has a photo, then calls the existing endpoints for every target that has no final text yet: `POST …/rooms/{roomId}/analysis`, `POST …/defects/{defectId}/analysis` and, for Move Out rooms without an AI comparison, `POST …/comparison/analysis`. The API already makes these idempotent (one running job per target) and pre-fills the final text only when it is still empty.
3. **Descriptions** (`/agent/inspections/{id}/descriptions`) polls rooms that still have a running analysis and shows each room's form only once its jobs have finished; after that the form stays mounted so a later re-run cannot discard what the agent is typing. *Complete rooms & review* flushes every autosave, completes each room and submits for review.

**Consequences.** The agent photographs the whole property in one pass and the AI work runs in parallel in the background queue. Completion rules are unchanged and still enforced by the API (`InspectionRoom.GetCompletionIssues`). Removing the last defect on the capture page also clears *defects found*, otherwise the room could not be completed. The per-room page was removed; links that pointed to it now go to the capture or descriptions page with a `#room-{id}` anchor.
