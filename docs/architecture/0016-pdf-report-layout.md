# 0016 — PDF report layout: themed components, keep-together rules, preview-driven review

**Context.** The PDF is the document people rely on at the end of a tenancy, and it is frozen at finalization. The first
version was a single long renderer. That made its layout hard to change or review, and page breaks were unpredictable:
- text was split from its photos;
- photos sat alone with half-empty pages below them;
- decisions were separated from their room.

**Decision.**
- **Structure.**
  - `PdfTheme` holds every visual token: colours, spacing, type scale, photo heights and keep-together thresholds.
  - `PdfIcons` holds the line icons and the logo mark.
  - `Components/` holds one component per part of the document:
    - `CoverSection`: details and important information;
    - `RoomsOverview`: an index with page links;
    - `PageChrome`;
    - `RoomSection`;
    - `PhotoGrid`;
    - `DefectSection`;
    - `ComparisonSection`: Move In | Move Out side by side;
    - `InspectionSummarySection`: figures, defects by classification, the comparison table and report details.
  - `QuestPdfReportService` only assembles them: a cover page set with a footer only, then content pages with a small
    header.
  - All labels and dates come from `ReportPdfText` (en / pt-BR).
- **Page-break rules**, all expressed with `PdfTheme` constants:
  - A room header always stays with its photos (`RoomMinSpace`).
  - The last photo row stays with the condition text, and a text of up to `KeepWithPhotosMaxChars` stays on the same
    page as its photos.
  - A single photo sits beside a short condition instead of taking the full width.
  - A defect card keeps its description beside up to two photos.
  - On Move Outs, "possible changes" and the inspector's decision form one unbreakable block, so the decision is never
    orphaned on the next page.
  - The whole summary is kept on one page. Only a comparison table longer than `SummaryKeepTogetherMaxRooms` rows may
    flow across pages.
- **Move In photos in Move Out reports:** they come from the immutable baseline snapshot (`ReportRoomComparison.BaselinePhotos`).
  `ReportImageLoader.PhotoKeys` loads them together with the room and defect photos. Before this, real Move Out
  reports showed "Photo unavailable" in the Before column.
- **Review workflow:** `ReportPdfRenderingTests` renders seven scenarios from `Support/ReportSamples`:
  - no defects;
  - with defects;
  - many photos;
  - long texts;
  - optional data missing;
  - Move Out (en);
  - Move Out (pt-BR).

  QuestPDF throws on layouts that can never fit, so rendering is itself a check. With `PDF_PREVIEW_DIR` set, the
  tests also write each PDF and its pages as PNG for page-by-page review.

**Consequences.**
- Layout changes are local to a component or a theme constant, and they are reviewed against the same seven
  scenarios.
- Keep-together rules trade some white space for readability. For example, a room that does not fit in the
  remaining third of a page starts on the next page.
- Not implemented:
  - An "assessment limited" marker: the snapshot has no reliable flag for it. The AI draft's `limitations` field may
    have been edited away by the inspector.
  - The cover still leaves some space at the bottom for short reports.
