# Task: PdfConvertTool Integration Tests

## Task ID
009

## Feature
FRD-002 — PDF Conversion Tool Interface

## Description
Write integration tests that verify the PdfConvertTool is correctly wired into the MCP server, is discoverable by clients, exposes the expected parameter schema, and correctly delegates to the conversion orchestrator. These tests complement the unit tests written alongside Tasks 007 and 008 by exercising the full MCP protocol path — from client request through tool discovery and invocation.

## Dependencies
- Task 005 (Server Integration Smoke Tests) — test infrastructure for launching the MCP server process must be in place.
- Task 007 (PdfConvertTool MCP Tool Definition) — the tool must be registered and discoverable.
- Task 008 (Conversion Pipeline Orchestrator) — the orchestrator must be wired so the tool can be invoked end-to-end.

## Technical Requirements

### Test scope
These tests exercise the MCP protocol layer for tool discovery and invocation. They verify that the tool is correctly registered, described, and callable through the MCP server's JSON-RPC interface.

### Test scenarios

1. **Tool discovery**
   - Send a `tools/list` request to the MCP server.
   - Verify the response includes the PDF conversion tool.
   - Verify the tool has a non-empty name and description.

2. **Parameter schema correctness**
   - From the `tools/list` response, inspect the tool's input schema.
   - Verify `pdfPath` is listed as a required parameter of type `string` with a non-empty description.
   - Verify `chunkByChapter` is listed as an optional parameter of type `boolean` with a default value of `false` and a non-empty description.
   - Verify no unexpected required parameters are present.

3. **Tool invocation with invalid path**
   - Invoke the tool via a `tools/call` request with a `pdfPath` that does not exist on the file system.
   - Verify the response contains an error result with a descriptive, user-facing message (not a stack trace or generic error).
   - This validates the error reporting pipeline from tool through orchestrator through validator.

4. **Tool invocation returns confirmation on success**
   - This test may require a minimal test PDF fixture in `TestData/` or may be deferred to FRD-003/FRD-008 integration tests if service implementations do not yet exist.
   - If stub implementations are in place (from Task 007), verify that invoking the tool produces the expected stub behavior (e.g., `NotImplementedException` is handled gracefully by the MCP SDK).

### Test infrastructure
- Reuse the server process management infrastructure from Task 005 (server fixture, process launch, stdin/stdout interaction).
- Use the MCP SDK's client library if available, or raw JSON-RPC messages over stdin/stdout.
- Set appropriate timeouts and clean up server processes after each test.
- Mark tests with `[Trait("Category", "Integration")]`.

### Test naming
- Follow the `MethodName_Condition_ExpectedResult` convention from AGENTS.md.

## Acceptance Criteria
- [ ] Tool discovery test verifies the tool appears in `tools/list` with name and description.
- [ ] Parameter schema test verifies `pdfPath` (required, string) and `chunkByChapter` (optional, boolean, default false) with descriptions.
- [ ] Invalid path invocation test verifies a descriptive error message is returned, not a stack trace.
- [ ] All tests pass via `dotnet test tests/PdfToMarkdown.Tests`.
- [ ] Tests clean up server processes reliably (no orphaned processes).
- [ ] Test names follow AGENTS.md naming convention.

## Notes
- All tests in this task are themselves the testing deliverable.
- Tests must be runnable independently and as part of the full test suite.
- Tests must not depend on external files outside the `TestData/` directory.
- CLI mode integration tests are covered separately in Task 023. This task covers only MCP tool integration.
