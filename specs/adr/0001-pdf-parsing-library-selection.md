# ADR-0001: PDF Parsing Library Selection

## Status

Accepted

## Date

2026-03-22

## Context

PdfToMarkdown is a C# console application that runs as a local MCP (Model Context Protocol) stdio server. Its core function is converting text-based PDF files to well-formed Markdown, preserving headings, lists, tables, and text emphasis (bold/italic).

The conversion pipeline (FRD-003) requires:

1. **Character/word-level text extraction** with correct reading order.
2. **Font metadata per text segment** — font size, weight (bold), and style (italic) — to support heading detection by font-size heuristics and emphasis preservation.
3. **Page-by-page iteration** for incremental Markdown output (content is flushed to disk per page, not accumulated in memory).
4. **Detection of encrypted/password-protected PDFs** so the tool can surface clear error messages.
5. **No OCR** — only machine-readable text layers are in scope.
6. **Pure managed code** preferred — no native binary dependencies to simplify deployment and cross-platform support.
7. **.NET 9 compatibility.**
8. **Permissive open-source license** — the project must not be encumbered by copyleft obligations.

## Decision

Use **PdfPig** (`UglyToad.PdfPig`) as the PDF parsing library.

## Considered Options

### 1. PdfPig (`UglyToad.PdfPig`) — **Selected**

A fully managed .NET library focused on reading and extracting content from PDF files.

**Pros:**

- Provides letter-level and word-level text extraction with positional data (`page.GetLetters()`, `page.GetWords()`).
- Exposes full font metadata per letter: `FontName`, `FontSize`, `PointSize`, plus font descriptor flags for bold/italic detection — exactly what the pre-analysis and heading-detection heuristics require.
- Supports page-by-page iteration via `document.GetPages()`, enabling the incremental write pattern required by FRD-003.
- Pure managed code — no native dependencies. Single NuGet package, no platform-specific binaries.
- Apache 2.0 license — fully permissive.
- Targets .NET Standard 2.0, compatible with .NET 9.
- Active open-source project with community contributions.

**Cons:**

- Smaller community compared to iText.
- Some complex PDFs with unusual encoding or structure may not parse perfectly.
- Table detection requires manual heuristics based on spatial positioning (no built-in "give me the tables" API that handles all layouts).

### 2. iText 7 (`itext7`)

A mature, full-featured PDF library with comprehensive text extraction and manipulation capabilities.

**Pros:**

- Industry-standard library with extensive documentation.
- Excellent text extraction via `ITextExtractionStrategy`.
- Rich font and layout metadata.
- Handles a wide range of PDF edge cases.

**Cons:**

- **Licensed under AGPL-3.0.** Any software that uses iText must itself be released under AGPL, or the developer must purchase a commercial license. This is a licensing dealbreaker for most projects that are not themselves AGPL.
- Commercial license is expensive.
- Heavier dependency footprint.

**Verdict:** Eliminated due to AGPL license.

### 3. PdfSharp (`PdfSharp` / `PDFsharp`)

A .NET library for creating and modifying PDF documents.

**Pros:**

- MIT license.
- Well-known in the .NET ecosystem.
- Good for PDF creation/generation.

**Cons:**

- **Does not provide a text extraction API.** PdfSharp is designed for creating, modifying, and merging PDFs — not for reading text content out of existing documents.
- No API to retrieve font metadata (size, bold, italic) per character from an existing PDF.
- Would require manually walking PDF content streams and reimplementing a text extraction engine from scratch.

**Verdict:** Eliminated — fundamentally lacks the text extraction capability this project requires.

### 4. Docnet (`Docnet.Core`)

A .NET wrapper around Google's PDFium rendering engine.

**Pros:**

- MIT license.
- Backed by PDFium, which handles a wide range of PDFs.
- Page-level text extraction available.

**Cons:**

- Wraps a native C library (PDFium) — requires platform-specific native binaries, complicating deployment.
- Limited font metadata access compared to PdfPig.
- Less granular text extraction (page-level, not letter-level with font properties).

**Verdict:** Viable but inferior on font metadata access and deployment simplicity.

### 5. Commercial Libraries (Syncfusion, Aspose.PDF)

**Pros:**

- Feature-rich with excellent support.
- Handle edge cases well.

**Cons:**

- Proprietary licenses with cost (Syncfusion has a free community license but still requires a license key and has usage restrictions; Aspose is expensive).
- Adds vendor lock-in and licensing management overhead.

**Verdict:** Eliminated due to cost and licensing complexity for an open-source tool.

## Consequences

### Positive

- The project gets character-level text extraction with full font metadata using a single, permissive, pure-managed NuGet package.
- No native dependencies simplify build, test, and deployment.
- Font-size heuristics for heading detection (FRD-003 §13, FRD-004) are straightforward to implement using PdfPig's `Letter.FontSize` and font descriptor flags.
- Page-by-page iteration supports the incremental output pattern without architectural workarounds.

### Negative

- Table detection will require custom spatial-analysis logic rather than relying on a built-in high-level API. This is acceptable given that FRD-006 already anticipates heuristic-based table detection.
- Edge-case PDFs with unusual encodings or non-standard fonts may produce degraded output. This is acknowledged as acceptable per the PRD constraint: "Conversion quality depends on the structural quality of the source PDF."

### Risks

- PdfPig is a smaller project than iText. If the maintainer stops development, the project may need to fork or migrate. Mitigated by the Apache 2.0 license allowing forking.

## Notes

- The current csproj references version `1.7.0-custom-5`, which is not an official NuGet release. If a custom fork is used, the fork's source and the rationale for the modifications should be documented. If the official release is sufficient, the project should switch to it.
