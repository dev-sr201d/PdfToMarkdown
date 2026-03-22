# Task: Text Emphasis Refinement and Cross-Context Verification

## Task ID
018

## Feature
FRD-007 — Text Emphasis Preservation

## Description
Refine and verify text emphasis preservation across all content contexts in `PdfMarkdownConverter` to satisfy all requirements and acceptance criteria of FRD-007. The current implementation (Task 011) provides basic bold and italic detection via font name heuristics with segment merging. This task addresses the remaining gaps: ensuring emphasis works correctly in all content types (body text, headings, list items, table cells), fixing adjacent segment merging edge cases, validating that emphasis markers do not break across word boundaries, and verifying bold suppression in headings. This task serves as the cross-cutting verification layer that confirms emphasis behavior is consistent after Tasks 015–017.

## Dependencies
- Task 015 (Heading Conversion Enhancement) — heading emphasis behavior (bold suppression, italic in headings) must be implemented.
- Task 016 (List Conversion Enhancement) — inline emphasis within list items must be implemented.
- Task 017 (Table Detection and Conversion) — inline emphasis within table cells must be implemented.

## Technical Requirements

### Cross-context emphasis consistency
- Emphasis detection and formatting must produce correct results in all content contexts: body paragraphs, headings, list items, and table cells.
- The same underlying emphasis formatting logic (e.g., `FormatInlineEmphasis`) must be used across all contexts to ensure consistency.
- Verify that emphasis is applied at the inline (span) level in every context — only the emphasized words are wrapped, not entire paragraphs, list items, or cells.

### Bold detection
- Text segments rendered with a bold font must be wrapped in `**...**`.
- Bold detection must use font name heuristics: names containing "Bold", "Heavy", or "Black" (case-insensitive).
- Consider adding font descriptor flag support if available in PdfPig's API, as a secondary detection method.

### Italic detection
- Text segments rendered with an italic font must be wrapped in `*...*`.
- Italic detection must use font name heuristics: names containing "Italic" or "Oblique" (case-insensitive).

### Bold-italic combination
- Text that is both bold and italic must be wrapped in `***...***`.
- In heading context, bold-italic text must render as `*...*` only (bold is suppressed in headings).

### Adjacent segment merging
- Consecutive words with the same emphasis (both bold, both italic, or both bold-italic) must be merged into a single emphasis span.
- Merging must not produce invalid Markdown such as `**word1****word2**` — these must become `**word1 word2**`.
- When emphasis changes mid-sentence (e.g., regular then bold then regular), each transition must produce its own correctly opened and closed emphasis span.

### Emphasis marker validity
- Emphasis markers must not produce invalid Markdown (e.g., unclosed markers, markers that start inside one word and end inside another).
- A trailing space before a closing emphasis marker, or a leading space after an opening marker, is invalid and must be avoided (e.g., `** text **` is invalid; `**text**` is correct).

### Heading bold suppression
- Verify that heading text rendered in bold font does not receive `**...**` markers (redundant — headings render as bold by default).
- Verify that italic emphasis within headings is still applied.
- This requirement is implemented in Task 015 but must be verified with dedicated tests here.

### No-emphasis scenario
- A PDF with no emphasized text (all text in regular weight, regular style) must produce a valid Markdown document with no spurious emphasis markers (no stray `*` or `**`).

## Acceptance Criteria
- [ ] Bold text in body paragraphs is rendered with `**...**`.
- [ ] Italic text in body paragraphs is rendered with `*...*`.
- [ ] Bold-italic text in body paragraphs is rendered with `***...***`.
- [ ] Emphasis is applied at the span level — surrounding regular text is unaffected.
- [ ] Adjacent same-emphasis words are merged into a single emphasis span (no `**a****b**`).
- [ ] Emphasis transitions mid-sentence produce correctly opened and closed spans.
- [ ] Emphasis works correctly within list items (bold, italic, bold-italic).
- [ ] Emphasis works correctly within table cells (bold, italic, bold-italic).
- [ ] Bold heading text does not receive redundant `**...**` markers.
- [ ] Italic text within headings is rendered with `*...*`.
- [ ] Bold-italic text within headings is rendered as `*...*` only.
- [ ] No emphasis markers contain leading or trailing spaces.
- [ ] A PDF with no emphasized text produces valid Markdown with no spurious markers.
- [ ] All existing converter tests continue to pass.
- [ ] The project builds with zero errors and zero warnings.

## Testing Requirements
Unit tests placed in the test project. Test PDF fixtures should be placed in `TestData/`. These tests verify emphasis behavior cross-contextually, complementing the per-context tests in Tasks 015–017.

- [ ] **Bold in body** — A body paragraph with a bold word produces `**word**` in the output.
- [ ] **Italic in body** — A body paragraph with an italic word produces `*word*` in the output.
- [ ] **Bold-italic in body** — A body paragraph with a bold-italic word produces `***word***`.
- [ ] **Span-level application** — A sentence with one bold word among regular words wraps only the bold word.
- [ ] **Adjacent merging** — Three consecutive bold words produce one `**word1 word2 word3**` span, not three separate spans.
- [ ] **Emphasis transition** — Regular → bold → regular text in one sentence produces correctly opened and closed `**...**`.
- [ ] **Emphasis in list items** — A list item containing a bold word produces `- text **bold** more text`.
- [ ] **Emphasis in table cells** — A table cell containing an italic word produces `*italic*` within the cell.
- [ ] **Bold suppression in heading** — A heading with bold font produces `# Heading Text` with no `**...**`.
- [ ] **Italic in heading** — A heading with an italic word produces `# Heading with *italic* word`.
- [ ] **Bold-italic in heading** — A bold-italic word in a heading produces `*word*` only.
- [ ] **No spurious markers** — A PDF with only regular-weight text produces no `*` or `**` in the output.
- [ ] **Mixed emphasis in document** — A full document with body text, headings, lists, and tables containing various emphasis patterns produces correct markers throughout.
- Minimum test coverage for emphasis-related code paths: ≥85%.
