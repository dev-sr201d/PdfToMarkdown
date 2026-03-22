# Task: Server Integration Smoke Tests

## Task ID
005

## Feature
FRD-001 — Application Hosting & Mode Selection

## Description
Write integration tests that verify the MCP server starts correctly, responds to protocol messages, and shuts down gracefully. These tests exercise the full hosting pipeline end-to-end and confirm all FRD-001 acceptance criteria are met programmatically.

## Dependencies
- Task 002 (Test Project Scaffolding) — test project must exist.
- Task 003 (MCP Server Host Bootstrap) — the server must be functional.

## Technical Requirements

### Test scope
These are **integration tests** that launch the actual server process and interact with it over stdin/stdout. They verify the hosting infrastructure, not individual tool behavior.

### Test scenarios

1. **Server starts successfully (no args → MCP mode)**
   - Launch the server process via `dotnet run --project src/PdfToMarkdown` (no extra arguments).
   - Verify the process starts without crashing and remains running (does not exit immediately).
   - Verify stderr contains startup log output.
   - Shut down the process after verification.

2. **Stdout contains no diagnostic output (MCP mode)**
   - Launch the server process with no extra arguments.
   - Without sending any MCP messages, capture stdout for a brief period.
   - Verify stdout is empty (no banners, no log lines, no diagnostic text).

3. **Graceful shutdown on stdin close**
   - Launch the server process with no extra arguments.
   - Close the stdin stream of the process.
   - Verify the process exits within a reasonable timeout (e.g., 10 seconds).
   - Verify the exit code is zero (clean shutdown).

4. **MCP protocol handshake**
   - Launch the server process with no extra arguments.
   - Send a valid MCP `initialize` request on stdin.
   - Read the response from stdout.
   - Verify the response is a valid JSON-RPC response containing server capabilities.
   - Shut down the process after verification.

5. **Mode selection — args present skips MCP server**
   - Launch the server process with a dummy argument (e.g., `dotnet run --project src/PdfToMarkdown -- dummy.pdf`).
   - Verify the process does NOT block waiting for MCP messages on stdin — it should exit on its own (success or error) within a reasonable timeout.
   - Verify no MCP handshake is possible (process is not an MCP server).

### Test infrastructure
- Use `System.Diagnostics.Process` (or a suitable wrapper) to launch and manage the server process.
- Set appropriate timeouts to prevent tests from hanging indefinitely.
- Ensure tests clean up all spawned processes, even on failure.
- Tests should be marked with a trait or category (e.g., `[Trait("Category", "Integration")]`) to allow selective execution.

## Acceptance Criteria
- [ ] All five test scenarios are implemented and pass.
- [ ] Tests clean up spawned processes reliably (no orphaned processes after test runs).
- [ ] Tests complete within reasonable time bounds (no indefinite hangs).
- [ ] Tests can be run via `dotnet test tests/PdfToMarkdown.Tests`.
- [ ] Test names follow the `MethodName_Condition_ExpectedResult` convention from AGENTS.md.

## Testing Requirements
- All tests in this task are themselves the testing deliverable.
- Minimum coverage target: all FRD-001 acceptance criteria must have at least one corresponding test.
- Tests must be deterministic and not depend on external network resources.
