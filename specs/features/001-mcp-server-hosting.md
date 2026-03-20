# Feature: MCP Server Hosting & Configuration

## Feature ID
FRD-001

## PRD Traceability
- **REQ-1**: The application must run as a local MCP server that GitHub Copilot Chat in VS Code can discover and communicate with.
- **Goal**: Low-friction setup — a developer can configure and start the MCP server locally within 5 minutes following the provided instructions.
- **User Story**: "As a developer setting up the tool, I want the MCP server to be easy to configure and run locally, so that I can start converting PDFs quickly without complex setup."

## Description
The application must operate as a local console-based MCP server using stdio transport. It must be discoverable and usable by GitHub Copilot Chat in VS Code through the standard MCP server configuration. The server is the foundational runtime shell that hosts all tool capabilities.

## Inputs
- Command-line invocation (e.g., `dotnet run --project src/PdfToMarkdown`).
- MCP JSON-RPC messages received on stdin from the MCP client (VS Code / Copilot Chat).

## Outputs
- MCP JSON-RPC responses written to stdout.
- Diagnostic/log output written to stderr.

## Functional Requirements
1. The application must start as a console process and begin listening for MCP messages on stdin immediately.
2. The application must respond to MCP protocol handshake and capability discovery messages.
3. The application must advertise all registered tools (see FRD-002) during capability discovery.
4. All diagnostic and log output must be written to stderr — stdout is reserved exclusively for MCP protocol messages.
5. The application must shut down gracefully when the MCP client disconnects or the stdin stream closes.
6. A working VS Code MCP configuration example must be provided so that users can set up the server with minimal effort.

## Acceptance Criteria
- [ ] The application starts and enters an MCP-ready state within a reasonable time on a standard developer machine.
- [ ] VS Code Copilot Chat can discover the server and list its available tools using the provided configuration.
- [ ] No diagnostic or log output appears on stdout — only valid MCP JSON-RPC messages.
- [ ] The server shuts down cleanly when the client disconnects without orphaned processes.
- [ ] A new developer can follow the setup instructions and have the server running in VS Code within 5 minutes.

## Dependencies
- None (this is the foundational feature).

## Notes
- This feature covers only the hosting shell. Tool definitions are covered in FRD-002.
- The server uses stdio transport only — no HTTP, no network ports.
