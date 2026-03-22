# Feature: Heading Conversion

## Feature ID
FRD-004

## PRD Traceability
- **REQ-4**: The conversion must correctly map PDF headings to the appropriate Markdown heading levels (`#`, `##`, `###`, etc.).
- **Goal**: Structural fidelity — headings in the source PDF are correctly represented in the Markdown output for ≥ 90% of well-structured text-based PDFs.
- **User Story**: "As a developer working with technical documentation, I want headings [...] in the PDF to be accurately reflected in the Markdown output, so that the converted document is immediately usable without manual cleanup."

## Description
The system must detect heading elements in the parsed PDF content and map them to the appropriate Markdown heading levels. Headings are the primary structural backbone of the output document and are also used as chapter boundaries by the chunking feature (FRD-009).

## Inputs
- PDF content with font metadata (size, weight) as extracted during the parsing/conversion pass (FRD-003).

## Outputs
- Markdown heading syntax (`#`, `##`, `###`, etc.) corresponding to the hierarchical heading levels detected in the PDF.

## Functional Requirements
1. The system must identify text segments that function as headings based on available structural information and/or font characteristics (e.g., larger font size, bold weight, distinct font family).
2. The system must establish a heading hierarchy — distinguishing between top-level headings (H1), second-level headings (H2), and deeper levels — based on relative font sizes or explicit PDF structure tags.
3. The system must map each detected heading level to the corresponding Markdown heading syntax:
   - Top-level → `# Heading`
   - Second-level → `## Heading`
   - Third-level → `### Heading`
   - And so on, up to a reasonable depth (e.g., 6 levels per Markdown spec).
4. Heading text must be trimmed of excess whitespace but otherwise preserved verbatim.
5. Headings must be placed in the output in their original reading-order position relative to surrounding content.
6. If the PDF provides explicit structural tags for headings (e.g., tagged PDF with `<H1>`, `<H2>`), those must take precedence over font-size heuristics.

## Acceptance Criteria
- [ ] A PDF with clearly differentiated heading levels (e.g., via font size or structural tags) produces correctly leveled Markdown headings.
- [ ] A PDF with tagged heading structure (`<H1>`, `<H2>`, etc.) produces headings matching those tag levels.
- [ ] Heading text content is preserved accurately (no truncation, no extra whitespace).
- [ ] Headings appear in the correct position within the overall document flow.
- [ ] A PDF with no identifiable headings produces a valid Markdown document (body text only, no spurious headings).
- [ ] At least 6 heading levels are supported.

## Dependencies
- **FRD-003** (PDF Parsing & Direct Markdown Conversion) — heading conversion rules are applied during the parsing/conversion pass using font metadata extracted from the PDF.

## Downstream Dependents
- **FRD-009** (Chapter-Based Chunking) — uses top-level headings as chapter boundaries.

## Notes
- Heading detection heuristics (font-size thresholds, relative sizing) are an implementation concern. This FRD requires only that the heuristic produces correct results for well-structured PDFs per the PRD's ≥ 90% fidelity target.
- Poorly-structured PDFs where headings are indistinguishable from body text may produce degraded results; this is acceptable per PRD constraints.
