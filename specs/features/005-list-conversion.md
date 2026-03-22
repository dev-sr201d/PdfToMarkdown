# Feature: List Conversion

## Feature ID
FRD-005

## PRD Traceability
- **REQ-5**: The conversion must correctly map PDF lists (bulleted and numbered) to Markdown list syntax.
- **Goal**: Structural fidelity — lists in the source PDF are correctly represented in the Markdown output for ≥ 90% of well-structured text-based PDFs.
- **User Story**: "As a developer working with technical documentation, I want [...] lists [...] in the PDF to be accurately reflected in the Markdown output, so that the converted document is immediately usable without manual cleanup."

## Description
The system must detect bulleted and numbered lists in the parsed PDF content and convert them to the corresponding Markdown list syntax, preserving item order, nesting levels, and list type (ordered vs. unordered).

## Inputs
- PDF content with structural groupings and text content as extracted during the parsing/conversion pass (FRD-003).

## Outputs
- Markdown unordered list items using `- ` prefix for bulleted lists.
- Markdown ordered list items using `1. ` (or sequential numbering) prefix for numbered lists.
- Proper indentation for nested lists.

## Functional Requirements
1. The system must detect unordered (bulleted) lists and convert each item to Markdown unordered list syntax (e.g., `- Item text`).
2. The system must detect ordered (numbered) lists and convert each item to Markdown ordered list syntax (e.g., `1. Item text`).
3. The system must preserve the original ordering of list items.
4. The system must support nested lists (lists within lists), rendering them with appropriate indentation to represent the nesting hierarchy.
5. The system must handle list items that span multiple lines, keeping continuation lines associated with their parent list item.
6. Lists must be placed in the output in their original reading-order position relative to surrounding content.
7. If the PDF provides explicit structural tags for lists (e.g., `<L>`, `<LI>`), those must be used to identify list boundaries and items.
8. Where structural tags are absent, the system should use heuristic detection (e.g., bullet characters, sequential numbering patterns, consistent indentation) to identify list structures.

## Acceptance Criteria
- [ ] A PDF with a bulleted list produces a correctly formatted Markdown unordered list.
- [ ] A PDF with a numbered list produces a correctly formatted Markdown ordered list.
- [ ] List item ordering is preserved exactly as in the source PDF.
- [ ] Nested lists are rendered with correct indentation reflecting the nesting hierarchy.
- [ ] Multi-line list items are kept intact as single list items (not split into separate items).
- [ ] Lists appear in the correct position within the overall document flow.
- [ ] A PDF with no lists produces a valid Markdown document with no spurious list items.

## Dependencies
- **FRD-003** (PDF Parsing & Direct Markdown Conversion) — list conversion rules are applied during the parsing/conversion pass using structural groupings and text content.

## Notes
- List detection heuristics are an implementation concern. This FRD requires correct results for well-structured PDFs with identifiable list patterns.
- Mixed lists (e.g., a numbered list containing a bulleted sub-list) should be handled correctly where the structure is clear.
