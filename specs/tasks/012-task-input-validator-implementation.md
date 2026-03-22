# Task: Input Validator Implementation

## Task ID
012

## Supersedes
Task 012 (PDF Parser Core Implementation) — the parser's functionality is merged into Task 011 (PDF Direct Markdown Converter). This task slot is now used for input validation.

## Feature
FRD-010 — Input Validation & Error Reporting

## Description
Implement the input validation service that verifies a PDF file path meets all preconditions before the conversion pipeline processes it. The validator runs as the first step in the orchestration pipeline and provides clear, actionable error messages when validation fails.

## Dependencies
- Task 006 (Conversion Pipeline Service Contracts) — the validator interface must be defined.

## Technical Requirements

### Validation checks
The validator must perform the following checks in order, throwing a descriptive `McpException` at the first failure:

1. **Path is non-empty** — The PDF file path must not be null, empty, or whitespace. Error message must indicate that no file path was provided.

2. **Path has `.pdf` extension** — The file path must end with `.pdf` (case-insensitive). Error message must indicate the expected file extension and the actual extension received.

3. **File exists** — The file must exist on the local file system. Error message must include the provided path and indicate the file was not found.

4. **File is readable** — The file must be accessible for reading (the process has read permissions). Error message must indicate that the file exists but cannot be read, including the path.

### Scope boundaries
- The validator must NOT check for PDF content validity (encrypted PDFs, PDFs with no text, corrupted content, etc.) — those checks belong in the converter (Task 011 / FRD-003).
- The validator must NOT attempt to open or parse the PDF file — it only validates file system preconditions.

### Design constraints
- The validator must be a stateless service with no dependencies beyond the file system.
- Throw `McpException` with clear, user-facing messages for each validation failure.
- Accept `CancellationToken` on the validation method.
- Use `ConfigureAwait(false)` if any async operations are used.
- All public members must have XML documentation comments.
- Follow AGENTS.md naming and code layout conventions.

### DI registration
- Register the validator implementation as the implementation of the validator interface from Task 006, replacing the stub.

## Acceptance Criteria
- [ ] The validator implements the validator interface from Task 006.
- [ ] An empty, null, or whitespace path throws `McpException` with a descriptive message.
- [ ] A path without `.pdf` extension throws `McpException` with a descriptive message.
- [ ] Extension validation is case-insensitive (`.PDF`, `.Pdf`, `.pdf` all pass).
- [ ] A nonexistent file path throws `McpException` including the path in the message.
- [ ] An unreadable file throws `McpException` with a descriptive message.
- [ ] A valid, readable `.pdf` file path passes validation without error.
- [ ] The validator is registered in DI, replacing the stub implementation.
- [ ] All public members have XML documentation comments.
- [ ] The project builds with zero errors and zero warnings.

## Testing Requirements
- [ ] **Valid PDF path** — A valid, existing `.pdf` file passes validation without throwing.
- [ ] **Null path** — Throws `McpException` with a message about missing path.
- [ ] **Empty path** — Throws `McpException` with a message about missing path.
- [ ] **Whitespace-only path** — Throws `McpException` with a message about missing path.
- [ ] **Wrong extension (.txt)** — Throws `McpException` mentioning the expected extension.
- [ ] **No extension** — Throws `McpException` mentioning the expected extension.
- [ ] **Extension case insensitivity** — Paths ending in `.PDF`, `.Pdf`, `.pDf` all pass validation.
- [ ] **Nonexistent file** — Throws `McpException` with the path included in the message.
- [ ] **Unreadable file** — Throws `McpException` (if testable on the platform; may require platform-specific setup).
- [ ] **Validation stops at first failure** — If path is empty, no file-existence check is performed (verify via test that doesn't create a file).
- Minimum test coverage: ≥85%.
