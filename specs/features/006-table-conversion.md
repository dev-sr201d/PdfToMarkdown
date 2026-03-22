# Feature: Table Conversion

## Feature ID
FRD-006

## PRD Traceability
- **REQ-6**: The conversion must correctly map PDF tables to Markdown table syntax.
- **Goal**: Structural fidelity — tables in the source PDF are correctly represented in the Markdown output for ≥ 90% of well-structured text-based PDFs.
- **User Story**: "As a developer working with technical documentation, I want [...] tables [...] in the PDF to be accurately reflected in the Markdown output, so that the converted document is immediately usable without manual cleanup."

## Description
The system must detect tables in the parsed PDF content and convert them to standard Markdown table syntax, preserving the grid structure (rows and columns), header rows, and cell content.

## Inputs
- PDF content with structural table/cell groupings and text content as extracted during the parsing/conversion pass (FRD-003).

## Outputs
- Markdown table syntax with:
  - Pipe-delimited columns (`| col1 | col2 |`).
  - A separator row after the header (`|---|---|`).
  - One row per data row in the source table.

## Functional Requirements
1. The system must detect table structures in the parsed PDF content, using structural tags (e.g., `<Table>`, `<TR>`, `<TD>`) where available, or heuristics (aligned columns, grid patterns) where tags are absent.
2. Table detection must operate within the spatial blocks produced by the spatial block discovery step (FRD-003). Table cell gaps use a separate, fixed horizontal gap threshold that is distinct from the adaptive column boundary threshold used for page-level column detection — this prevents table cell spacing from being misidentified as column splits, and vice versa.
3. The system must identify the header row of a table (typically the first row or a row with distinct formatting) and render it as the Markdown table header.
4. The system must render a separator row (`|---|---|...`) immediately after the header row, as required by Markdown table syntax.
5. The system must render each subsequent row as a Markdown table data row.
6. Cell content must be preserved accurately, including any inline text formatting (which may be further processed by FRD-007).
7. The system must handle tables with varying numbers of columns across rows gracefully — padding shorter rows with empty cells to maintain consistent column counts.
8. Tables must be placed in the output in their original reading-order position relative to surrounding content.
9. The system must handle cells containing multi-line text by collapsing it to single-line content within the cell (Markdown tables do not support multi-line cells).

## Acceptance Criteria
- [ ] A PDF with a simple table (header + data rows) produces a correctly formatted Markdown table.
- [ ] The header row is correctly identified and rendered with a separator row beneath it.
- [ ] All data rows are rendered with the correct number of columns and accurate cell content.
- [ ] Tables with inconsistent column counts across rows are handled without errors (shorter rows are padded).
- [ ] Cell text content is preserved accurately.
- [ ] Tables appear in the correct position within the overall document flow.
- [ ] A PDF with no tables produces a valid Markdown document with no spurious table syntax.

## Dependencies
- **FRD-003** (PDF Parsing & Direct Markdown Conversion) — table conversion rules are applied during the parsing/conversion pass using structural table/cell groupings.
- **FRD-007** (Text Emphasis Preservation) — table cell content may contain bold/italic text.

## Notes
- Markdown table syntax has inherent limitations (no merged cells, no multi-line cells, no complex formatting). The conversion should produce the best-effort representation within these constraints.
- Tables that rely heavily on visual alignment rather than structural tags may be harder to detect; degraded results for such cases are acceptable per PRD constraints.
- The spatial block discovery step (FRD-003) is specifically designed to preserve table cell gaps during column boundary detection. Only gaps with strong cross-line support are treated as column boundaries; table cell spacing, which appears in only a few rows, is left intact so that the table detection heuristic can identify cell boundaries correctly.
