# Task: Conversion Pipeline Orchestrator

## Task ID
008

## Feature
FRD-002 — PDF Conversion Tool Interface

## Description
Implement the conversion orchestrator service that coordinates the direct PDF-to-Markdown conversion pipeline. The orchestrator is the single service the MCP tool delegates to. It validates input, initializes the incremental writer, invokes the PDF-to-Markdown converter (which writes through the writer), finalizes the writer, and returns a confirmation message listing all files written.

This task supersedes the previous version of Task 008, which described a multi-step pipeline (validate → parse → convert → chunk → write) using an intermediate document model. The new pipeline reflects the direct-conversion architecture from revised FRD-002 and FRD-003.

## Dependencies
- Task 006 (Conversion Pipeline Service Contracts) — all service interfaces must be defined.
- Task 007 (PdfConvertTool MCP Tool Definition) — the tool must exist and delegate to the orchestrator interface (can be developed in parallel if both use the same interface contract).

## Technical Requirements

### Orchestration pipeline
The orchestrator must execute the following steps in order when invoked:

1. **Validate input** — Call the input validation service with the provided PDF path. If validation fails the service raises an exception, which propagates up to the tool and MCP SDK error handling.

2. **Initialize writer** — Create or initialize the Markdown writer with the source PDF path and the `chunkByChapter` mode. The writer prepares the output file(s) for incremental writing.

3. **Convert and write** — Call the PDF-to-Markdown converter service, passing the PDF path and the initialized writer. The converter reads the PDF, processes content page-by-page, converts each page's content directly to Markdown, and writes the converted content through the writer incrementally. The orchestrator does not handle individual pages or Markdown content — it delegates the entire conversion to the converter.

4. **Finalize writer** — Call the writer's finalization method to flush remaining content, close all file handles, and retrieve the list of absolute paths of files written.

5. **Return confirmation** — Compose and return a confirmation message listing all file paths written. The message format must be clear and suitable for display in Copilot Chat.

### Cancellation
- The orchestrator must pass the `CancellationToken` through to every service call.
- If cancellation is requested between pipeline steps, the orchestrator should check and honor it before proceeding to the next step.
- If cancellation or an exception occurs during conversion, the writer must still be properly disposed to avoid resource leaks (use `await using` or try/finally pattern).

### Service resolution
- The orchestrator receives all service dependencies via constructor injection.
- The orchestrator must be registered in DI as the implementation of the orchestrator interface defined in Task 006.

### Error handling
- The orchestrator must NOT catch exceptions from downstream services — it lets them propagate to the tool layer where the MCP SDK handles them.
- The only exception: the orchestrator may catch exceptions from downstream services if it needs to add context (e.g., wrapping a generic I/O exception in an `McpException` with a user-friendly message). In that case, preserve the original exception as the inner exception.
- The writer must always be properly disposed even when exceptions occur.

### Confirmation message format
- Standard mode: `"Converted: {absolutePath}"` (single file path).
- Chunked mode: `"Converted into {N} files:"` followed by a newline-separated list of absolute paths.
- The exact format may be adjusted for readability, but must include all file paths written.

## Acceptance Criteria
- [ ] The orchestrator implements the orchestrator interface from Task 006.
- [ ] The orchestrator calls pipeline services in the correct order: validate → initialize writer → convert → finalize writer.
- [ ] The `chunkByChapter` flag is passed to the writer during initialization to control output mode.
- [ ] The `CancellationToken` is forwarded to every async service call.
- [ ] The orchestrator checks for cancellation between pipeline steps.
- [ ] The writer is always properly disposed, even when exceptions or cancellation occur.
- [ ] The returned confirmation message includes all file paths written.
- [ ] The orchestrator is registered in DI as the implementation of the orchestrator interface.
- [ ] Exceptions from downstream services propagate without being swallowed.
- [ ] `dotnet build src/PdfToMarkdown` produces zero errors and zero warnings.

## Testing Requirements
- **Happy path test (standard mode)**: Mock all service dependencies, invoke the orchestrator with `chunkByChapter = false`, verify each service is called in order with correct arguments, and the confirmation message includes the output file path.
- **Happy path test (chunked mode)**: Mock all service dependencies, invoke with `chunkByChapter = true`, verify the writer is initialized in chunked mode and the confirmation message lists all output files.
- **Cancellation test**: Supply a pre-cancelled token, verify the orchestrator throws `OperationCanceledException` without calling downstream services (or stops early).
- **Writer disposal on exception**: Verify the writer is disposed even when the converter throws an exception during conversion.
- **Writer disposal on cancellation**: Verify the writer is disposed when cancellation occurs during conversion.
- **Validation failure test**: Mock the validator to throw, verify the orchestrator does not proceed to writer initialization or conversion.
- **Service call order test**: Verify that services are called in the expected sequence (validate before writer init, writer init before convert, etc.).
- **Confirmation message format tests**: Verify the message format for single-file and multi-file output.
- Minimum test coverage: all branching paths in the orchestrator must be exercised.
