# Task: CLI Entry Point Implementation

## Task ID
022

## Feature
FRD-011 — CLI Execution Mode

## Description
Implement the CLI mode entry point in `Program.cs` (or a dedicated helper invoked from `Program.cs`). When command-line arguments are present, the application must parse them, resolve the conversion orchestrator from DI, invoke the conversion pipeline, print the resulting file paths to stdout, and exit with a standard exit code. No MCP server is started.

## Dependencies
- Task 003 (Application Host Bootstrap & Mode Selection) — the mode-selection branch must exist in `Program.cs`.
- Task 006 (Conversion Pipeline Service Contracts) — the orchestrator interface must be defined.
- Task 008 (Conversion Pipeline Orchestrator) — the orchestrator implementation must be available (or stubs for initial development).

## Technical Requirements

### Argument parsing
- The CLI accepts a **positional argument** for the PDF file path (required).
- The CLI accepts an optional `--chunk-by-chapter` flag (default: off).
- If unrecognized arguments or missing required arguments are detected, print a usage message to stderr and exit with a non-zero exit code.
- The usage message should be brief and show the expected syntax, e.g.:
  ```
  Usage: PdfToMarkdown <pdfPath> [--chunk-by-chapter]
  ```

### DI and service resolution
- Build the DI container using the same service registrations as MCP mode (shared `IServiceCollection` setup).
- Do **not** register MCP server services (`AddMcpServer`, `WithStdioServerTransport`, `WithToolsFromAssembly`) on the CLI path — these are MCP-only.
- Resolve the `IConversionOrchestrator` from the built service provider.

### Conversion invocation
- Call the orchestrator's conversion method with the parsed PDF path, the `chunkByChapter` flag, and a `CancellationToken` (wired to Ctrl+C / SIGTERM for graceful cancellation).
- The orchestrator returns a confirmation message — extract the file paths from it or use the same message for stdout output.

### Output
- On success: print the absolute path(s) of all written Markdown files to **stdout**, one per line.
- On success: exit with code `0`.
- On failure (validation error, conversion error, I/O error): print the error message to **stderr** and exit with a non-zero exit code (e.g., `1`).
- Diagnostic/log output from `ILogger` must go to **stderr** (same logging configuration as MCP mode).

### Error handling
- Catch `McpException` from the orchestrator and print its `Message` to stderr (these are user-facing errors).
- Catch `OperationCanceledException` and exit with a non-zero code (e.g., `130` for SIGINT convention, or `1`).
- Catch unexpected exceptions, print a safe generic message to stderr (no stack traces), and exit with a non-zero code.

### Constraints
- Do **not** write to `Console.Out` using `Console.WriteLine` for logging — only for the final file path output on success.
- All logging must use `ILogger<T>` routed to stderr.
- Keep the CLI entry point thin — it parses args, resolves the orchestrator, invokes it, and translates the result to stdout/exit code.

## Acceptance Criteria
- [ ] Running `dotnet run --project src/PdfToMarkdown -- report.pdf` performs the conversion and prints the output file path to stdout.
- [ ] Running with `--chunk-by-chapter` produces multiple files and prints all paths to stdout.
- [ ] On success, the process exits with code `0`.
- [ ] On a validation error (e.g., file not found), the error is printed to stderr and the process exits with a non-zero code.
- [ ] On an unexpected error, a generic message is printed to stderr (no stack trace) and the process exits with a non-zero code.
- [ ] Running with unrecognized arguments prints a usage message to stderr and exits with a non-zero code.
- [ ] No MCP server is started when arguments are present.
- [ ] The DI container is built with shared service registrations (same validator, converter, writer, orchestrator as MCP mode).
- [ ] `dotnet build src/PdfToMarkdown` produces zero errors and zero warnings.

## Testing Requirements
- **Argument parsing tests**: Unit tests for the argument parsing logic — valid args, missing PDF path, unrecognized flags, `--chunk-by-chapter` flag present/absent.
- **Success exit code test**: Integration test that runs the CLI with a valid PDF and verifies exit code `0`.
- **Error exit code test**: Integration test that runs the CLI with an invalid path and verifies non-zero exit code.
- **Stdout output test**: Integration test that verifies file paths appear on stdout (not stderr) on success.
- **Stderr output test**: Integration test that verifies error messages appear on stderr (not stdout) on failure.
- **Usage message test**: Integration test that runs with invalid args and verifies the usage message appears on stderr.
- Minimum test coverage: all branching paths in the CLI entry point must be exercised.
