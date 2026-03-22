# Task: Application Host Bootstrap & Mode Selection

## Task ID
003

## Feature
FRD-001 — Application Hosting & Mode Selection

## Description
Implement the `Program.cs` entry point that inspects command-line arguments to determine the execution mode. When no arguments are provided, the application bootstraps the MCP server using `Microsoft.Extensions.Hosting` with stdio transport (existing behavior). When arguments are provided, the application enters CLI mode — this task only adds the mode-selection branch and defers CLI-specific behavior to Task 022.

All diagnostic logging must be routed to stderr in both modes.

## Dependencies
- Task 001 (Solution & Main Project Scaffolding) — project and packages must be in place.

## Technical Requirements

### Mode selection
- At the top of `Program.cs`, inspect `args` to determine the execution mode.
- **No arguments** (`args.Length == 0`) → proceed with MCP server bootstrap (existing behavior).
- **Arguments present** (`args.Length > 0`) → enter CLI mode branch (Task 022 implements the CLI logic; this task only needs to add the branching structure).

### MCP server hosting pattern (no-args path)
- Use `Host.CreateApplicationBuilder(args)` to create the application builder.
- Register the MCP server with `AddMcpServer()`.
- Attach the stdio transport with `WithStdioServerTransport()`.
- Enable automatic tool discovery with `WithToolsFromAssembly()`.
- Build and run the host with `await builder.Build().RunAsync()`.

### Logging configuration
- Add the console logging provider.
- Set `LogToStandardErrorThreshold` to `LogLevel.Trace` so that **all** log output is written to stderr.
- stdout must contain **only** MCP JSON-RPC protocol messages (in MCP mode) or result file paths (in CLI mode) — no diagnostics, no banners, no startup text.
- Logging configuration must apply to both modes (shared DI container setup).

### Graceful shutdown (MCP mode)
- The application must shut down cleanly when:
  - The stdin stream closes (client disconnects).
  - A termination signal is received (e.g., Ctrl+C / SIGTERM).
- No orphaned processes should remain after shutdown.
- The generic host handles this via `CancellationToken` propagation — ensure no code blocks or bypasses this behavior.

### Constraints
- Do **not** reference `ModelContextProtocol.AspNetCore`.
- Do **not** use `WithHttpTransport()` or `MapMcp()`.
- Do **not** bind to any network port.
- Do **not** write to `Console.Out` or `Console.WriteLine` anywhere in the MCP server path.
- MCP server registration (`AddMcpServer`, `WithStdioServerTransport`, `WithToolsFromAssembly`) must only execute on the MCP path — not on the CLI path.

## Acceptance Criteria
- [ ] `Program.cs` branches on `args` presence: no args → MCP server, args → CLI path.
- [ ] The MCP server path follows the hosting pattern specified in AGENTS.md section 3.1.
- [ ] The application starts and enters an MCP-ready state (listening on stdin) when run with no arguments.
- [ ] When arguments are provided, the MCP server is NOT started.
- [ ] All log output appears on stderr; stdout contains no non-protocol output in MCP mode.
- [ ] The server shuts down cleanly when stdin is closed (no orphaned processes, no unhandled exceptions).
- [ ] The server shuts down cleanly on Ctrl+C / termination signal.
- [ ] Service registrations (orchestrator, validator, writer, converter) are shared between both modes.
- [ ] `dotnet build src/PdfToMarkdown` produces zero errors and zero warnings.

## Testing Requirements
- **Startup verification test**: Start the server process with no args, confirm it launches without errors and does not immediately exit (it should block waiting for MCP messages).
- **Stderr-only logging test**: Capture stdout and stderr from the running process (no args); verify stdout is empty (or contains only valid JSON-RPC if a handshake is sent) and stderr contains log output.
- **Graceful shutdown test**: Start the server process (no args), close its stdin stream, and verify the process exits with a zero exit code within a reasonable timeout.
- **Mode selection test**: Start the process with a dummy argument, verify the process does NOT enter MCP server mode (exits promptly or runs CLI logic instead of blocking on stdin).
- Minimum test coverage: all testable paths in `Program.cs` bootstrap logic must be exercised.
