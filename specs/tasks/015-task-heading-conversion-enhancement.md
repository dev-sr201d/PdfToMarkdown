# Task: Heading Conversion Enhancement

## Task ID
015

## Feature
FRD-004 — Heading Conversion

## Description
Enhance the heading detection and conversion logic in `PdfMarkdownConverter` to satisfy all requirements and acceptance criteria of FRD-004. The current implementation (Task 011) provides basic font-size-based heading detection with levels 1–6. This task addresses the remaining gaps: PDF structural tag support, heading text whitespace handling, correct interaction between heading formatting and emphasis markers, and dedicated test coverage for heading conversion edge cases.

## Dependencies
- Task 011 (PDF Direct Markdown Converter) — the base converter implementation with font-size heading detection must be in place.

## Downstream Dependents
- Task 018 (Text Emphasis Refinement) — heading-related emphasis behavior (bold suppression) is verified cross-context there.
- FRD-009 (Chapter-Based Chunking) — relies on accurate H1 detection for chapter boundaries.

## Technical Requirements

### Structural tag precedence
- When a PDF provides explicit structural heading tags (tagged PDF with `<H1>` through `<H6>` markers), those tags must be used to determine heading levels.
- Tagged heading information must take precedence over font-size heuristics — if both are available, structural tags win.
- When structural tags are absent (untagged PDF), the existing font-size heuristic approach remains the fallback.

### Heading text processing
- Heading text must be trimmed of leading and trailing whitespace.
- Internal whitespace (e.g., double spaces) should be collapsed to a single space.
- Heading text content must otherwise be preserved verbatim — no truncation, no case changes.

### Heading emphasis interaction
- Heading text that is rendered with a bold font must NOT receive redundant `**...**` emphasis markers. Headings are inherently bold in Markdown rendering, so wrapping them in bold markers is redundant.
- Italic emphasis within heading text must still be applied (e.g., `# A heading with *italic* word`).
- Bold-italic text within headings should be rendered as italic only (`*...*`), since the bold component is already conveyed by the heading.

### Heading levels
- Support heading levels 1 through 6 (the full Markdown heading range).
- Font sizes larger than body text map to H1–H6 based on relative size (largest = H1), as already implemented.
- If more than 6 distinct heading font sizes are present, only the 6 largest are mapped; smaller sizes fall through to body text.

### Document order
- Headings must appear in the output in their original reading-order position relative to surrounding content (paragraphs, lists, tables).
- Heading detection must not reorder or displace adjacent content.
- Reading order within multi-column layouts is maintained by the spatial block discovery step (FRD-003) — headings are classified within each spatial block independently.

### No-heading scenario
- A PDF with no identifiable headings (all text at the same font size, no structural tags) must produce a valid Markdown document containing only body text — no spurious heading markers.

## Acceptance Criteria
- [ ] A PDF with clearly differentiated heading levels (via font size) produces correctly leveled Markdown headings (`#` through `######`).
- [ ] A tagged PDF with `<H1>`, `<H2>`, etc. structural tags produces headings matching those tag levels, regardless of font size.
- [ ] When both structural tags and font-size differences exist, structural tags take precedence.
- [ ] Heading text is trimmed of excess whitespace; internal double spaces are collapsed.
- [ ] Heading text that is bold does not receive `**...**` emphasis markers.
- [ ] Italic text within headings is still wrapped in `*...*`.
- [ ] Bold-italic text within headings is rendered as `*...*` only (italic, not redundant bold).
- [ ] At least 6 heading levels are supported.
- [ ] Headings appear in the correct position within the overall document flow.
- [ ] A PDF with no identifiable headings produces valid Markdown with no spurious heading markers.
- [ ] All existing converter tests continue to pass after changes.
- [ ] The project builds with zero errors and zero warnings.

## Testing Requirements
Unit tests using mock writer, placed in the test project. Mark heading-specific tests with descriptive names. Test PDF fixtures should be placed in `TestData/`.

- [ ] **Tagged PDF headings** — A tagged PDF with H1, H2, H3 structural tags produces correctly leveled Markdown headings matching the tag levels.
- [ ] **Tag precedence over font size** — A tagged PDF where font sizes contradict the tag levels produces headings matching the tags, not the font sizes.
- [ ] **Untagged PDF headings** — An untagged PDF with multiple font sizes produces headings based on font-size heuristics (largest = H1).
- [ ] **Six heading levels** — A PDF with 6 distinct heading font sizes produces `#` through `######` headings.
- [ ] **More than six font sizes** — A PDF with 7+ distinct heading font sizes maps only the 6 largest; smaller sizes are body text.
- [ ] **Heading whitespace trimming** — Heading text with leading, trailing, and double internal spaces is trimmed and collapsed.
- [ ] **Bold suppression in headings** — A heading with bold font does not produce `**...**` markers.
- [ ] **Italic in headings** — A heading containing an italic word produces `*...*` for that word.
- [ ] **Bold-italic in headings** — A bold-italic heading word produces `*...*` only (not `***...***`).
- [ ] **No headings** — A PDF with all text at the same font size and no structural tags produces body text only.
- [ ] **Heading position in flow** — A PDF with interleaved headings and paragraphs preserves the correct reading order.
- Minimum test coverage for heading-related code paths: ≥85%.
