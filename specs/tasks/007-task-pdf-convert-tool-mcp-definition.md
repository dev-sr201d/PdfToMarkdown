# Task: PdfConvertTool MCP Tool Definition

## Task ID
007

## Feature
FRD-002 — PDF Conversion Tool Interface

## Description
Create the MCP tool class that serves as the entry point for PDF-to-Markdown conversion requests from Copilot Chat. The tool must be discoverable by the MCP client, expose clearly described parameters, and delegate all work to the conversion orchestrator service defined in Task 006. The tool itself must be a thin wrapper with no business logic — it receives the request, passes it to the orchestrator, and returns the result.

## Dependencies
- Task 003 (MCP Server Host Bootstrap) — the server must be running and discovering tools.
- Task 006 (Conversion Pipeline Service Contracts) — service interfaces must exist so the tool can depend on the orchestrator interface.

## Technical Requirements

### Tool class
- Located in `Tools/` directory per AGENTS.md project structure.
- Marked with `[McpServerToolType]` attribute for auto-discovery.
- Contains a single tool method marked with `[McpServerTool]`.
- The tool method must have a `[Description]` attribute providing a clear, LLM-readable description of what the tool does (e.g., purpose, behavior, output).

### Tool parameters
- **`pdfPath`** (required, `string`): Absolute path to the PDF file. Must have a `[Description]` attribute explaining it is an absolute local file system path.
- **`chunkByChapter`** (optional, `bool`, default `false`): When true, output is split into separate files per chapter. Must have a `[Description]` attribute explaining the chunking behavior.
- **`CancellationToken`** (framework-supplied): Supports cancellation of long-running operations.

### Orchestrator delegation
- The tool must accept the conversion orchestrator interface (from Task 006) as a parameter — the MCP SDK resolves DI-registered services automatically when they appear as tool method parameters.
- The tool method must call the orchestrator's pipeline method, passing `pdfPath`, `chunkByChapter`, and `cancellationToken`.
- The tool must return the orchestrator's confirmation message as a `string` (auto-wrapped in `TextContentBlock` by the SDK).

### Error handling
- The tool must NOT catch or suppress exceptions from the orchestrator — the MCP SDK's built-in exception handling (AGENTS.md §3.3) manages error responses.
- `McpException` from the orchestrator surfaces to the client with its message.
- `OperationCanceledException` propagates naturally for cancellation.
- Unexpected exceptions are handled generically by the SDK (no details leaked).

### Relationship to CLI mode
- In CLI mode (FRD-011 / Task 022), the orchestrator is invoked directly from the CLI entry point — bypassing this MCP tool layer entirely.
- The tool class is only relevant in MCP server mode. No changes to the tool are needed to support CLI mode.

### DI registration
- Register the conversion orchestrator interface and a stub/no-op implementation in `Program.cs` so that the tool can be instantiated and invoked. The stub implementation should throw `NotImplementedException` for all methods — it exists only to satisfy DI until real implementations are provided by FRD-003, FRD-008, FRD-009, and FRD-010.
- The tool itself does not need explicit DI registration — `WithToolsFromAssembly()` handles tool discovery.

## Acceptance Criteria
- [ ] The tool class exists in `Tools/` with `[McpServerToolType]` and `[McpServerTool]` attributes.
- [ ] The tool's name and description are visible when querying the MCP server's tool list.
- [ ] `pdfPath` and `chunkByChapter` parameters each have `[Description]` attributes with meaningful, LLM-friendly text.
- [ ] `chunkByChapter` defaults to `false`.
- [ ] The tool delegates entirely to the orchestrator service — no validation, parsing, conversion, or file I/O logic in the tool class.
- [ ] The orchestrator interface is registered in DI with a stub implementation.
- [ ] `dotnet build src/PdfToMarkdown` produces zero errors and zero warnings.
- [ ] The tool is auto-discovered by the MCP server (appears in `tools/list` response).

## Testing Requirements
- **Tool discovery test**: Start the MCP server, send a `tools/list` request, and verify the conversion tool appears with the expected name, description, and parameter schema.
- **Parameter schema test**: Verify that the tool's parameter schema includes `pdfPath` (required, string) and `chunkByChapter` (optional, boolean, default false) with their descriptions.
- **Delegation test**: Unit test that verifies the tool method calls the orchestrator's method with the correct arguments and returns its result unchanged.
- **Error propagation test**: Unit test that verifies exceptions from the orchestrator are not caught or modified by the tool.
- Minimum test coverage for the tool class: all code paths must be exercised.
