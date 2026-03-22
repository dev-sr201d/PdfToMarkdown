# Task: Table Detection and Conversion

## Task ID
017

## Feature
FRD-006 — Table Conversion

## Description
Implement table detection and Markdown table conversion in `PdfMarkdownConverter`. The current implementation (Task 011) includes no table handling — table detection is referenced in the task description but not yet implemented. This task adds the complete table pipeline: detecting tabular structures in PDF content, identifying header rows, emitting Markdown pipe-delimited table syntax, and handling edge cases such as varying column counts and multi-line cell content.

## Dependencies
- Task 011 (PDF Direct Markdown Converter) — the base converter implementation and page-processing loop must be in place. Table detection integrates into the existing element classification within `ConvertPageToMarkdown`.

## Technical Requirements

### Table detection
- The converter must detect tabular structures in the parsed PDF content.
- When the PDF provides explicit structural table tags (e.g., `<Table>`, `<TR>`, `<TD>`, `<TH>`), those must be used to identify table boundaries, rows, and cells. Structural tags take precedence over heuristic detection.
- When structural tags are absent, the converter must use positional heuristics to identify tables: groups of text elements arranged in grid-aligned columns with consistent horizontal spacing across multiple rows.
- Table detection must be integrated into the existing element classification logic so tables are identified before content falls through to default paragraph handling. Specifically, tables must be checked before style-based heading detection to avoid misidentifying bold table header rows as headings.
- Table detection operates within each spatial block independently. The spatial block discovery step (FRD-003) preserves table cell gaps by only splitting lines at detected column boundaries — table cell spacing that appears in only a few rows is not treated as a column split.

### Column and row identification
- For heuristic-based detection, words within each line are grouped into cells based on horizontal gaps exceeding a fixed column gap threshold (separate from the adaptive threshold used for page-level column boundary detection). This separation ensures table cell detection is not affected by the column detection algorithm.
- A minimum of 2 columns and 2 rows of aligned text is required to classify a region as a table.
- Column alignment is verified by checking that the first column's X position is consistent (within alignment tolerance) across all candidate rows.

### Header row identification
- The first row of a detected table is treated as the header row.
- If the first row has distinct formatting (e.g., bold font, different font size) or if structural tags indicate a header (`<TH>`), that strengthens the header identification. However, in the absence of distinguishing markers, the first row is still treated as the header.

### Markdown table output
- Tables must be emitted using standard Markdown pipe-delimited syntax:
  ```
  | Header 1 | Header 2 |
  |---|---|
  | Cell 1 | Cell 2 |
  ```
- A separator row (`|---|---|...`) must be emitted immediately after the header row.
- Each subsequent row is emitted as a data row with pipe delimiters.
- Cell content must be trimmed of leading and trailing whitespace.

### Varying column counts
- Tables where some rows have fewer columns than the header must be handled gracefully — shorter rows are padded with empty cells to maintain consistent column counts.
- Tables where some rows have more columns than the header are clipped to the header column count.

### Multi-line cell content
- Markdown tables do not support multi-line cells. If a cell contains text that spans multiple lines in the PDF, the content must be collapsed to a single line within the cell (lines joined with a space).

### Inline emphasis in cells
- Cell text must be processed through the existing emphasis formatting logic so that bold, italic, and bold-italic text within cells is preserved with Markdown emphasis markers.

### Document order
- Tables must appear in the output in their original reading-order position relative to surrounding content.
- Table detection must not reorder or displace adjacent content.
- Reading order within multi-column layouts is maintained by the spatial block discovery step (FRD-003) — table detection operates within each spatial block independently.

### No-table scenario
- A PDF with no identifiable tables must produce a valid Markdown document with no spurious table syntax (no stray pipe characters or separator rows).

## Acceptance Criteria
- [ ] A PDF with a simple table (header + data rows) produces a correctly formatted Markdown table with pipe delimiters and separator row.
- [ ] A tagged PDF with `<Table>`, `<TR>`, `<TD>` tags produces a Markdown table matching the tagged structure.
- [ ] When both structural tags and heuristic patterns exist, structural tags take precedence.
- [ ] The header row is correctly identified and rendered above the separator row.
- [ ] All data rows are rendered with the correct number of columns and accurate cell content.
- [ ] Tables with inconsistent column counts across rows are handled without errors (shorter rows padded, longer rows clipped).
- [ ] Multi-line cell content is collapsed to a single line.
- [ ] Bold, italic, and bold-italic text within table cells is preserved with Markdown emphasis markers.
- [ ] Cell text is trimmed of excess whitespace.
- [ ] Tables appear in the correct position within the overall document flow.
- [ ] A PDF with no tables produces a valid Markdown document with no spurious table syntax.
- [ ] All existing converter tests continue to pass after changes.
- [ ] The project builds with zero errors and zero warnings.

## Testing Requirements
Unit tests using mock writer, placed in the test project. Test PDF fixtures should be placed in `TestData/`.

- [ ] **Simple table** — A PDF with a 3-column, 3-row table produces correct Markdown table syntax with header, separator, and data rows.
- [ ] **Header identification** — The first row of a detected table is rendered as the Markdown header row above the separator.
- [ ] **Cell content accuracy** — All cell text content in a known table is preserved accurately in the Markdown output.
- [ ] **Inconsistent columns (fewer)** — A table where one data row has fewer columns than the header produces a padded row with empty cells.
- [ ] **Inconsistent columns (more)** — A table where one data row has more columns than the header clips to the header column count.
- [ ] **Multi-line cell** — A cell containing wrapped text spanning two lines in the PDF produces single-line cell content in the Markdown table.
- [ ] **Emphasis in cells** — Bold and italic text within table cells preserves `**...**` and `*...*` markers.
- [ ] **Tagged PDF table** — A tagged PDF with table structural tags produces a correctly structured Markdown table.
- [ ] **No tables** — A PDF with only paragraphs produces no table syntax.
- [ ] **Table position in flow** — A PDF with a table between two paragraphs preserves the correct reading order (paragraph → table → paragraph).
- [ ] **Table with single data row** — A table with only a header row and one data row produces valid Markdown table syntax.
- [ ] **Empty cells** — A table containing empty cells renders them as empty between pipe delimiters.
- Minimum test coverage for table-related code paths: ≥85%.
