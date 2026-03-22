# Task: PDF Conversion End-to-End Tests

## Task ID
014

## Supersedes
Task 014 (PDF Parsing End-to-End Tests) — the previous version tested the intermediate `DocumentModel`, JSON serialization, and `.parsed.json` caching. The new version tests the direct PDF-to-Markdown conversion pipeline with Markdown output file verification.

## Feature
FRD-003 — PDF Parsing & Direct Markdown Conversion

## Description
Create a comprehensive suite of integration tests that exercise the full PDF-to-Markdown conversion pipeline end-to-end — from a real PDF file on disk through the converter and writer to Markdown output files. These tests validate that the converter, writer, validator, and orchestrator work together correctly with realistic inputs, producing well-formed Markdown output files on disk.

This task also includes creating the test PDF fixture files required by these tests and by downstream feature tests (FRD-004 through FRD-007).

## Dependencies
- Task 008 (Conversion Pipeline Orchestrator) — the orchestrator must be implemented for full-pipeline tests.
- Task 010 (Incremental Markdown Writer) — the writer must be implemented.
- Task 011 (PDF Direct Markdown Converter) — the converter must be implemented.
- Task 012 (Input Validator Implementation) — the validator must be implemented.

## Technical Requirements

### Test PDF Fixtures

Create programmatically-generated PDF fixture files in `tests/PdfToMarkdown.Tests/TestData/`. Each fixture should target a specific structural scenario:

1. **`simple.pdf`** — A single-page PDF with a title (large font) and a few body-text paragraphs at a consistent font size. No lists, tables, or emphasis.
2. **`multi-page.pdf`** — A PDF with 3+ pages, each containing different body text. Used to verify multi-page processing.
3. **`headings.pdf`** — A PDF with text at multiple distinct font sizes, representing heading levels H1 through H3 (at minimum) plus body text. Used by downstream FRD-004 tests.
4. **`lists.pdf`** — A PDF containing both a bulleted list (using bullet characters) and a numbered list (using numeric prefixes). Used by downstream FRD-005 tests.
5. **`tables.pdf`** — A PDF containing a simple data table with header row and data rows. Used by downstream FRD-006 tests.
6. **`emphasis.pdf`** — A PDF with body text containing bold, italic, and bold-italic formatted words. Used by downstream FRD-007 tests.
7. **`mixed-content.pdf`** — A comprehensive PDF containing headings, paragraphs, a list, a table, and emphasis — all in one document. Used for full-pipeline integration testing.
8. **`no-text.pdf`** — A PDF containing only an embedded image with no text layer. Used for error-case testing.
9. **`empty.pdf`** — A valid PDF file with zero pages or zero text content.

Fixtures may be created using PdfPig's `PdfDocumentBuilder` API or any other reproducible method. The generation approach should be documented or scripted so fixtures can be regenerated if needed.

### Integration Test Scenarios

All tests should use real service implementations (converter, writer, validator, orchestrator) — not mocks. Tests should be marked with `[Trait("Category", "Integration")]`.

#### Markdown Output Correctness

- [ ] **Simple document** — Convert `simple.pdf` and verify the output `.md` file exists alongside the PDF. Verify it contains paragraph text matching the PDF content.
- [ ] **Multi-page document** — Convert `multi-page.pdf` and verify all pages' content appears in the output `.md` file in correct order.
- [ ] **Heading conversion** — Convert `headings.pdf` and verify the output contains Markdown heading syntax (`#`, `##`, `###`) at the correct levels with correct text content.
- [ ] **List conversion** — Convert `lists.pdf` and verify the output contains Markdown list syntax (bulleted with `- ` and numbered with `1. `) with correct items.
- [ ] **Table conversion** — Convert `tables.pdf` and verify the output contains Markdown table syntax with pipes, header separator, and correct cell content.
- [ ] **Emphasis preservation** — Convert `emphasis.pdf` and verify bold text is wrapped in `**`, italic text in `*`, and bold-italic in `***`.
- [ ] **Mixed content** — Convert `mixed-content.pdf` and verify the output contains headings, paragraphs, lists, tables, and emphasis in the correct order.

#### Incremental Writing Verification

- [ ] **Output file exists during conversion** — Verify (via test instrumentation or file monitoring) that the output file is created and receives content before the full conversion completes, confirming incremental writing behavior.

#### Chunked Output

- [ ] **Chunked mode produces multiple files** — Convert a multi-chapter PDF (with multiple H1 headings) using `chunkByChapter = true`. Verify separate numbered `.md` files are created (e.g., `_1.md`, `_2.md`) with correct chapter boundaries.
- [ ] **Chunked content correctness** — Verify each chapter file starts with its H1 heading (except possibly the first file if there is pre-heading content) and contains only content belonging to that chapter.
- [ ] **Chunked confirmation message** — Verify the orchestrator's confirmation message lists all output file paths.

#### Full Pipeline via Orchestrator

- [ ] **Orchestrator standard conversion** — Invoke the orchestrator with a valid PDF and `chunkByChapter = false`. Verify a single `.md` file is written and the confirmation message includes its path.
- [ ] **Orchestrator chunked conversion** — Invoke the orchestrator with a valid multi-chapter PDF and `chunkByChapter = true`. Verify multiple `.md` files are written and the confirmation message lists all paths.

#### Error Scenarios

- [ ] **Nonexistent file** — Attempt to convert a path that does not exist. Verify an `McpException` is thrown with a descriptive message.
- [ ] **No-text PDF** — Convert `no-text.pdf`. Verify an `McpException` is thrown indicating no extractable text content.
- [ ] **Empty PDF** — Convert `empty.pdf`. Verify appropriate behavior (either an `McpException` or an empty output, as fits the implementation).
- [ ] **Invalid extension** — Attempt to convert a `.txt` file. Verify an `McpException` is thrown.

### Design Constraints

- Use xUnit as the test framework with FluentAssertions for assertions.
- Name test methods following `MethodName_Condition_ExpectedResult` convention.
- Mark integration tests with `[Trait("Category", "Integration")]`.
- Test fixture PDFs must be included as project content files (copied to output directory).
- Clean up any output `.md` files created during tests to avoid polluting the test data directory (use `IDisposable` or `IAsyncLifetime` for cleanup).
- Do not reference `DocumentModel`, `.parsed.json`, or any other types/concepts from the obsolete architecture.

## Acceptance Criteria

- [ ] At least 7 test PDF fixture files are created in `TestData/` covering simple, multi-page, headings, lists, tables, emphasis, mixed content, and error scenarios.
- [ ] Fixture files are configured to copy to the output directory during build.
- [ ] Integration tests verify Markdown output correctness for all element types (headings, paragraphs, lists, tables, emphasis).
- [ ] Integration tests verify multi-page conversion produces complete output.
- [ ] Integration tests verify incremental writing behavior (output file created before conversion completes).
- [ ] Integration tests verify chunked mode produces separate numbered files at chapter boundaries.
- [ ] Integration tests verify full pipeline through the orchestrator (standard and chunked).
- [ ] Integration tests cover error scenarios (nonexistent file, no-text PDF, invalid extension).
- [ ] All tests are marked with `[Trait("Category", "Integration")]`.
- [ ] All tests pass via `dotnet test tests/PdfToMarkdown.Tests`.
- [ ] Test cleanup removes any output `.md` files generated during test runs.
- [ ] No references to obsolete types (`DocumentModel`, `.parsed.json`, `CachingPdfParser`, etc.).
