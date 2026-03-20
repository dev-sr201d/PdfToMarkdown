# Feature: File Output Management

## Feature ID
FRD-008

## PRD Traceability
- **REQ-9**: The server must write the converted Markdown file to the same directory as the source PDF, using the same base filename with a `.md` extension (e.g., `report.pdf` → `report.md`).
- **REQ-11**: The server must return a confirmation message to the caller indicating the output file path(s) written.
- **User Story**: "As a developer using VS Code, I want to provide a path to a PDF file through Copilot Chat, so that a Markdown version of the document is saved alongside the original PDF."

## Description
The system must write the converted Markdown content to the local file system, placing the output file in the same directory as the source PDF, using the same base filename with a `.md` extension. The system must return the absolute path(s) of all files written.

## Inputs
- The absolute path of the source PDF file (used to determine output directory and filename).
- The converted Markdown content (a single string for standard mode; multiple named chunks for chunked mode — see FRD-009).

## Outputs
- One or more `.md` files written to the same directory as the source PDF.
- A list of absolute paths of all files successfully written, for use in the confirmation message.

## Functional Requirements
1. In standard mode (no chunking), the system must write a single Markdown file using the source PDF's base filename with a `.md` extension in the same directory.
   - Example: `C:\docs\report.pdf` → `C:\docs\report.md`
2. In chunked mode, the system must write multiple Markdown files per the naming convention defined in FRD-009.
3. The system must overwrite existing output files without prompting — the tool is non-interactive.
4. The system must write files using UTF-8 encoding.
5. The system must return the absolute path(s) of all files written so that the tool (FRD-002) can include them in its confirmation message.
6. The system must handle file system errors (e.g., permission denied, disk full) and raise clear, descriptive errors rather than failing silently.
7. The system must not leave partial or corrupt output files if writing is interrupted — either the file is written completely or it is not written at all.

## Acceptance Criteria
- [ ] Converting `report.pdf` produces `report.md` in the same directory.
- [ ] The output file uses UTF-8 encoding.
- [ ] If `report.md` already exists, it is overwritten without error or prompt.
- [ ] The returned path list contains the correct absolute path(s) of all files written.
- [ ] A file system error (e.g., read-only directory) produces a clear, descriptive error message.
- [ ] An interrupted write does not leave a partial output file.

## Dependencies
- **FRD-003** (PDF Parsing) — provides the content to be written.
- **FRD-004–007** (Conversion features) — produce the Markdown content.

## Downstream Dependents
- **FRD-009** (Chapter-Based Chunking) — extends the file writing logic with multi-file output and chapter numbering.

## Notes
- The output directory is always the same as the source PDF's directory. There is no option to specify a different output directory.
- The tool does not return Markdown content to the caller — it writes to disk and returns only file paths.
