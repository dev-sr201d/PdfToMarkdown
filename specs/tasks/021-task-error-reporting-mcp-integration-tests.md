# Task: Error Reporting MCP Protocol Integration Tests

## Task ID
021

## Feature
FRD-010 — Input Validation & Error Reporting

## Description
Create MCP protocol-level integration tests that verify all FRD-010 error scenarios return properly formatted error results through the JSON-RPC protocol. These tests start the MCP server as a child process, send `tools/call` requests with invalid or problematic inputs, and assert that the response conforms to the MCP error result format — specifically that `isError` is `true`, the error text is human-readable, and no internal details (stack traces, exception types) are leaked to the client.

Task 009 (PdfConvertTool Integration Tests) delivered one protocol-level error test (`ToolsCall_WithNonExistentPath_ReturnsErrorResult`). This task extends that coverage to all remaining FRD-010 error scenarios at the protocol level.

## Dependencies
- Task 005 (Server Integration Smoke Tests) — the `ServerFixture` test infrastructure must be in place.
- Task 009 (PdfConvertTool Integration Tests) — the MCP protocol test helpers and patterns must be established.
- Task 020 (Error Reporting Pipeline Tests) — pipeline-level error behavior must be verified first.

## Technical Requirements

### MCP protocol error result format
The MCP SDK wraps `McpException` errors as tool results with `isError: true` and the exception message in a `text` content block. Tests must verify:
1. The response contains `result.isError = true`.
2. The response contains `result.content` with at least one element.
3. The first content element's `text` property contains a human-readable error message.
4. The error text does NOT contain stack traces, `.cs` file references, or .NET exception type names.

### Error scenarios to test at protocol level

1. **Empty / missing `pdfPath`** — Call the tool with an empty string for `pdfPath`. Expect an error result mentioning that no path was provided.

2. **Wrong file extension** — Call the tool with a path to a `.txt` file that exists on disk. Expect an error result mentioning `.pdf` extension requirement.

3. **Non-existent file** — Call the tool with a path that does not exist. Expect an error result including the path and stating the file was not found. (This extends the existing test with stronger message quality assertions.)

4. **No-text PDF** — Call the tool with a path to a no-text PDF fixture. Expect an error result mentioning "no extractable text".

5. **Unexpected exception safety** — If feasible, trigger an unexpected (non-`McpException`) error through the tool and verify the response does NOT contain internal details. This may require a specially crafted input or a test-only hook. If not feasible without modifying production code, document the limitation and rely on the MCP SDK's documented behavior.

### Test infrastructure
- Reuse the existing `ServerFixture` and MCP protocol helpers from `PdfConvertToolIntegrationTests`.
- Each test must start an independent server process to avoid shared state.
- For tests requiring fixture files (wrong extension, no-text PDF), write test files to a temp directory before calling the tool, and clean them up after the test.
- All tool calls must use `chunkByChapter: false` or omit the parameter entirely — no dependency on FRD-009.

### Error message quality assertions (at protocol level)
For each error response text, assert:
- The message describes the specific error (not a generic "An error occurred").
- The message includes the relevant file path where applicable.
- The message does NOT contain: "StackTrace", "at System.", "at PdfToMarkdown.", "Exception", or `.cs:line`.

### Test organization
- Place tests in a new test class `ErrorReportingIntegrationTests` in the test project.
- Mark all tests with `[Trait("Category", "Integration")]`.
- Use the same JSON-RPC communication pattern as `PdfConvertToolIntegrationTests`.

### Scope boundaries
- This task tests ONLY the MCP protocol-level error result format.
- This task does NOT test error behavior at the service/orchestrator level — that is covered by Task 020.
- This task does NOT test chunked mode errors — those belong in FRD-009 tasks.
- This task does NOT modify any production code — it is a test-only task.

## Acceptance Criteria
- [ ] An empty `pdfPath` produces an MCP error result (`isError: true`) with a message about a missing path.
- [ ] A `.txt` file path produces an MCP error result with a message mentioning `.pdf`.
- [ ] A non-existent `.pdf` path produces an MCP error result with the path in the message and "not found".
- [ ] A no-text PDF produces an MCP error result with "no extractable text" in the message.
- [ ] No MCP error result text contains stack traces, exception type names, or `.cs` file references.
- [ ] All MCP error results have `isError: true` and at least one content block.
- [ ] All tests use standard mode (no `chunkByChapter` or `chunkByChapter: false`).
- [ ] All existing tests continue to pass — no regressions.
- [ ] The project builds with zero errors and zero warnings.

## Testing Requirements
- [ ] **Empty path at MCP level** — Tool call with `pdfPath: ""` returns `isError: true` with a message about missing path.
- [ ] **Wrong extension at MCP level** — Tool call with a `.txt` path returns `isError: true` with a message about `.pdf` extension.
- [ ] **Non-existent file at MCP level** — Tool call with a non-existent path returns `isError: true` with the path and "not found" in the message.
- [ ] **No-text PDF at MCP level** — Tool call with a no-text PDF returns `isError: true` with "no extractable text" in the message.
- [ ] **No stack traces in any error** — All error response texts are asserted against a helper that checks for stack trace patterns.
- [ ] **Unexpected exception safety** — If testable, an unhandled exception does not leak internals. If not testable, document why.
- Minimum test coverage: ≥85% for new test code paths.
