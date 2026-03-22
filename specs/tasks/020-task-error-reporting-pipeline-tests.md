# Task: Error Reporting Pipeline Tests

## Task ID
020

## Feature
FRD-010 — Input Validation & Error Reporting

## Description
Create comprehensive unit and orchestrator-level tests that verify every FRD-010 error scenario produces the correct `McpException` with a well-formed, user-facing error message. These tests exercise the full conversion pipeline (validator → converter → writer) through the `ConversionOrchestrator` in standard mode (`chunkByChapter = false`), ensuring no dependency on FRD-009 (chapter-based chunking).

Task 012 delivered the `InputValidator` with unit tests covering input validation checks (empty path, wrong extension, non-existent file, unreadable file). This task extends coverage to **runtime error scenarios** detected by the converter and writer, and adds **error message quality assertions** across all error paths.

## Dependencies
- Task 008 (Conversion Pipeline Orchestrator) — the orchestrator and its pipeline must be in place.
- Task 011 (PDF Direct Markdown Converter) — the converter error paths must be implemented.
- Task 012 (Input Validator Implementation) — the validator must be in place.

## Technical Requirements

### Encrypted / password-protected PDF test
- Include a pre-built encrypted PDF test fixture in `tests/PdfToMarkdown.Tests/TestData/` (e.g., `encrypted.pdf`). If generating a fixture programmatically is not feasible with PdfPig, include a minimal manually-created encrypted PDF binary or create a helper that produces a file whose opening causes PdfPig to throw an exception containing "password" or "encrypt" in the message.
- Tests must verify that the converter wraps this exception in an `McpException` with a message stating the file is encrypted or password-protected.
- The error message must include the file path.

### No-text-content PDF test (orchestrator level)
- Verify the orchestrator-level path: calling `ConvertAsync` with a PDF that has no extractable text produces an `McpException` whose message mentions "no extractable text" and includes the file path.
- This supplements the existing converter-level test by exercising the full pipeline.

### Write failure test
- Simulate a write failure by pointing the output directory to a read-only location or creating conditions where the `MarkdownWriter` cannot create the output file.
- Verify the resulting `McpException` includes the output file path and a description of the failure.
- Platform considerations: on Windows, test by setting a directory to read-only attributes or using a path with invalid characters that the writer cannot create. If platform-reliable simulation is impractical, document the limitation and test at the writer unit level by mocking the stream.

### Error message quality assertions
- For every error scenario tested, assert the following message properties:
  1. **Includes file path** — The error message must contain the path of the file that caused the error.
  2. **Is actionable** — The message describes what went wrong (not a generic "error occurred" message).
  3. **No stack traces** — The message must not contain the strings "StackTrace", "at System.", "at PdfToMarkdown.", or " in " followed by a `.cs` file reference.
  4. **No exception type names** — The message must not contain strings like "IOException", "UnauthorizedAccessException", "NullReferenceException", or other .NET exception type names.

### Test organization
- All tests must use `chunkByChapter: false` — they must not exercise or depend on chunking functionality (FRD-009).
- Place tests in a new test class `ErrorReportingPipelineTests` in the test project.
- Use `IDisposable` or `IAsyncLifetime` for test fixture setup/teardown (temp directories, test PDFs).
- Use `FluentAssertions` for all assertions.
- Mark tests that require real file system interaction with `[Trait("Category", "Integration")]`.

### Scope boundaries
- This task does NOT test MCP protocol-level error formatting — that is covered by Task 021.
- This task does NOT test chunked mode error scenarios — those belong in FRD-009 tasks.
- This task does NOT modify any production code — it is a test-only task.

## Acceptance Criteria
- [ ] An encrypted or password-protected PDF produces `McpException` with message containing "encrypted" or "password-protected" and the file path.
- [ ] A PDF with no extractable text produces `McpException` through the full orchestrator pipeline with message containing "no extractable text" and the file path.
- [ ] A write failure (read-only directory or equivalent) produces `McpException` with a message containing the output file path and a description of the failure.
- [ ] All error messages across all test scenarios include the relevant file path.
- [ ] No error messages contain stack traces, internal exception type names, or `.cs` file references.
- [ ] All error messages are specific and actionable (not generic "an error occurred" messages).
- [ ] All tests use `chunkByChapter: false` and do not depend on FRD-009.
- [ ] All existing tests continue to pass.
- [ ] The project builds with zero errors and zero warnings.

## Testing Requirements
- [ ] **Encrypted PDF** — Calling `ConvertAsync` with an encrypted PDF throws `McpException` whose message contains "encrypted" or "password-protected" and includes the file path.
- [ ] **No-text PDF through orchestrator** — Calling orchestrator `ConvertAsync` with a no-text PDF throws `McpException` whose message contains "no extractable text" and includes the file path.
- [ ] **Write failure** — A configuration where the writer cannot create the output file produces `McpException` with a message containing the output path and a failure description.
- [ ] **Empty path through orchestrator** — Calling orchestrator `ConvertAsync` with null/empty path throws `McpException` with a message about missing path.
- [ ] **Wrong extension through orchestrator** — Calling orchestrator `ConvertAsync` with a `.txt` file throws `McpException` mentioning `.pdf`.
- [ ] **Non-existent file through orchestrator** — Calling orchestrator `ConvertAsync` with a non-existent `.pdf` path throws `McpException` with the path in the message.
- [ ] **Error message quality (all scenarios)** — Each scenario's error message is asserted for: (a) file path inclusion, (b) actionable description, (c) no stack traces, (d) no exception type names.
- [ ] **OperationCanceledException propagates** — Cancelling the orchestrator's `ConvertAsync` via token throws `OperationCanceledException` (not wrapped in `McpException`).
- Minimum test coverage: ≥85% for error-path code.
