# Task: File Output Cleanup and Error Handling Hardening

## Task ID
019

## Feature
FRD-008 — File Output Management

## Description
Harden the `MarkdownWriter` to fully satisfy all FRD-008 acceptance criteria that are not yet addressed by Task 010. Specifically, this task adds best-effort cleanup of partial output files when writing is interrupted before finalization, wraps file system I/O errors during writing with descriptive `McpException` messages, and adds the missing test coverage for cancellation, encoding verification, and file system error scenarios.

Task 010 delivered the core incremental writing behavior (standard mode, chunked mode, flush-per-page, overwrite, path derivation, `IAsyncDisposable`). This task completes FRD-008 compliance by addressing the remaining behavioral and test gaps.

## Dependencies
- Task 010 (Incremental Markdown Writer) — the writer implementation must be in place.

## Technical Requirements

### Best-effort partial output cleanup

- Track whether `FinalizeAsync` has been called successfully. If `DisposeAsync` is invoked without a prior successful finalization, the writer must attempt to delete any output files it has created during the current session.
- In standard mode, this means deleting the single `.md` file if it exists.
- In chunked mode, this means deleting all chapter `.md` files that were created.
- File deletion must be wrapped in a try/catch — cleanup is best-effort and must never throw, even if the files are locked or already deleted.
- Log a warning when cleanup deletes partial files, and log a warning (not an error) when cleanup fails.
- Do not delete output files when `FinalizeAsync` has been called successfully — only clean up on abnormal disposal (error or cancellation path).

### File system error handling during writes

- Wrap `IOException` and `UnauthorizedAccessException` thrown during `WritePageAsync` and `InitializeAsync` with `McpException` containing a descriptive, user-facing message that includes the file path and the nature of the failure.
- Example messages:
  - `"Cannot write to output file: {path} — {innerMessage}"`
  - `"Cannot create output file (access denied): {path}"`
- Preserve the original exception as the inner exception for diagnostic purposes.
- Do not wrap `OperationCanceledException` — let it propagate naturally.
- The existing directory-existence check in `InitializeAsync` already throws `McpException` — retain that behavior.

### UTF-8 encoding assurance

- Verify the `StreamWriter` is created with `new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)` to produce UTF-8 without BOM, which is the standard for Markdown files and cross-platform compatibility.
- If the current implementation uses `Encoding.UTF8` (which includes BOM), change it to `new UTF8Encoding(false)`.

### Design constraints
- Accept `CancellationToken` on all async methods (already satisfied).
- Use `ConfigureAwait(false)` in library-level code (already satisfied).
- Use `ILogger<T>` for diagnostic logging — log cleanup actions at `Warning` level.
- All public members must retain XML documentation comments.
- Follow AGENTS.md naming and code layout conventions.

## Acceptance Criteria
- [ ] `DisposeAsync` without prior `FinalizeAsync` deletes any partial output files created during the session (best-effort).
- [ ] `DisposeAsync` after a successful `FinalizeAsync` does NOT delete output files.
- [ ] Partial file cleanup is best-effort — exceptions during deletion are caught and logged, never rethrown.
- [ ] A warning is logged when partial files are cleaned up.
- [ ] `IOException` during `WritePageAsync` is wrapped in `McpException` with a descriptive message including the file path.
- [ ] `UnauthorizedAccessException` during `InitializeAsync` (file creation) is wrapped in `McpException` with a descriptive message.
- [ ] `OperationCanceledException` is not wrapped — it propagates naturally.
- [ ] Output files use UTF-8 encoding without BOM.
- [ ] All existing `MarkdownWriterTests` continue to pass without modification.
- [ ] The project builds with zero errors and zero warnings.

## Testing Requirements
- [ ] **Dispose without finalize deletes partial file (standard mode)** — Initialize the writer in standard mode, write content, dispose without calling `FinalizeAsync`, verify the `.md` file has been deleted.
- [ ] **Dispose without finalize deletes chapter files (chunked mode)** — Initialize in chunked mode, write content spanning multiple chapters, dispose without finalizing, verify all chapter `.md` files have been deleted.
- [ ] **Dispose after finalize preserves files** — Initialize, write, finalize, then dispose. Verify the output files still exist on disk.
- [ ] **Cancellation during write triggers cleanup on dispose** — Start writing, cancel via token, verify that disposal cleans up partial files.
- [ ] **File system error wraps in McpException** — Simulate a write failure (e.g., write to a read-only file or closed stream) and verify an `McpException` is thrown with a message containing the file path.
- [ ] **UTF-8 encoding without BOM** — Write content containing non-ASCII characters (e.g., accented characters, emoji), read back the raw bytes, verify no BOM (`EF BB BF`) prefix and correct UTF-8 byte sequences.
- [ ] **Cleanup failure is logged not thrown** — Verify that if file deletion fails during cleanup (e.g., file locked by another process), `DisposeAsync` completes without throwing.
- Minimum test coverage: ≥85% for modified code paths.
