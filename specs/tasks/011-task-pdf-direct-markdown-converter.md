# Task: PDF Direct Markdown Converter

## Task ID
011

## Supersedes
- Task 011 (Document Model JSON Serialization) — JSON serialization is no longer part of the architecture.
- Task 012 (PDF Parser Core Implementation) — the parser's functionality is merged into this task. Instead of building a `DocumentModel`, the converter produces Markdown directly during parsing.

## Feature
FRD-003 — PDF Parsing & Direct Markdown Conversion

## Description
Implement the PDF-to-Markdown converter service — the core service of the new architecture. This service reads a text-based PDF file, extracts its content, classifies structural elements, and converts them directly to Markdown syntax during parsing. Converted Markdown content is passed to the provided writer incrementally on a page-by-page basis, without constructing an intermediate document model or accumulating the full output in memory.

The converter may perform an initial pre-analysis read pass over the document to gather metadata needed for accurate conversion (e.g., determining the body font size for heading level classification). The pre-analysis pass must only collect metadata — it must not extract or store the full text content of the document.

The conversion rules for specific element types — headings (FRD-004), lists (FRD-005), tables (FRD-006), and emphasis (FRD-007) — are applied inline during the conversion pass.

## Dependencies
- Task 006 (Conversion Pipeline Service Contracts) — the converter interface and writer interface must be defined.
- Task 010 (Incremental Markdown Writer) — the writer implementation must exist for integration testing (unit tests may use mocks).

## Technical Requirements

### PDF parsing library
- Use the **UglyToad.PdfPig** NuGet package (already in the project) for PDF reading. PdfPig is a fully managed, open-source .NET library for reading PDF content with no native dependencies.

### Pre-analysis pass
- Before the conversion pass, perform an initial read-only pass over the document to gather metadata needed for accurate structural classification.
- At minimum, determine the body text font size by analyzing font size frequency distribution across all pages. The most common font size is the body text size.
- Derive heading level thresholds from the body text size — text at larger font sizes maps to heading levels 1–6, with the largest size mapping to level 1.
- The pre-analysis pass must not extract or store the full text content of the document — only collect font size statistics and other structural metadata.
- Check the cancellation token between pages during pre-analysis.

### Conversion pass (page-by-page)
1. **Text extraction** — Extract words from each page using PdfPig's `page.GetWords()` API. Extract individual words with their associated font metadata (size, bold, italic, font name) and bounding box positions.

2. **Spatial block discovery** — Reconstruct reading order and logical content grouping through spatial analysis of word positions:
   - **Word-to-line grouping** — Group words into text lines based on vertical proximity (±2.0 pt tolerance). Sort words by Y position descending (top-of-page first in PDF coordinates), then X ascending (left-to-right).
   - **Adaptive column gap calculation** — Compute a page-specific gap threshold from the inter-word spacing distribution. Use 4× the median word gap with a minimum of 25 pt. This avoids false column splits from justified text while detecting narrow column gutters.
   - **Consistent column boundary detection** — Scan all lines for horizontal gaps exceeding the adaptive threshold. Cluster gap center positions that are within alignment tolerance. Only clusters with strong support (appearing in ≥8 lines or ≥30% of multi-word lines, whichever is larger) are treated as column boundaries. This prevents table cell spacing from being misidentified as column structure.
   - **Line splitting at column boundaries** — Split each line into segments only at gaps that align with detected column boundaries. Gaps that do not match a column boundary are preserved intact (e.g., table cell gaps remain within the line).
   - **Block clustering** — Assign each line segment to a spatial block based on vertical proximity (within 1.5× the median line spacing) and horizontal overlap (≥30% overlap relative to the narrower element). Segments that do not fit an existing block start a new block.
   - **Reading order sorting** — Sort blocks for correct reading order. Detect multi-column layouts by checking for narrow blocks on both sides of the page midpoint. Full-width blocks (>60% of page width) act as section separators. Between separators, output left-column blocks top-to-bottom, then right-column blocks top-to-bottom.

3. **Font metadata detection**
   - For each text segment, capture: font size, bold (weight), italic (style), and font name.
   - Bold detection: use font name heuristics (names containing "Bold", "Heavy", "Black") and/or font descriptor flags.
   - Italic detection: use font name heuristics (names containing "Italic", "Oblique") and/or font descriptor flags.

4. **Block-level element classification** — Within each spatial block, classify lines into structural elements:
   - **Headings**: Text at a font size larger than body text, mapped to heading levels 1–6 based on the pre-analysis font size thresholds. Additionally, short lines (≤8 words) that are all-bold at body font size or use a different font family than body text are classified as style-based headings.
   - **Tables**: Detected before style-based headings to avoid misidentifying bold table headers. A table requires ≥2 consecutive lines, each with ≥2 cells (word groups separated by gaps exceeding the column gap threshold), with aligned first-column positions across rows.
   - **Paragraphs**: Consecutive text runs at body text font size grouped into paragraphs.
   - **Lists**: Lines beginning with bullet characters (•, ‣, ▪, -, *, –, ◦, ●) for unordered lists, or numeric/letter prefixes followed by a period, parenthesis, or bracket for ordered lists.

5. **Direct Markdown emission** — For each classified element, emit the corresponding Markdown syntax directly:
   - Headings → `#`, `##`, `###`, etc. with correct levels (per FRD-004)
   - Paragraphs → plain text with blank line separation
   - Bold text → `**text**` (per FRD-007)
   - Italic text → `*text*` (per FRD-007)
   - Bold + italic → `***text***` (per FRD-007)
   - Unordered lists → `- item` (per FRD-005)
   - Ordered lists → `1. item` with correct numbering (per FRD-005)
   - Tables → pipe-delimited table syntax with header separator row (per FRD-006)

6. **Incremental output** — After converting all spatial blocks on a page, pass the page's combined Markdown content to the provided writer. Do not accumulate Markdown content across pages in the converter.

### Multi-page support
- Process all pages in the PDF sequentially.
- Check the cancellation token between pages.

### Error handling
- **File cannot be opened or read** — Throw `McpException` with a message including the file path and the underlying I/O error description.
- **Encrypted or password-protected PDF** — Throw `McpException` with a message indicating the PDF is encrypted and cannot be processed.
- **No extractable text content** — After processing all pages, if no text content was extracted, throw `McpException` indicating the PDF contains no machine-readable text.
- **No OCR** — The converter must not attempt OCR or image-based text extraction. Only machine-readable text layers are supported.

### DI registration
- Register the converter implementation as the implementation of the converter interface from Task 006, replacing the stub.
- Remove the old `PdfParser` and `CachingPdfParser` implementations if not already removed by Task 006.

### Design constraints
- Accept `CancellationToken` as the last parameter on all async methods.
- Use `ConfigureAwait(false)` in library-level code.
- Use `ILogger<T>` for diagnostic logging (log at `Information` level when starting/completing conversion, `Warning` for recoverable issues like unrecognized PDF structure, `Error` for failures).
- All public members must have XML documentation comments.
- Follow AGENTS.md naming and code layout conventions.

## Acceptance Criteria
- [ ] The converter implements the converter interface from Task 006.
- [ ] A well-structured text-based PDF is converted directly to Markdown without producing an intermediate data structure or file.
- [ ] A pre-analysis pass determines body font size and heading thresholds before conversion begins.
- [ ] Spatial block discovery partitions page content into cohesive blocks and reconstructs correct reading order.
- [ ] Multi-column layouts are handled correctly — content is read left-to-right, top-to-bottom within each column region.
- [ ] Column boundaries are detected adaptively; table cell gaps are not misidentified as column splits.
- [ ] Font metadata (size, bold, italic) is used to correctly identify headings and emphasis during conversion.
- [ ] Headings are emitted as Markdown heading syntax with correct levels (1–6).
- [ ] Paragraphs are emitted as plain text with blank line separation.
- [ ] Bold and italic text is wrapped in Markdown emphasis syntax (`**`, `*`, `***`).
- [ ] List items are detected and emitted as Markdown list syntax (bulleted and numbered).
- [ ] Tables are detected and emitted as Markdown table syntax with header separator.
- [ ] Multi-page PDFs are fully processed across all pages.
- [ ] Markdown content is written through the writer incrementally — page-by-page, not accumulated in memory.
- [ ] A PDF with no extractable text content produces a clear `McpException`.
- [ ] An encrypted or password-protected PDF produces a clear `McpException`.
- [ ] Cancellation is respected between pages during both pre-analysis and conversion passes.
- [ ] The converter is registered in DI, replacing the stub implementation.
- [ ] Diagnostic logging is present using `ILogger<T>`.
- [ ] All public members have XML documentation comments.
- [ ] The project builds with zero errors and zero warnings.

## Testing Requirements
Unit tests must cover (use a mock writer for output capture):

- [ ] **Spatial block discovery** — A page with distinct spatial regions (e.g., heading followed by two-column text) produces blocks in correct reading order: heading first, then left column, then right column.
- [ ] **Column detection** — A two-column PDF layout produces content in left-column-then-right-column order, not interleaved.
- [ ] **Table cell gap preservation** — Table cell gaps within a line are not treated as column boundaries; table cells remain intact for table detection.
- [ ] **Simple PDF conversion** — A single-page PDF with body text produces Markdown paragraphs written to the mock writer. Verify the text content is correct.
- [ ] **Font metadata extraction** — Verify that font size, bold, and italic flags are correctly detected and used for Markdown emphasis (`**`, `*`).
- [ ] **Heading detection and conversion** — Text at a larger font size than body text is emitted as a Markdown heading (`#`, `##`, etc.) with the correct level.
- [ ] **Multi-page conversion** — A multi-page PDF produces separate writer calls for each page, with correct content on each call.
- [ ] **List detection and conversion** — Lines starting with bullet characters produce Markdown unordered list syntax; lines with numeric prefixes produce ordered list syntax.
- [ ] **Table detection and conversion** — Tabular content produces Markdown table syntax with header separator.
- [ ] **Pre-analysis determines body font** — Verify the most common font size is used as the body text threshold for heading classification.
- [ ] **Incremental output** — Verify that the writer receives content page-by-page (one call per page), not as a single accumulated string.
- [ ] **Cancellation during pre-analysis** — A pre-cancelled token stops the pre-analysis pass with `OperationCanceledException`.
- [ ] **Cancellation during conversion** — A cancellation token triggered mid-conversion stops processing promptly with `OperationCanceledException`.
- [ ] **Error: file not found** — Converting a nonexistent path throws `McpException`.
- [ ] **Error: encrypted PDF** — Converting a password-protected PDF throws `McpException` with an encryption-related message.
- [ ] **Error: no text content** — Converting a PDF with only images (no text layer) throws `McpException`.
- Test PDF fixtures must be placed in `tests/PdfToMarkdown.Tests/TestData/`.
- Minimum test coverage: ≥85%.
