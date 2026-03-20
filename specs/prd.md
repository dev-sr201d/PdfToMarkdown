# 📝 Product Requirements Document (PRD)

## 1. Purpose

Many developers and knowledge workers need to extract structured content from PDF documents for use in documentation pipelines, AI-assisted workflows, or content migration. Today, converting a PDF to well-formatted Markdown — preserving headings, lists, tables, and emphasis — requires manual effort or fragmented tooling outside the editor.

**PdfToMarkdown** is a C# console application that runs as a local MCP (Model Context Protocol) server, enabling GitHub Copilot Chat in VS Code to convert PDF files to Markdown directly from the chat interface. This keeps developers in their editor, reduces context-switching, and produces high-fidelity Markdown output ready for further use.

## 2. Scope

### In Scope

- A C# console application distributed as a local MCP server.
- Integration with GitHub Copilot Chat in VS Code via the MCP server protocol.
- Accepting a local file path to a PDF as input.
- Converting PDF content to Markdown with faithful preservation of:
  - Headlines / headings (mapped to Markdown heading levels)
  - Bulleted and numbered lists
  - Tables (mapped to Markdown table syntax)
  - Emphasized text (bold, italic)
- An optional mode to chunk the Markdown output by chapter (top-level heading boundaries).
- Writing the converted Markdown output to disk alongside the original PDF file.
- When chunking is enabled, writing each chapter as a separate file with a chapter-number suffix (e.g., `_1`, `_2`).

### Out of Scope

- A graphical user interface (GUI).
- Cloud deployment or remote server hosting.
- Conversion of non-PDF file formats (e.g., Word, HTML).
- OCR or scanned-image PDF support (only text-based PDFs are targeted).
- PDF creation or editing capabilities.
- Authentication, multi-user access, or network security concerns (local-only server).

## 3. Goals & Success Criteria

| Goal | Success Metric |
|---|---|
| Developers can convert a PDF to Markdown without leaving VS Code | A user can invoke the conversion from Copilot Chat and receive Markdown output in a single interaction |
| Structural fidelity of converted content | Headings, lists, tables, and emphasis in the source PDF are correctly represented in the Markdown output for ≥ 90% of well-structured text-based PDFs |
| Chapter-based chunking is available on demand | When the chunking option is enabled, separate Markdown files are written per chapter, named with an underscore-separated chapter number suffix |
| Low-friction setup | A developer can configure and start the MCP server locally within 5 minutes following the provided instructions |

## 4. High-Level Requirements

- **[REQ-1]** The application must run as a local MCP server that GitHub Copilot Chat in VS Code can discover and communicate with.
- **[REQ-2]** The server must expose a tool that accepts a local file system path to a PDF file.
- **[REQ-3]** The server must convert the content of the provided PDF into well-formed Markdown.
- **[REQ-4]** The conversion must correctly map PDF headings to the appropriate Markdown heading levels (`#`, `##`, `###`, etc.).
- **[REQ-5]** The conversion must correctly map PDF lists (bulleted and numbered) to Markdown list syntax.
- **[REQ-6]** The conversion must correctly map PDF tables to Markdown table syntax.
- **[REQ-7]** The conversion must preserve bold and italic emphasis in the Markdown output.
- **[REQ-8]** The server must support an optional parameter to chunk the output by chapter, splitting the Markdown at top-level heading boundaries.
- **[REQ-9]** The server must write the converted Markdown file to the same directory as the source PDF, using the same base filename with a `.md` extension (e.g., `report.pdf` → `report.md`).
- **[REQ-10]** When chunking is enabled, each chapter must be written as a separate Markdown file using the base filename with an underscore and chapter number as suffix (e.g., `report_1.md`, `report_2.md`).
- **[REQ-11]** The server must return a confirmation message to the caller indicating the output file path(s) written.
- **[REQ-12]** The application must provide clear error messages when the PDF path is invalid, the file is unreadable, or the content cannot be converted.

## 5. User Stories

```gherkin
As a developer using VS Code,
I want to provide a path to a PDF file through Copilot Chat,
so that a Markdown version of the document is saved alongside the original PDF.
```

```gherkin
As a developer working with technical documentation,
I want headings, lists, tables, and emphasized text in the PDF to be accurately reflected in the Markdown output,
so that the converted document is immediately usable without manual cleanup.
```

```gherkin
As a developer dealing with long PDF documents,
I want to optionally split the Markdown output by chapter into separate numbered files (e.g., document_1.md, document_2.md),
so that I can work with smaller, focused sections of the content independently.
```

```gherkin
As a developer setting up the tool,
I want the MCP server to be easy to configure and run locally,
so that I can start converting PDFs quickly without complex setup.
```

```gherkin
As a developer providing an incorrect file path,
I want to receive a clear error message explaining what went wrong,
so that I can correct the issue and retry.
```

## 6. Assumptions & Constraints

### Assumptions

- The target PDFs are text-based (not scanned images) and contain machine-readable text layers.
- The user has the .NET runtime installed on their local machine.
- The user has GitHub Copilot Chat available in their VS Code installation.
- Chapter boundaries in PDFs can be reliably identified by top-level heading styles or font-size heuristics in the PDF structure.

### Constraints

- The application runs locally only; no network or cloud dependencies for the conversion itself.
- The MCP server protocol defines the communication contract; the application must conform to the MCP specification supported by VS Code.
- Conversion quality depends on the structural quality of the source PDF — poorly structured or design-heavy PDFs may produce lower-fidelity output.
