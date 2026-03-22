# Task: Conversion Pipeline Service Contracts

## Task ID
006

## Feature
FRD-002 — PDF Conversion Tool Interface
FRD-003 — PDF Parsing & Direct Markdown Conversion

## Description
Define the service interfaces that form the contracts for the direct-conversion pipeline. The revised architecture eliminates the intermediate document model and JSON persistence — parsing and Markdown conversion are a single unified operation that writes output incrementally to disk. These interfaces establish the architectural boundaries between the MCP tool layer and the service layer.

This task supersedes the previous version of Task 006, which defined contracts for a multi-step pipeline with separate parser, converter, chunker, and writer services. The new contracts reflect the direct-conversion architecture specified in the revised FRD-002 and FRD-003.

## Dependencies
- Task 001 (Solution & Main Project Scaffolding) — project structure and folders must exist.

## Technical Requirements

### Existing code to remove

The following interfaces, implementations, and types from the previous architecture must be removed, as they no longer fit the pipeline contract:

- **Interfaces**: `IDocumentConverter`, `IDocumentModelSerializer`, `IPdfParser` (in its current `DocumentModel`-producing form), `IChapterChunker`.
- **Implementations**: `StubDocumentConverter`, `DocumentModelSerializer`, `PdfParser`, `CachingPdfParser`, `StubChapterChunker`.
- **Model types (all files in `Models/`)**: `DocumentModel`, `PageModel`, `HeadingBlock`, `ParagraphBlock`, `ListBlock`, `TableBlock`, `ListItem`, `TableRow`, `TableCell`, `TextSegment`, `FontMetadata`, `DocumentBlock`, `DocumentElementKind`, `ChapterChunk`.
- **Test classes** that reference removed types must be removed or updated to compile cleanly.

The `IInputValidator`, `StubInputValidator`, `IOutputWriter`, `StubOutputWriter`, and `IConversionOrchestrator` interfaces may be retained and revised, or removed and replaced, at the implementer's discretion — as long as the new contracts below are satisfied.

### Service interfaces to define

The following interfaces must be created in the `Services/` directory, one per file:

1. **Input validation interface** — Accepts a file path and validates that it meets all preconditions for processing (path is non-empty, file exists, has `.pdf` extension, is readable). Must throw a descriptive `McpException` on validation failure. Corresponds to FRD-010.

2. **PDF-to-Markdown converter interface** — The core service of the new architecture. Accepts a PDF file path, a Markdown writer instance (see #3), and a cancellation token. Reads the PDF, performs any necessary pre-analysis (e.g., font size distribution for heading detection), then processes the document page-by-page — converting content directly to Markdown and passing the converted content to the writer incrementally. Does not return Markdown content to the caller — it writes through the provided writer. May return lightweight summary metadata (e.g., pages processed) if useful for confirmation messages. Corresponds to FRD-003.

3. **Markdown writer interface** — Manages incremental Markdown output to disk. Must support two modes:
   - **Standard mode**: Writes all content to a single `.md` file alongside the source PDF.
   - **Chunked mode**: Detects chapter boundaries (top-level heading markers — a line starting with `# ` followed by text) in the incoming Markdown content and routes content to separate numbered files (e.g., `report_1.md`, `report_2.md`).

   The writer must be initializable with the source PDF path and the chunking mode. It must accept Markdown content incrementally (page-by-page) and flush to disk after each write. It must provide a finalization method that closes all files and returns the list of absolute paths written. Must implement `IAsyncDisposable` for resource cleanup. Corresponds to FRD-008 and FRD-009.

4. **Conversion orchestrator interface** — Accepts a PDF file path, a `chunkByChapter` flag, and a cancellation token. Coordinates the full pipeline (validate → initialize writer → convert with incremental writes → finalize → return confirmation message). This is the single interface the MCP tool delegates to.

### Stub implementations

Provide a stub implementation for each new interface that throws `NotImplementedException`. These stubs exist only to satisfy DI until real implementations are provided by Tasks 010–012. Register all stubs in `Program.cs`.

### Shared model types

No shared model types are required for the new architecture. The interfaces work with primitive types (file paths, Markdown strings, file path lists). If the converter needs to communicate metadata to the orchestrator (e.g., page count for the confirmation message), a lightweight result type may be defined, but this is optional.

### Design constraints
- All interfaces must follow AGENTS.md naming conventions (`I` + PascalCase).
- All async methods must accept `CancellationToken` as the last parameter.
- All async methods must follow the `Async` suffix convention.
- All public members must have XML documentation comments (`///`).
- Interfaces must be general enough to allow different implementations but specific enough to enforce the pipeline contract.
- One interface per file, one stub per file. File name matches type name.

## Acceptance Criteria
- [ ] Old interfaces, implementations, and model types from the previous architecture are removed.
- [ ] Each new service interface is defined in its own file in `Services/`.
- [ ] Stub implementations are provided for each interface (throwing `NotImplementedException`).
- [ ] All interfaces use `CancellationToken` on async methods.
- [ ] All public members have XML documentation comments.
- [ ] `Program.cs` DI registrations are updated to use the new interfaces and stub implementations.
- [ ] The project compiles with zero errors and zero warnings after the changes.
- [ ] The test project compiles with zero errors and zero warnings (tests referencing removed types are cleaned up).
- [ ] Interface names follow the `I` + PascalCase convention from AGENTS.md.
- [ ] The orchestrator interface captures the full pipeline contract (path + chunking flag + cancellation → confirmation message).

## Testing Requirements
- No unit tests for interfaces — they contain no logic.
- Existing tests that reference removed types must be updated or removed to maintain a green build.
- Build verification: `dotnet build src/PdfToMarkdown` must succeed with zero errors and zero warnings.
- Test build verification: `dotnet test tests/PdfToMarkdown.Tests` must succeed (all remaining tests pass).
