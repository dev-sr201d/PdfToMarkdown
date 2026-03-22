# Task: List Conversion Enhancement

## Task ID
016

## Feature
FRD-005 — List Conversion

## Description
Enhance the list detection and conversion logic in `PdfMarkdownConverter` to satisfy all requirements and acceptance criteria of FRD-005. The current implementation (Task 011) provides basic flat list detection — identifying unordered lists by bullet characters and ordered lists by numeric prefixes. This task addresses the remaining gaps: nested list support, multi-line list item handling, inline emphasis within list items, PDF structural tag support, and mixed list type handling.

## Dependencies
- Task 011 (PDF Direct Markdown Converter) — the base converter implementation with basic list detection must be in place.

## Technical Requirements

### Nested list support
- The converter must detect nested lists — lists within lists — based on indentation levels in the PDF content.
- Nested list items must be rendered with appropriate Markdown indentation to represent the nesting hierarchy (e.g., 2 or 4 spaces per nesting level).
- At least 3 levels of nesting must be supported (top-level, one sub-level, one sub-sub-level).
- Nesting may change list type — e.g., an ordered list may contain an unordered sub-list, and vice versa.

### Multi-line list items
- List items that span multiple lines in the PDF must be kept intact as single list items in the Markdown output.
- Continuation lines (lines that belong to the same item but do not start with a bullet or numeric prefix) must be associated with their parent list item.
- Continuation line content must be appended to the parent item's text, separated by a space.

### Inline emphasis within list items
- The existing `FormatInlineEmphasis` method (or equivalent) must be applied to list item text, so bold, italic, and bold-italic text within list items is correctly preserved.
- The current implementation uses `GetText()` for list items, which discards font metadata; this must be changed to use word-level emphasis formatting.

### PDF structural tag support
- When the PDF provides explicit list structural tags (e.g., `<L>` for list, `<LI>` for list item), those must be used to identify list boundaries and items.
- Structural tags must take precedence over heuristic detection (bullet/numeric prefix matching) when available.
- When structural tags are absent, the existing heuristic approach remains the fallback.

### Ordered list numbering
- Ordered list items must be numbered sequentially in the Markdown output (1, 2, 3, ...).
- The original numbering from the PDF may differ (e.g., "a)", "b)", "i.", "ii."); all are converted to numeric Markdown syntax (`1. `, `2. `, etc.).

### Document order
- Lists must appear in the output in their original reading-order position relative to surrounding content.
- List detection must not reorder or displace adjacent content.
- Reading order within multi-column layouts is maintained by the spatial block discovery step (FRD-003) — list detection operates within each spatial block independently.

### No-list scenario
- A PDF with no identifiable lists must produce a valid Markdown document with no spurious list items or bullet markers.

## Acceptance Criteria
- [ ] A PDF with a bulleted list produces a correctly formatted Markdown unordered list using `- ` prefix.
- [ ] A PDF with a numbered list produces a correctly formatted Markdown ordered list using `1. ` prefix with sequential numbering.
- [ ] List item ordering is preserved exactly as in the source PDF.
- [ ] Nested lists are rendered with correct indentation reflecting the nesting hierarchy.
- [ ] Multi-line list items are kept intact as single list items (continuation lines are merged).
- [ ] Bold, italic, and bold-italic text within list items is preserved with correct Markdown emphasis markers.
- [ ] A tagged PDF with `<L>` and `<LI>` structural tags produces correctly identified lists matching those tags.
- [ ] When both structural tags and heuristic markers exist, structural tags take precedence.
- [ ] Mixed list types (e.g., numbered list containing a bulleted sub-list) are handled correctly.
- [ ] Lists appear in the correct position within the overall document flow.
- [ ] A PDF with no lists produces a valid Markdown document with no spurious list markers.
- [ ] All existing converter tests continue to pass after changes.
- [ ] The project builds with zero errors and zero warnings.

## Testing Requirements
Unit tests using mock writer, placed in the test project. Test PDF fixtures should be placed in `TestData/`.

- [ ] **Unordered list** — A PDF with bullet-prefixed items produces Markdown unordered list with `- ` prefix.
- [ ] **Ordered list** — A PDF with numbered items produces Markdown ordered list with sequential `1. `, `2. `, etc.
- [ ] **Letter-prefix ordered list** — A PDF with "a)", "b)" prefixed items produces numeric Markdown ordered list.
- [ ] **Nested unordered list** — A PDF with indented sub-items under a bullet list produces correctly indented nested list.
- [ ] **Nested mixed list** — An ordered list containing an unordered sub-list produces correct mixed nesting.
- [ ] **Multi-line list item** — A list item spanning two lines in the PDF produces a single Markdown list item with merged text.
- [ ] **Emphasis in list items** — Bold and italic text within list items is wrapped with correct Markdown emphasis markers.
- [ ] **Tagged PDF lists** — A tagged PDF with `<L>` and `<LI>` tags produces correctly structured lists.
- [ ] **Tag precedence** — A tagged PDF where tags contradict heuristic markers produces lists matching the tags.
- [ ] **No lists** — A PDF with only paragraphs produces no list markers.
- [ ] **List position in flow** — A PDF with interleaved paragraphs and lists preserves the correct reading order.
- [ ] **Consecutive separate lists** — Two distinct lists separated by a paragraph are rendered as separate lists, not merged.
- Minimum test coverage for list-related code paths: ≥85%.
