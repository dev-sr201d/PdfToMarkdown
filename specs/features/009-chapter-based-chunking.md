# Feature: Chapter-Based Chunking

## Feature ID
FRD-009

## PRD Traceability
- **REQ-8**: The server must support an optional parameter to chunk the output by chapter, splitting the Markdown at top-level heading boundaries.
- **REQ-10**: When chunking is enabled, each chapter must be written as a separate Markdown file using the base filename with an underscore and chapter number as suffix (e.g., `report_1.md`, `report_2.md`).
- **Goal**: Chapter-based chunking is available on demand — when the chunking option is enabled, separate Markdown files are written per chapter, named with an underscore-separated chapter number suffix.
- **User Story**: "As a developer dealing with long PDF documents, I want to optionally split the Markdown output by chapter into separate numbered files (e.g., document_1.md, document_2.md), so that I can work with smaller, focused sections of the content independently."

## Description
When the user enables the chunking option, the system must split the converted Markdown content at top-level heading boundaries and write each chapter as a separate numbered file. This allows long documents to be decomposed into manageable sections.

## Inputs
- Converted Markdown content with heading structure (as produced by the conversion pipeline, FRD-003 through FRD-007).
- The `chunkByChapter` flag (set to `true` to enable chunking).
- The source PDF path (used to determine output directory and base filename).

## Outputs
- Multiple Markdown files, one per chapter, written to the same directory as the source PDF.
- File naming convention: `{baseFilename}_{chapterNumber}.md`
  - Chapter numbers are sequential starting from 1.
  - Example: `report.pdf` → `report_1.md`, `report_2.md`, `report_3.md`, ...

## Functional Requirements
1. The system must identify chapter boundaries by locating top-level headings (H1 / `#`) in the converted Markdown content.
2. Content before the first top-level heading (e.g., a title page, preamble, or front matter) must be included in the first chunk (`_1`).
3. Each chunk must start with its top-level heading and include all content up to (but not including) the next top-level heading.
4. Chunks must be numbered sequentially starting from 1.
5. Each chunk must be written as a separate file using the naming convention `{baseFilename}_{N}.md` where `N` is the chapter number.
6. All chunk files must be written to the same directory as the source PDF.
7. If the PDF contains only one top-level heading (or none), chunking must still produce valid output — a single file with the `_1` suffix.
8. Existing files matching the output naming pattern must be overwritten without prompting.
9. The confirmation message must list all chunk file paths written.

## Acceptance Criteria
- [ ] A PDF with 3 top-level headings and `chunkByChapter = true` produces 3 separate Markdown files: `{name}_1.md`, `{name}_2.md`, `{name}_3.md`.
- [ ] Content before the first heading is included in `_1.md`.
- [ ] Each chunk file starts with its respective top-level heading.
- [ ] Chapter numbers are sequential starting from 1.
- [ ] All chunk files are in the same directory as the source PDF.
- [ ] A PDF with no top-level headings and chunking enabled produces a single `_1.md` file containing all content.
- [ ] A PDF with one top-level heading and chunking enabled produces a single `_1.md` file.
- [ ] Existing chunk files are overwritten.
- [ ] The confirmation message lists all chunk file paths.

## Dependencies
- **FRD-004** (Heading Conversion) — provides the heading structure used to identify chapter boundaries.
- **FRD-008** (File Output Management) — handles the actual file writing.

## Notes
- Chapter boundaries are defined exclusively by top-level headings (`#` / H1). Sub-headings (`##`, `###`, etc.) do not create new chunks.
- If a previous chunked conversion produced more files than the current run (e.g., previously 5 chapters, now 3), the leftover files (`_4.md`, `_5.md`) are not automatically deleted. This is by design — the tool overwrites but does not clean up stale files.
