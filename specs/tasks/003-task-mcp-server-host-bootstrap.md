# Task: MCP Server Host Bootstrap

## Task ID
003

## Feature
FRD-001 — MCP Server Hosting & Configuration

## Description
Implement the `Program.cs` entry point that bootstraps the MCP server using `Microsoft.Extensions.Hosting` with stdio transport. This is the core runtime shell that listens for MCP JSON-RPC messages on stdin and responds on stdout. All diagnostic logging must be routed to stderr.

## Dependencies
- Task 001 (Solution & Main Project Scaffolding) — project and packages must be in place.

## Technical Requirements

### Hosting pattern
- Use `Host.CreateApplicationBuilder(args)` to create the application builder.
- Register the MCP server with `AddMcpServer()`.
- Attach the stdio transport with `WithStdioServerTransport()`.
- Enable automatic tool discovery with `WithToolsFromAssembly()`.
- Build and run the host with `await builder.Build().RunAsync()`.

### Logging configuration
- Add the console logging provider.
- Set `LogToStandardErrorThreshold` to `LogLevel.Trace` so that **all** log output is written to stderr.
- stdout must contain **only** MCP JSON-RPC protocol messages — no diagnostics, no banners, no startup text.

### Graceful shutdown
- The application must shut down cleanly when:
  - The stdin stream closes (client disconnects).
  - A termination signal is received (e.g., Ctrl+C / SIGTERM).
- No orphaned processes should remain after shutdown.
- The generic host handles this via `CancellationToken` propagation — ensure no code blocks or bypasses this behavior.

### Constraints
- Do **not** reference `ModelContextProtocol.AspNetCore`.
- Do **not** use `WithHttpTransport()` or `MapMcp()`.
- Do **not** bind to any network port.
- Do **not** write to `Console.Out` or `Console.WriteLine` anywhere in the application.

## Acceptance Criteria
- [ ] `Program.cs` follows the hosting pattern specified in AGENTS.md section 3.1.
- [ ] The application starts and enters an MCP-ready state (listening on stdin) without errors.
- [ ] All log output appears on stderr; stdout contains no non-protocol output.
- [ ] The server shuts down cleanly when stdin is closed (no orphaned processes, no unhandled exceptions).
- [ ] The server shuts down cleanly on Ctrl+C / termination signal.
- [ ] `dotnet build src/PdfToMarkdown` produces zero errors and zero warnings.

## Testing Requirements
- **Startup verification test**: Start the server process, confirm it launches without errors and does not immediately exit (it should block waiting for MCP messages).
- **Stderr-only logging test**: Capture stdout and stderr from the running process; verify stdout is empty (or contains only valid JSON-RPC if a handshake is sent) and stderr contains log output.
- **Graceful shutdown test**: Start the server process, close its stdin stream, and verify the process exits with a zero exit code within a reasonable timeout.
- Minimum test coverage: all testable paths in `Program.cs` bootstrap logic must be exercised.
