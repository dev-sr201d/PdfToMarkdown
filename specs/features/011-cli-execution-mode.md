# Feature: CLI Execution Mode

## Feature ID
FRD-011

## PRD Traceability
- **REQ-1a**: When command-line arguments are provided, the application must operate in CLI mode — performing a one-shot conversion and exiting without starting the MCP server.
- **REQ-13**: In CLI mode, the application must use standard exit codes: `0` for success, non-zero for failure.
- **REQ-14**: In CLI mode, error messages must be written to stderr and the confirmation of written file path(s) must be written to stdout.
- **User Stories**:
  - "As a developer or script author, I want to run the application from the command line with a PDF path, so that I can convert PDFs without needing VS Code or an MCP client."
  - "As a developer using the CLI mode, I want the application to return standard exit codes (0 for success, non-zero for failure), so that I can use it reliably in shell scripts and CI pipelines."

## Description
When command-line arguments are provided, the application must bypass MCP server startup and instead perform a one-shot PDF-to-Markdown conversion. The CLI mode reuses the same conversion pipeline, services, and output behavior as MCP mode — the only differences are how input is received (command-line arguments instead of MCP JSON-RPC) and how results are communicated (stdout/stderr and exit codes instead of MCP responses).

## Inputs
- **PDF path** (required, positional argument): An absolute or relative path to the PDF file to convert.
- **`--chunk-by-chapter`** (optional flag, default: off): When present, enables chapter-based chunking (same behavior as the `chunkByChapter` parameter in MCP mode).

## Outputs
- On success: the absolute path(s) of all written Markdown file(s) printed to stdout (one path per line), and the process exits with code `0`.
- On failure: a clear, human-readable error message printed to stderr, and the process exits with a non-zero exit code.

## Functional Requirements

### Argument Parsing
1. The application must accept a PDF file path as a positional command-line argument.
2. The application must accept an optional `--chunk-by-chapter` flag.
3. If unrecognized arguments are provided, the application must print a usage message to stderr and exit with a non-zero exit code.

### Conversion Behavior
4. The CLI mode must use the same conversion pipeline as MCP mode — the same input validation (FRD-010), PDF parsing and Markdown conversion (FRD-003), file output (FRD-008), and optional chunking (FRD-009).
5. Output files must be written to the same directory as the source PDF, following the same naming conventions as MCP mode.

### Output & Exit Codes
6. On successful conversion, the application must print the absolute path(s) of all written Markdown files to stdout, one per line.
7. On successful conversion, the application must exit with code `0`.
8. On any failure (validation error, parsing error, write error), the application must print a clear error message to stderr and exit with a non-zero exit code.
9. Diagnostic and log output must be written to stderr — stdout is reserved for the file path output on success.

## Acceptance Criteria
- [ ] Running `dotnet run --project src/PdfToMarkdown -- report.pdf` converts the PDF and writes `report.md` alongside it.
- [ ] The absolute path of `report.md` is printed to stdout.
- [ ] The process exits with code `0` on success.
- [ ] Running with `--chunk-by-chapter` produces multiple files and prints all paths to stdout.
- [ ] Running with a non-existent file path prints an error to stderr and exits with a non-zero code.
- [ ] Running with no arguments starts the MCP server (not CLI mode).
- [ ] Running with unrecognized arguments prints a usage message to stderr and exits with a non-zero code.
- [ ] No MCP server is started when arguments are present.
- [ ] Diagnostic/log output goes to stderr — stdout contains only the result file paths on success.

## Dependencies
- **FRD-001** (Application Hosting & Mode Selection) — mode selection logic determines when CLI mode is activated.
- **FRD-003** (PDF Parsing & Direct Markdown Conversion) — the conversion pipeline used by CLI mode.
- **FRD-008** (File Output Management) — file writing behavior shared with MCP mode.
- **FRD-009** (Chapter-Based Chunking) — chunking behavior when `--chunk-by-chapter` is specified.
- **FRD-010** (Input Validation & Error Reporting) — input validation and error formatting shared with MCP mode.

## Notes
- CLI mode is a thin entry-point layer. It parses arguments, invokes the same DI-registered services as MCP mode, and translates the result into stdout output and an exit code.
- The conversion pipeline, output behavior, and error messages are identical between CLI and MCP modes — only the I/O framing differs.
