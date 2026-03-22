# Task: CLI Mode Integration Tests

## Task ID
023

## Feature
FRD-011 — CLI Execution Mode

## Description
Write integration tests that verify the CLI mode works end-to-end: argument parsing, conversion execution, stdout/stderr output, and exit codes. These tests complement the MCP integration tests (Task 009) by exercising the CLI entry path — they launch the application process with command-line arguments and verify its behavior from the outside.

## Dependencies
- Task 022 (CLI Entry Point Implementation) — the CLI entry point must be functional.
- Task 005 (Server Integration Smoke Tests) — reuse process-management test infrastructure.
- A valid test PDF fixture in `TestData/` (from Task 014 or earlier).

## Technical Requirements

### Test scope
These are **integration tests** that launch the application as a process with command-line arguments and verify its external behavior (stdout, stderr, exit code, output files). They do not test internal service logic — that is covered by unit tests in other tasks.

### Test scenarios

1. **CLI converts a valid PDF (standard mode)**
   - Run the application with a valid PDF path as argument.
   - Verify the process exits with code `0`.
   - Verify stdout contains the absolute path of the written `.md` file.
   - Verify the `.md` file exists alongside the source PDF.

2. **CLI converts a valid PDF (chunked mode)**
   - Run the application with a valid PDF path and `--chunk-by-chapter`.
   - Verify the process exits with code `0`.
   - Verify stdout contains one or more file paths.
   - Verify the chunk files exist on disk.

3. **CLI reports error for non-existent file**
   - Run the application with a path to a file that does not exist.
   - Verify the process exits with a non-zero exit code.
   - Verify stderr contains a descriptive error message including the file path.
   - Verify stdout is empty (no file paths printed).

4. **CLI reports error for non-PDF file**
   - Run the application with a path to a `.txt` file (or other non-PDF).
   - Verify the process exits with a non-zero exit code.
   - Verify stderr contains an error message about unsupported file type.

5. **CLI prints usage on invalid arguments**
   - Run the application with unrecognized arguments (e.g., `--unknown-flag`).
   - Verify the process exits with a non-zero exit code.
   - Verify stderr contains a usage message.

6. **CLI does not start MCP server**
   - Run the application with a valid PDF path.
   - Verify the process exits on its own within a reasonable timeout (does not block waiting for MCP messages on stdin).

7. **No arguments starts MCP server (not CLI)**
   - Run the application with no arguments.
   - Verify the process blocks (does not exit immediately) — confirming MCP server mode.
   - Shut down the process after verification.

### Test infrastructure
- Reuse `System.Diagnostics.Process` infrastructure from Task 005.
- Set appropriate timeouts to prevent tests from hanging.
- Clean up spawned processes and output files after each test.
- Mark tests with `[Trait("Category", "Integration")]`.
- Use test PDF fixtures from `TestData/`.

### Test naming
- Follow the `MethodName_Condition_ExpectedResult` convention from AGENTS.md.

## Acceptance Criteria
- [ ] All seven test scenarios are implemented and pass.
- [ ] Tests verify exit codes (`0` for success, non-zero for failure).
- [ ] Tests verify stdout contains file paths on success and is empty on failure.
- [ ] Tests verify stderr contains error messages on failure.
- [ ] Tests clean up spawned processes and output files reliably.
- [ ] Tests complete within reasonable time bounds (no indefinite hangs).
- [ ] All tests pass via `dotnet test tests/PdfToMarkdown.Tests`.
- [ ] Test names follow AGENTS.md naming convention.

## Notes
- All tests in this task are themselves the testing deliverable.
- Tests must be runnable independently and as part of the full test suite.
- Tests must not depend on external files outside the `TestData/` directory.
