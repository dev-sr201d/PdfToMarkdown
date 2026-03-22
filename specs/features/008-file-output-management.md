# Feature: File Output Management

## Feature ID
FRD-008

## PRD Traceability
- **REQ-9**: The server must write the converted Markdown file to the same directory as the source PDF, using the same base filename with a `.md` extension (e.g., `report.pdf` → `report.md`).
- **REQ-11**: The server must return a confirmation message to the caller indicating the output file path(s) written.
- **User Story**: "As a developer using VS Code, I want to provide a path to a PDF file through Copilot Chat, so that a Markdown version of the document is saved alongside the original PDF."

## Description
The system must write the converted Markdown content to the local file system incrementally as pages are processed, placing the output file in the same directory as the source PDF, using the same base filename with a `.md` extension. The system must return the absolute path(s) of all files written.

## Inputs
- The absolute path of the source PDF file (used to determine output directory and filename).
- Converted Markdown content provided incrementally (page by page) during the parsing/conversion pass — not as a single complete string.

## Outputs
- One or more `.md` files written to the same directory as the source PDF.
- A list of absolute paths of all files successfully written, for use in the confirmation message.

## Functional Requirements
1. In standard mode (no chunking), the system must write a single Markdown file using the source PDF's base filename with a `.md` extension in the same directory.
   - Example: `C:\docs\report.pdf` → `C:\docs\report.md`
2. In chunked mode, the system must write multiple Markdown files per the naming convention defined in FRD-009.
3. The system must support receiving Markdown content incrementally (page by page) and appending it to the output file as each page is processed, rather than requiring the complete content up front.
4. The system must overwrite existing output files without prompting — the tool is non-interactive.
5. The system must write files using UTF-8 encoding.
6. The system must return the absolute path(s) of all files written so that the tool (FRD-002) can include them in its confirmation message.
7. The system must handle file system errors (e.g., permission denied, disk full) and raise clear, descriptive errors rather than failing silently.
8. If writing is interrupted before completion, the system should make a best effort to clean up any partial output files that were written.

## Acceptance Criteria
- [ ] Converting `report.pdf` produces `report.md` in the same directory.
- [ ] The output file uses UTF-8 encoding.
- [ ] If `report.md` already exists, it is overwritten without error or prompt.
- [ ] Markdown content is written incrementally as pages are processed — the system does not wait for the entire document to be converted before beginning to write.
- [ ] The returned path list contains the correct absolute path(s) of all files written.
- [ ] A file system error (e.g., read-only directory) produces a clear, descriptive error message.
- [ ] An interrupted write makes a best effort to clean up partial output files.

## Dependencies
- **FRD-003** (PDF Parsing & Direct Markdown Conversion) — provides the converted content incrementally during the parsing/conversion pass.

## Downstream Dependents
- **FRD-009** (Chapter-Based Chunking) — extends the file writing logic with multi-file output and chapter numbering.

## Notes
- The output directory is always the same as the source PDF's directory. There is no option to specify a different output directory.
- The tool does not return Markdown content to the caller — it writes to disk and returns only file paths.
- With incremental writing, the atomicity guarantee changes: if a conversion is interrupted mid-document, a partial output file may exist. The system should attempt cleanup but cannot guarantee it in all failure scenarios (e.g., process crash). This trade-off is acceptable for the memory efficiency gained.
