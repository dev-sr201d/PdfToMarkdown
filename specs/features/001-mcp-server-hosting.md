# Feature: Application Hosting & Mode Selection

## Feature ID
FRD-001

## PRD Traceability
- **REQ-1**: The application must run as a local MCP server that GitHub Copilot Chat in VS Code can discover and communicate with.
- **REQ-1a**: When command-line arguments are provided, the application must operate in CLI mode — performing a one-shot conversion and exiting without starting the MCP server.
- **REQ-1b**: When no command-line arguments are provided, the application must default to MCP server mode.
- **Goal**: Low-friction setup — a developer can configure and start the MCP server locally within 5 minutes following the provided instructions.
- **User Stories**:
  - "As a developer setting up the tool, I want the MCP server to be easy to configure and run locally, so that I can start converting PDFs quickly without complex setup."
  - "As a developer or script author, I want to run the application from the command line with a PDF path, so that I can convert PDFs without needing VS Code or an MCP client."

## Description
The application must support two execution modes selected automatically at startup based on the presence of command-line arguments:

1. **MCP Server mode (default):** When no arguments are provided, the application starts as a local console-based MCP server using stdio transport. It is discoverable and usable by GitHub Copilot Chat in VS Code through the standard MCP server configuration.
2. **CLI mode:** When command-line arguments are provided (a PDF file path and optional flags), the application performs a one-shot conversion using the same conversion pipeline as MCP mode, writes output to disk, and exits with a standard exit code. No MCP server is started.

Both modes share the same conversion pipeline, services, and output behavior — the only difference is how the application is invoked and how results are communicated back to the caller.

## Inputs
- Command-line arguments (presence or absence determines the mode).
  - **No arguments** → MCP server mode.
  - **Arguments present** → CLI mode (see FRD-011 for CLI-specific behavior).
- In MCP server mode: MCP JSON-RPC messages received on stdin from the MCP client (VS Code / Copilot Chat).

## Outputs
- In MCP server mode: MCP JSON-RPC responses written to stdout; diagnostic/log output written to stderr.
- In CLI mode: Confirmation of written file path(s) on stdout; errors/diagnostics on stderr; process exit code.

## Functional Requirements

### Mode Selection
1. The application must inspect command-line arguments at startup to determine the execution mode.
2. If no command-line arguments are provided, the application must enter MCP server mode.
3. If command-line arguments are provided, the application must enter CLI mode (see FRD-011).

### MCP Server Mode
4. In MCP server mode, the application must start as a console process and begin listening for MCP messages on stdin immediately.
5. The application must respond to MCP protocol handshake and capability discovery messages.
6. The application must advertise all registered tools (see FRD-002) during capability discovery.
7. All diagnostic and log output must be written to stderr — stdout is reserved exclusively for MCP protocol messages.
8. The application must shut down gracefully when the MCP client disconnects or the stdin stream closes.
9. A working VS Code MCP configuration example must be provided so that users can set up the server with minimal effort.

## Acceptance Criteria

### Mode Selection
- [ ] Running the application with no arguments starts the MCP server.
- [ ] Running the application with a PDF path argument enters CLI mode (no MCP server started).

### MCP Server Mode
- [ ] The application starts and enters an MCP-ready state within a reasonable time on a standard developer machine.
- [ ] VS Code Copilot Chat can discover the server and list its available tools using the provided configuration.
- [ ] No diagnostic or log output appears on stdout — only valid MCP JSON-RPC messages.
- [ ] The server shuts down cleanly when the client disconnects without orphaned processes.
- [ ] A new developer can follow the setup instructions and have the server running in VS Code within 5 minutes.

## Dependencies
- None (this is the foundational feature).

## Notes
- This feature covers the hosting shell and mode selection. Tool definitions are covered in FRD-002. CLI-specific behavior (argument parsing, exit codes) is covered in FRD-011.
- The MCP server uses stdio transport only — no HTTP, no network ports.
- Both modes reuse the same DI-registered services and conversion pipeline — mode selection only affects the entry-point behavior.
