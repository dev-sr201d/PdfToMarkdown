# Feature: PDF Parsing & Direct Markdown Conversion

## Feature ID
FRD-003

## PRD Traceability
- **REQ-3**: The server must convert the content of the provided PDF into well-formed Markdown.
- **Assumption**: The target PDFs are text-based (not scanned images) and contain machine-readable text layers.
- **Constraint**: Conversion quality depends on the structural quality of the source PDF.

## Description
The system must read a text-based PDF file, extract its content, and convert it directly to Markdown during parsing — without constructing a separate intermediate document model. Because PDFs store content in render order rather than logical reading order, the system must reconstruct reading order through spatial analysis — grouping words into lines, partitioning lines into spatially cohesive blocks, and sorting blocks for correct reading order (including multi-column layouts). The system must then classify block-level elements (headings, paragraphs, lists, tables) within each spatial block and emit the corresponding Markdown syntax. Converted Markdown content must be written to disk incrementally on a page-by-page basis rather than accumulated entirely in memory before writing.

This feature encompasses content extraction, spatial analysis, and Markdown conversion in a two-pass pipeline (pre-analysis followed by conversion). The conversion rules for specific element types — headings (FRD-004), lists (FRD-005), tables (FRD-006), and emphasis (FRD-007) — still apply and must be respected during this direct conversion.

## Inputs
- An absolute local file system path to a valid, readable, text-based PDF file.

## Outputs
- Markdown content written incrementally to the output file(s) as pages are processed. The system does not produce an intermediate data structure or file — it writes Markdown directly.

## Functional Requirements

### Parsing & Direct Conversion
1. The system must open and read PDF files from the local file system.
2. The system must reconstruct the correct reading order from the spatial positions of text on each page. PDF content streams do not guarantee logical reading order, so the system must use spatial analysis rather than relying on the raw content stream order.
3. The system must extract font metadata (size, weight/bold, style/italic) for each text segment to support heading detection and emphasis preservation.
4. The system must identify structural groupings such as paragraphs, list items, and table cells where the PDF structure provides this information.
5. The system must classify each block-level element by type (heading, paragraph, list, table) and emit the corresponding Markdown syntax directly during parsing.
6. The system must handle multi-page PDFs, processing all pages sequentially.
7. The system must support cancellation — long-running operations should respect the cancellation signal from the caller.
8. The system must not attempt OCR or image-based text extraction — only machine-readable text layers are supported.
9. The system must raise clear errors when:
   - The file cannot be opened or read.
   - The PDF is encrypted or password-protected.
   - The PDF contains no extractable text content.

### Spatial Block Discovery
10. On each page, the system must group extracted words into text lines based on vertical proximity.
11. The system must partition text lines into spatially cohesive blocks — groups of lines that are vertically close and share significant horizontal overlap. Each block represents a logical region of content (e.g., a column, a heading, a paragraph cluster).
12. The system must detect multi-column layouts by identifying consistent horizontal gap positions that recur across many lines. Only gaps with strong support (appearing in a significant proportion of multi-word lines) qualify as column boundaries — incidental gaps (such as table cell spacing) must not be treated as column splits.
13. Lines must be split into segments only at detected column boundaries. Gaps that do not align with column boundaries must be preserved intact so that table cell spacing and other intra-line structure are not disrupted.
14. The system must calculate gap thresholds adaptively from the page's inter-word spacing distribution rather than using fixed thresholds alone.
15. The system must sort spatial blocks for correct reading order. In multi-column layouts, full-width blocks (spanning more than 60% of page width) act as section separators; between separators, column blocks are read left-to-right, top-to-bottom.
16. Element classification (headings, paragraphs, lists, tables) must operate within each spatial block independently, preserving the block's reading order.

### Incremental Output
17. The system must write converted Markdown content to disk incrementally as pages are processed, rather than accumulating the entire document's Markdown content in memory before writing.
18. The system must not require holding the full converted output of the document in memory at any point during processing.
19. Each page's converted content must be flushed to the output file before proceeding to the next page.

### Pre-analysis
20. If determining heading levels or other structural properties requires knowledge of the full document (e.g., identifying the most common font size across all pages), the system may perform an initial read pass over the document to gather this information before the conversion pass begins.
21. The pre-analysis pass must only collect metadata needed for accurate conversion — it must not extract or store the full text content of the document.

## Acceptance Criteria
- [ ] A well-structured text-based PDF is parsed and converted directly to Markdown without producing an intermediate data structure or file.
- [ ] Font metadata (size, bold, italic) is used to correctly identify headings and emphasis during conversion.
- [ ] Structural elements (paragraphs, lists, tables) are identified and converted to Markdown where the PDF provides structural information.
- [ ] Each block-level element is classified by type and emitted as the corresponding Markdown syntax.
- [ ] Multi-column PDF layouts are correctly handled — content from each column is read in the correct order (left-to-right, top-to-bottom within each column).
- [ ] Spatial block discovery partitions page content into cohesive blocks based on vertical proximity and horizontal overlap.
- [ ] Column boundaries are detected adaptively and only gaps with strong cross-line support are treated as column splits — table cell spacing and incidental gaps are preserved.
- [ ] Multi-page PDFs are fully processed across all pages.
- [ ] Markdown content is written to disk incrementally — not accumulated entirely in memory before writing.
- [ ] A PDF with no extractable text content produces a clear, descriptive error — not an empty output or crash.
- [ ] An encrypted or password-protected PDF produces a clear error message.
- [ ] Cancellation is respected and stops processing promptly without resource leaks.

## Dependencies
- None (this is a foundational service feature).

## Downstream Dependents
- **FRD-004** (Heading Conversion) — heading conversion rules applied during parsing.
- **FRD-005** (List Conversion) — list conversion rules applied during parsing.
- **FRD-006** (Table Conversion) — table conversion rules applied during parsing.
- **FRD-007** (Text Emphasis Preservation) — emphasis rules applied during parsing.

## Notes
- There is no separate intermediate document model or JSON persistence. Parsing and Markdown conversion are a single unified operation.
- The conversion rules defined in FRD-004 through FRD-007 describe WHAT the Markdown output must look like for each element type. Those rules are applied inline during the parsing/conversion pass — not as a separate transformation step.
- Spatial block discovery is the foundational mechanism that enables correct reading order reconstruction, multi-column support, and per-block element classification. It replaces reliance on the PDF content stream order, which is not guaranteed to match logical reading order.
- The spatial block algorithm uses adaptive thresholds derived from each page's actual word spacing distribution. Column boundaries require strong statistical support (recurring across many lines) to avoid false positives from table cell gaps or justified text spacing.
- The quality of output is bounded by the structural quality of the input PDF. Poorly-structured PDFs may yield incomplete structural information, which is acceptable per PRD constraints.
- Only text-based PDFs are in scope. Scanned-image PDFs are explicitly out of scope per the PRD.
