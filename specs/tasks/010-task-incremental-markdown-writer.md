# Task: Incremental Markdown Writer

## Task ID
010

## Supersedes
Task 010 (Document Model Type Hierarchy) — the intermediate document model is no longer part of the architecture. The full type hierarchy (`DocumentModel`, `PageModel`, block types, `TextSegment`, `FontMetadata`, etc.) is removed by Task 006.

## Feature
FRD-008 — File Output Management
FRD-009 — Chapter-Based Chunking

## Description
Implement the Markdown writer service that handles incremental output of converted Markdown content to disk. The writer supports two modes: standard mode (single output file) and chunked mode (multiple files split at chapter boundaries). The writer receives Markdown content page-by-page from the converter and flushes it to disk incrementally — it does not accumulate the entire document's content in memory.

## Dependencies
- Task 006 (Conversion Pipeline Service Contracts) — the writer interface must be defined.

## Technical Requirements

### Standard mode (single file)
- Given a source PDF path, derive the output file path by replacing the `.pdf` extension with `.md` in the same directory (e.g., `report.pdf` → `report.md`).
- Open the output file for writing when the writer is initialized.
- Accept Markdown content incrementally (page-by-page). Each call appends the content to the output file and flushes.
- On finalization, close the file and return the single output file path.
- Overwrite the output file if it already exists.

### Chunked mode (chapter-based splitting)
- Given a source PDF path, derive output file paths using the base filename with chapter number suffixes (e.g., `report_1.md`, `report_2.md`).
- Detect chapter boundaries in the incoming Markdown content: a chapter boundary occurs when a line starts with a top-level heading marker (`# ` — a single `#` followed by a space, at the start of a line).
- Content before the first chapter heading goes into the first file (chapter 1).
- Each subsequent chapter heading starts a new file with the next chapter number.
- When a chapter boundary is detected mid-page, split the content: everything before the boundary goes to the current chapter file, and the boundary line and everything after goes to the next chapter file.
- On finalization, close all open files and return the list of all chapter file paths in order.
- Overwrite existing chapter files if they already exist.

### Incremental writing guarantees
- Each page's Markdown content must be flushed to disk after each write call. The writer must not buffer multiple pages in memory.
- The writer must not require holding the full document's converted content in memory at any point.

### Resource management
- The writer must implement `IAsyncDisposable` for proper cleanup of file handles.
- All file handles must be closed on disposal, even if finalization was not called (handles error/cancellation scenarios).
- Use async file I/O APIs.

### Error handling
- If the output directory is not writable, throw an `McpException` with a descriptive message including the directory path.
- If file I/O fails during writing, allow the exception to propagate to the orchestrator.

### DI registration
- Register the writer implementation in the DI container, replacing the stub from Task 006.
- The writer may need to be registered as `Transient` since each conversion creates a new writer instance with its own state and file handles.

### Design constraints
- Accept `CancellationToken` on all async methods.
- Use `ConfigureAwait(false)` in library-level code.
- Use `ILogger<T>` for diagnostic logging (log file creation, chapter splits, finalization).
- All public members must have XML documentation comments.
- Follow AGENTS.md naming and code layout conventions.

## Acceptance Criteria
- [ ] The writer implements the writer interface from Task 006 and `IAsyncDisposable`.
- [ ] Standard mode writes a single `.md` file alongside the source PDF with the correct derived filename.
- [ ] Chunked mode splits output at top-level heading boundaries into separate numbered files.
- [ ] Chapter boundaries mid-page are handled correctly — content is split at the heading line.
- [ ] Content before the first chapter heading is included in the first chapter file.
- [ ] Markdown content is flushed to disk after each page write call — not accumulated in memory.
- [ ] The writer returns the correct list of absolute file paths on finalization.
- [ ] Existing output files are overwritten without error.
- [ ] The writer properly disposes file handles, even in error/cancellation scenarios without finalization.
- [ ] The writer is registered in DI, replacing the stub implementation.
- [ ] All public members have XML documentation comments.
- [ ] The project builds with zero errors and zero warnings.

## Testing Requirements
- [ ] **Standard mode: single page** — Write one page of Markdown, finalize, verify the `.md` file exists alongside the PDF path with correct content.
- [ ] **Standard mode: multi-page** — Write multiple pages incrementally, verify all content appears in the single output file in order.
- [ ] **Standard mode: overwrite** — Write to a path where a `.md` file already exists, verify it is overwritten with new content.
- [ ] **Standard mode: path derivation** — Verify the output path is correctly derived from the PDF path (same directory, same base name, `.md` extension).
- [ ] **Chunked mode: single chapter** — Write content with no H1 markers, verify a single `_1.md` file is produced.
- [ ] **Chunked mode: multiple chapters** — Write content with multiple H1 markers, verify separate numbered files are created with correct content boundaries.
- [ ] **Chunked mode: H1 mid-page** — Write a single page that contains an H1 marker in the middle, verify the content is split correctly between chapter files.
- [ ] **Chunked mode: content before first H1** — Write content where the first H1 does not appear at the start, verify pre-heading content goes into chapter 1.
- [ ] **Chunked mode: path derivation** — Verify correct `_N.md` paths for chunked mode.
- [ ] **Disposal without finalization** — Dispose the writer without calling finalize, verify no resource leaks (no file handle exceptions on process exit).
- [ ] **Cancellation** — Cancel during a write operation, verify the writer disposes cleanly.
- [ ] **Flush verification** — Verify content is flushed to disk after each write call (readable by another process/stream before finalization).
- Minimum test coverage: ≥85%.
