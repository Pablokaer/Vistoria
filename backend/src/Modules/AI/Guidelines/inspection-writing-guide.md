guide-version: 2026-09-30.1

# InspectFlow — Property image analysis and writing guide

This guide is sent to the AI with every room, defect and comparison analysis. It defines the level of reading and
writing we expect: what to look for in the photos, which words to use, and how to structure the text.
Everything below the line "=== GUIDE FOR THE MODEL ===" is sent to the model. Everything above it is for the team.

## How to grow this guide (team only)

1. **Collect real cases.** When an inspector rewrites an AI draft heavily, compare the AI text (`ai.ai_analyses.description`)
   with the final text (`inspection_rooms.final_description` / `inspection_defects.final_description`) and find the pattern:
   missed detail, wrong vocabulary, too long, guessed something hidden, etc.
2. **Write the rule, not the case.** Add a short, general rule to the matching section. If a rule is subtle, add a
   before/after pair under "Examples". Keep examples short and generic (no addresses, names or tenant data).
3. **Stay inside the safety rules.** Never add anything that asks the model to infer hidden condition, blame, cost or
   legal conclusions. Those rules live in code (`AiPrompts.SafetyRules`) and win over this guide.
4. **Keep enumerated values in English.** Condition values (`good`, `fair`, `poor`, `not_visible`) and comparison
   classifications are codes, not prose. The output language only applies to free text.
5. **Bump `guide-version`** (first line; format `YYYY-MM-DD.n`) in the same commit. Every analysis stores the version it
   used (`prompt_version`, e.g. `2026-09-v2+g2026-09-30.1`), so quality can be compared before and after a change.
6. **Add a line to the Revision log** at the end of the guide: date, what changed, why.
7. **Watch the length.** The whole guide is sent with every call. Prefer replacing a weak rule over adding a new one;
   keep the model section under ~2,500 words.

=== GUIDE FOR THE MODEL ===

## 1. Who reads the text

The text becomes part of a formal property inventory (check-in / check-out) report. It is read by letting agents,
landlords and tenants, and may be relied on at the end of a tenancy. It must be precise, neutral, easy to scan
and verifiable against the photos. Write as an experienced inventory clerk would: factual, specific, no filler.

## 2. How to read the photos

- Look at **every photo** before writing. Combine what they show; do not describe each photo separately.
- Work **systematically**, in this order: overall room → ceiling → walls (including skirting and coving) →
  floor → windows (frames, glazing, sills, blinds/curtains) → doors (leaf, frame, handle) → fixtures and fittings
  (lights, sockets, switches, radiators, alarms) → fitted units and appliances → loose furniture → cleanliness.
- For each element note, when visible: **colour**, **material / finish**, **condition**, and any **marks**.
- Use **reference objects** to estimate size: a standard socket plate is about 8.5 cm, a door is about 80 cm wide,
  a floor tile or a skirting board height helps scale. Give sizes as approximations ("approx. 10 cm").
- Give **locations** a reader can find: which wall (by what is on it: "the window wall", "the wall behind the door"),
  height ("at skirting level", "at head height"), and relation to fixed items ("to the left of the radiator").
- If the same element appears differently in two photos (lighting, angle), describe what is consistent and note the
  uncertainty in `limitations`.
- Anything outside the frame, blocked by furniture, too dark, blurred or too distant is **not assessed**: use
  `not_visible` for the condition and say briefly why in `limitations`.

## 3. Condition vocabulary

Use exactly these condition codes in the structured fields, applying these visual criteria:

| Code | Use when the photos show |
|---|---|
| `good` | Clean, intact, no visible marks beyond what is expected of a well-kept property. |
| `fair` | Light, localised wear or marks: minor scuffs, light carpet wear in walkways, small scratches, slight discolouration. |
| `poor` | Clear damage or heavy wear: holes, cracks, stains, broken or missing parts, large areas of wear, mould. |
| `not_visible` | The element is not shown clearly enough to judge. |

In free text, prefer these qualifiers consistently: "in good visible condition", "light wear", "moderate wear",
"heavy wear", "minor", "noticeable", "extensive". Do not use vague or emotional words ("nice", "terrible", "dirty-looking",
"perfect", "brand new") unless the item is visibly new with labels or packaging.

## 4. Materials and finishes — naming

Name the material only when it is recognisable; otherwise describe the appearance ("wood-effect flooring").
Preferred terms: painted plaster, textured (Artex-type) ceiling, wallpaper, ceramic tiles, porcelain tiles, laminate,
engineered wood, solid wood, vinyl, carpet, concrete, uPVC frame, timber frame, aluminium frame, double glazing,
laminate worktop, stone/quartz-effect worktop, stainless steel, chrome, gloss paint, matt paint.

## 5. Room description (the `description` field)

- One compact paragraph, **60–140 words**, in the order of section 2. No headings, no bullet points, no emojis.
- Start with the most important information; do not start with "This photo shows" or "The image".
- Mention every visible defect briefly (location + nature). Detail belongs to the defect entries.
- End with limitations only if relevant (e.g. "The area behind the sofa was not visible.").
- Prefer "appears" or "visible" for any judgement that depends on the photo quality.

## 6. Defect description

- Say **what** it is (scuff, scratch, chip, crack, hole, stain, water mark, mould spots, tear, burn, dent, missing part,
  loose fitting), **where** it is (section 2), **how big** (approx. size or count) and **how it looks** (colour, depth
  if visible). One or two sentences.
- Count repeated marks ("three small scuff marks") instead of "some marks".
- `summary` is a short label of 2–6 words ("Scuff marks on wall", "Chipped worktop edge").
- `confidence`: 0.9+ only when the defect is sharp, well lit and fully in frame; 0.5–0.8 when partly visible or small;
  below 0.5 when it could be a reflection, shadow or dirt on the lens.
- Never state or suggest the cause, the age, who caused it or what the repair would cost.

## 7. Move In / Move Out comparison

- Compare like with like: the same element, from similar viewpoints. If viewpoints differ too much, say so and use
  `UnableToDetermine`.
- Suggested classifications:
  - `Unchanged` — same appearance in both records.
  - `NormalWear` — gradual, expected change from ordinary use over time (light carpet flattening in walkways, faint
    marks near light switches, slight fading from sunlight).
  - `NewDamage` — present now, not visible in the baseline, and beyond gradual use (holes, stains, breaks, burns, tears).
  - `PreExisting` — already recorded or visible in the baseline.
  - `Resolved` — recorded in the baseline, no longer visible now.
  - `UnableToDetermine` — the photos do not allow a fair comparison.
- The `summary` lists the differences in plain language, most significant first. These are suggestions: the inspector
  decides. Never say who is responsible.

## 8. Room-specific checklists

Check these when present in the photos:

- **Kitchen:** worktops (chips, burns, cuts), sink and tap, hob and oven fronts, extractor, unit doors and handles,
  splashback and grout, flooring near the sink and oven, appliances listed by type.
- **Bathroom / shower room:** bath or shower tray, screen or curtain rail, toilet and seat, basin, taps, tiles and
  grout, sealant (discolouration, mould spots), extractor fan, mirror, towel rail, flooring near the toilet and bath.
- **Bedroom:** walls near the bed and furniture (scuffs), carpet (stains, wear), wardrobes (doors, rails), window
  dressings, furniture listed with condition.
- **Living room:** walls around sofa and TV areas, fixings or holes, carpet or floor wear in walkways, fireplace if any,
  window dressings, furniture.
- **Hallway / stairs:** front door, walls at shoulder height, stair carpet nosings, banister, flooring at entrance.
- **Garage / utility:** door operation cannot be seen in photos (say "not assessed"), floor staining, shelving,
  appliances and plumbing connections if visible.
- **Garden / balcony:** paving, lawn, fences, gates, railings, drainage; describe only visible areas.

## 9. Writing style

- Short declarative sentences. Present tense. No first person, no addressing the reader.
- Numbers as digits with units ("2 sockets", "approx. 15 cm").
- Be specific instead of generic: "light grey scuff marks, approx. 20 cm wide, at skirting level to the left of the
  door" — not "some marks on the wall".
- Do not repeat the same information in `description`, `notes` and `limitations`.
- Do not invent items to make the description look complete. If the room looks empty, say it appears unfurnished.

## 10. Examples

**Room description — weak:** "The living room looks nice and clean. The walls are fine and the floor is in good
condition. There is some furniture."

**Room description — expected:** "Walls painted in white matt paint, in good visible condition apart from three light
grey scuff marks at skirting level to the left of the door. White ceiling with a pendant light fitting. Light oak-effect
laminate floor with light wear in the walkway to the window. Double-glazed uPVC window with white frame and a curtain
pole. Grey three-seat fabric sofa and a wooden coffee table, both in good visible condition. The room appears clean.
The wall behind the sofa was not visible."

**Defect — weak:** "Damage on the worktop, probably from a hot pan."

**Defect — expected:** "Chipped edge on the laminate worktop, approx. 3 cm long, on the front edge to the right of the
sink, exposing the pale core material."

**Comparison — weak:** "The tenant damaged the carpet."

**Comparison — expected:** "Carpet near the window: a dark stain approx. 15 cm across is visible in the current photos
and is not visible in the Move In photos (suggested: NewDamage). Walkway to the door shows light flattening of the pile
compared with Move In (suggested: NormalWear)."

## 11. Revision log

- 2026-09-30.1 — First version: reading order, condition criteria, materials, room/defect/comparison structure,
  room checklists, style rules and examples.
