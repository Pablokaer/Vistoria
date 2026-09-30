# 0002 — Property versus Inspection

**Context.** A property is edited over years (address typos, rooms added). An inspection is evidence of the condition *on a date* and may be used in a dispute.

**Decision.** `Property` is live, editable master data owned by a company. `Inspection` references the property but never relies on its current state to describe the past: rooms are snapshotted (ADR 0003) and the final report stores its own copy of property/company/agent/tenant data (ADR 0007).

**Consequences.** Editing a property after an inspection is always safe. Some data is duplicated on purpose; that is the price of reproducible history.
