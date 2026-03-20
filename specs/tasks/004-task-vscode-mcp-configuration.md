# Task: VS Code MCP Configuration

## Task ID
004

## Feature
FRD-001 — MCP Server Hosting & Configuration

## Description
Provide a working VS Code MCP server configuration file so that developers can discover and use the PdfToMarkdown server from GitHub Copilot Chat with minimal setup effort. This directly supports the "5-minute setup" goal from the PRD and the acceptance criterion in FRD-001.

## Dependencies
- Task 003 (MCP Server Host Bootstrap) — the server must be runnable before the configuration can be verified.

## Technical Requirements

### Configuration file
- Create `.vscode/mcp.json` in the repository root.
- The configuration must register the server with:
  - Server name: `pdf-to-markdown`
  - Type: `stdio`
  - Command: `dotnet`
  - Args: `["run", "--project", "src/PdfToMarkdown"]`

### Configuration format
- Use standard JSON (JSONC comments are acceptable for documentation within the file).
- Follow the VS Code MCP configuration schema as documented in AGENTS.md section 3.5.

### Documentation
- Include a brief inline comment in the JSON file explaining the server's purpose.
- The project README (if one exists) or the configuration file itself should be self-explanatory enough for a developer to understand what it does.

## Acceptance Criteria
- [ ] `.vscode/mcp.json` exists at the repository root with the correct server configuration.
- [ ] The configuration uses stdio transport — no HTTP URLs or port bindings.
- [ ] A developer can open the project in VS Code, and the MCP server appears in the Copilot Chat server list without additional manual configuration.
- [ ] The server starts successfully when invoked through the VS Code MCP configuration.

## Testing Requirements
- **Manual verification**: Open the project in VS Code with Copilot Chat, confirm the server is discovered and its tools are listed.
- **Configuration validation**: Verify the JSON is well-formed and matches the expected schema structure.
- No automated unit tests required for a static configuration file.
