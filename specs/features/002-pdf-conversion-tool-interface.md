# Feature: PDF Conversion Tool Interface

## Feature ID
FRD-002

## PRD Traceability
- **REQ-2**: The server must expose a tool that accepts a local file system path to a PDF file.
- **REQ-11**: The server must return a confirmation message to the caller indicating the output file path(s) written.
- **User Story**: "As a developer using VS Code, I want to provide a path to a PDF file through Copilot Chat, so that a Markdown version of the document is saved alongside the original PDF."

## Description
The MCP server must expose a tool callable from Copilot Chat that accepts a PDF file path and an optional chunking parameter, orchestrates the conversion pipeline, and returns a confirmation message listing the files written. This tool serves as the entry point for all conversion operations.

## Inputs
- **pdfPath** (required): An absolute local file system path to the PDF file to convert.
- **chunkByChapter** (optional, default: false): When true, the output is split into separate Markdown files per chapter.

## Outputs
- A confirmation message (text) listing the absolute path(s) of all Markdown files written to disk.

## Functional Requirements
1. The tool must be discoverable by the MCP client with a clear name and description that conveys its purpose.
2. Each parameter must have a human-readable description so that the LLM can understand how to use the tool.
3. The tool must accept a `pdfPath` parameter representing an absolute path to a PDF file on the local file system.
4. The tool must accept an optional `chunkByChapter` boolean parameter (default: `false`).
5. The tool must orchestrate the conversion pipeline: validate input → parse PDF and convert directly to Markdown → write output incrementally.
6. On success, the tool must return a text message confirming the file path(s) written (e.g., `"Converted: C:\docs\report.md"`).
7. On failure, the tool must return an actionable error message (see FRD-010).
8. The tool must support cancellation — if the client cancels the request, the operation should stop promptly.

## Acceptance Criteria
- [ ] The tool appears in the list of available tools when the MCP client queries server capabilities.
- [ ] The tool's name and all parameter descriptions are visible and meaningful to the LLM.
- [ ] Invoking the tool with a valid `pdfPath` results in one or more Markdown files being written and a confirmation message returned.
- [ ] Invoking the tool with `chunkByChapter = true` produces multiple output files and the confirmation lists all of them.
- [ ] Invoking the tool with an invalid path returns a clear error message (not a stack trace or generic error).
- [ ] Cancelling a long-running conversion request stops the operation without leaving partial output.

## Dependencies
- **FRD-001** (MCP Server Hosting) — the tool must be hosted within the running MCP server.
- **FRD-003** (PDF Parsing) — the tool delegates PDF content extraction to the parsing service.
- **FRD-008** (File Output Management) — the tool delegates file writing to the output service.
- **FRD-009** (Chapter-Based Chunking) — the tool conditionally invokes chunking based on the `chunkByChapter` parameter.
- **FRD-010** (Input Validation) — the tool delegates input validation and error formatting.

## Notes
- The tool is a thin orchestration layer — all business logic resides in service components defined by other FRDs.
- The tool does not return converted content in the response; it writes files to disk and returns only a confirmation.
- There is no intermediate `.parsed.json` file. Parsing and Markdown conversion happen in a single operation.
- In CLI mode (FRD-011), the same conversion pipeline is invoked directly from the command-line entry point — bypassing the MCP tool layer. The underlying services, validation, and output behavior are identical.
