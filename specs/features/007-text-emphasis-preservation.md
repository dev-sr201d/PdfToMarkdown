# Feature: Text Emphasis Preservation

## Feature ID
FRD-007

## PRD Traceability
- **REQ-7**: The conversion must preserve bold and italic emphasis in the Markdown output.
- **Goal**: Structural fidelity — emphasis in the source PDF is correctly represented in the Markdown output for ≥ 90% of well-structured text-based PDFs.
- **User Story**: "As a developer working with technical documentation, I want [...] emphasized text in the PDF to be accurately reflected in the Markdown output, so that the converted document is immediately usable without manual cleanup."

## Description
The system must detect bold and italic formatting in the parsed PDF content and apply the corresponding Markdown emphasis syntax. This applies to text in all contexts — body paragraphs, list items, table cells, and any other content area.

## Inputs
- PDF content with font metadata (weight, style) for each text segment as extracted during the parsing/conversion pass (FRD-003).

## Outputs
- Markdown emphasis syntax applied to the appropriate text spans:
  - Bold text wrapped in `**...**`.
  - Italic text wrapped in `*...*`.
  - Bold-italic text wrapped in `***...***`.

## Functional Requirements
1. The system must detect bold text segments (based on font weight metadata) and wrap them in Markdown bold syntax (`**text**`).
2. The system must detect italic text segments (based on font style metadata) and wrap them in Markdown italic syntax (`*text*`).
3. The system must detect text that is both bold and italic and wrap it in combined syntax (`***text***`).
4. Emphasis must be applied at the inline level — only the emphasized span of text is wrapped, not entire paragraphs or blocks.
5. Adjacent text segments with the same emphasis should be merged into a single emphasis span (e.g., two consecutive bold segments should produce one `**...**` wrapper, not `**word1****word2**`).
6. Emphasis markers must not break across word boundaries in a way that produces invalid Markdown.
7. Emphasis detection must work consistently across all content types: body text, headings, list items, and table cells.
8. Heading text that happens to also be bold should not produce redundant emphasis (headings are inherently bold in rendering); however, italic emphasis within headings should still be applied.

## Acceptance Criteria
- [ ] Bold text in the PDF is rendered with `**...**` in the Markdown output.
- [ ] Italic text in the PDF is rendered with `*...*` in the Markdown output.
- [ ] Text that is both bold and italic is rendered with `***...***`.
- [ ] Emphasis is applied at the span level, not the block level — surrounding non-emphasized text is unaffected.
- [ ] Adjacent same-emphasis segments are merged into a single emphasis span.
- [ ] Emphasis works correctly within list items and table cells.
- [ ] A PDF with no emphasized text produces a valid Markdown document with no spurious emphasis markers.

## Dependencies
- **FRD-003** (PDF Parsing & Direct Markdown Conversion) — emphasis rules are applied during the parsing/conversion pass using font weight and style metadata.

## Notes
- Determining whether text is "bold" vs. "regular heading weight" may require context-aware logic. The specific heuristic is an implementation concern; this FRD requires only that the result is correct for well-structured PDFs.
- Some PDFs use font substitution or custom font names to indicate emphasis (e.g., "Arial-Bold"). The system should handle common font-naming conventions.
