using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace PdfToMarkdown.Tests;

/// <summary>
/// Generates PDF test fixture files using PdfPig's <see cref="PdfDocumentBuilder"/>.
/// Call <see cref="GenerateAll"/> to create all fixtures in a target directory.
/// </summary>
internal static class TestPdfGenerator
{
    /// <summary>
    /// Generates all test PDF fixtures in the specified directory.
    /// </summary>
    public static void GenerateAll(string directory)
    {
        Directory.CreateDirectory(directory);

        GenerateSimple(directory);
        GenerateMultiPage(directory);
        GenerateHeadings(directory);
        GenerateLists(directory);
        GenerateTables(directory);
        GenerateEmphasis(directory);
        GenerateMixedContent(directory);
        GenerateNoText(directory);
        GenerateEmpty(directory);
        GenerateSixHeadingLevels(directory);
        GenerateHeadingWithEmphasis(directory);
        GenerateNestedLists(directory);
        GenerateMultiLineListItems(directory);
        GenerateTableWithEmphasis(directory);
        GenerateTableWithVaryingColumns(directory);
        GenerateNoHeadings(directory);
        GenerateListWithEmphasis(directory);
        GenerateTwoColumn(directory);
        GenerateTwoColumnWithFullWidthHeader(directory);
        GenerateBoldHeadings(directory);
        GenerateMixedFontHeadings(directory);
    }

    /// <summary>Single page with a title and body text.</summary>
    public static void GenerateSimple(string directory)
    {
        PdfDocumentBuilder builder = new();
        PdfDocumentBuilder.AddedFont regular = builder.AddStandard14Font(Standard14Font.Helvetica);
        PdfDocumentBuilder.AddedFont bold = builder.AddStandard14Font(Standard14Font.HelveticaBold);

        PdfPageBuilder page = builder.AddPage(PageSize.A4);
        double top = page.PageSize.Top - 50;

        page.AddText("Document Title", 24, new PdfPoint(50, top), bold);
        page.AddText("This is the first paragraph of the document. It contains simple body text.", 12, new PdfPoint(50, top - 40), regular);
        page.AddText("This is the second paragraph with more content for testing.", 12, new PdfPoint(50, top - 60), regular);

        File.WriteAllBytes(Path.Combine(directory, "simple.pdf"), builder.Build());
    }

    /// <summary>Three pages with distinct content.</summary>
    public static void GenerateMultiPage(string directory)
    {
        PdfDocumentBuilder builder = new();
        PdfDocumentBuilder.AddedFont regular = builder.AddStandard14Font(Standard14Font.Helvetica);

        for (int i = 1; i <= 3; i++)
        {
            PdfPageBuilder page = builder.AddPage(PageSize.A4);
            double top = page.PageSize.Top - 50;
            page.AddText($"Content on page {i}.", 12, new PdfPoint(50, top), regular);
            page.AddText($"Additional text on page {i} for testing multi-page parsing.", 12, new PdfPoint(50, top - 20), regular);
        }

        File.WriteAllBytes(Path.Combine(directory, "multi-page.pdf"), builder.Build());
    }

    /// <summary>Text at multiple font sizes representing H1–H3 plus body.</summary>
    public static void GenerateHeadings(string directory)
    {
        PdfDocumentBuilder builder = new();
        PdfDocumentBuilder.AddedFont regular = builder.AddStandard14Font(Standard14Font.Helvetica);
        PdfDocumentBuilder.AddedFont bold = builder.AddStandard14Font(Standard14Font.HelveticaBold);

        PdfPageBuilder page = builder.AddPage(PageSize.A4);
        double top = page.PageSize.Top - 50;

        page.AddText("Main Heading", 28, new PdfPoint(50, top), bold);
        page.AddText("Body text under main heading.", 12, new PdfPoint(50, top - 40), regular);
        page.AddText("Sub Heading", 20, new PdfPoint(50, top - 70), bold);
        page.AddText("Body text under sub heading.", 12, new PdfPoint(50, top - 100), regular);
        page.AddText("Minor Heading", 16, new PdfPoint(50, top - 130), bold);
        page.AddText("Body text under minor heading.", 12, new PdfPoint(50, top - 160), regular);

        File.WriteAllBytes(Path.Combine(directory, "headings.pdf"), builder.Build());
    }

    /// <summary>Bulleted and numbered lists.</summary>
    public static void GenerateLists(string directory)
    {
        PdfDocumentBuilder builder = new();
        PdfDocumentBuilder.AddedFont regular = builder.AddStandard14Font(Standard14Font.Helvetica);

        PdfPageBuilder page = builder.AddPage(PageSize.A4);
        double top = page.PageSize.Top - 50;

        // Unordered list
        page.AddText("\u2022 First bullet item", 12, new PdfPoint(50, top), regular);
        page.AddText("\u2022 Second bullet item", 12, new PdfPoint(50, top - 18), regular);
        page.AddText("\u2022 Third bullet item", 12, new PdfPoint(50, top - 36), regular);

        // Ordered list
        page.AddText("1. First numbered item", 12, new PdfPoint(50, top - 70), regular);
        page.AddText("2. Second numbered item", 12, new PdfPoint(50, top - 88), regular);
        page.AddText("3. Third numbered item", 12, new PdfPoint(50, top - 106), regular);

        File.WriteAllBytes(Path.Combine(directory, "lists.pdf"), builder.Build());
    }

    /// <summary>A simple table with header row and data rows.</summary>
    public static void GenerateTables(string directory)
    {
        PdfDocumentBuilder builder = new();
        PdfDocumentBuilder.AddedFont regular = builder.AddStandard14Font(Standard14Font.Helvetica);
        PdfDocumentBuilder.AddedFont bold = builder.AddStandard14Font(Standard14Font.HelveticaBold);

        PdfPageBuilder page = builder.AddPage(PageSize.A4);
        double top = page.PageSize.Top - 50;

        // Header row
        page.AddText("Name", 12, new PdfPoint(50, top), bold);
        page.AddText("Age", 12, new PdfPoint(200, top), bold);
        page.AddText("City", 12, new PdfPoint(300, top), bold);

        // Data rows
        page.AddText("Alice", 12, new PdfPoint(50, top - 20), regular);
        page.AddText("30", 12, new PdfPoint(200, top - 20), regular);
        page.AddText("London", 12, new PdfPoint(300, top - 20), regular);

        page.AddText("Bob", 12, new PdfPoint(50, top - 40), regular);
        page.AddText("25", 12, new PdfPoint(200, top - 40), regular);
        page.AddText("Paris", 12, new PdfPoint(300, top - 40), regular);

        File.WriteAllBytes(Path.Combine(directory, "tables.pdf"), builder.Build());
    }

    /// <summary>Body text with bold, italic, and bold-italic formatting.</summary>
    public static void GenerateEmphasis(string directory)
    {
        PdfDocumentBuilder builder = new();
        PdfDocumentBuilder.AddedFont regular = builder.AddStandard14Font(Standard14Font.Helvetica);
        PdfDocumentBuilder.AddedFont bold = builder.AddStandard14Font(Standard14Font.HelveticaBold);
        PdfDocumentBuilder.AddedFont italic = builder.AddStandard14Font(Standard14Font.HelveticaOblique);
        PdfDocumentBuilder.AddedFont boldItalic = builder.AddStandard14Font(Standard14Font.HelveticaBoldOblique);

        PdfPageBuilder page = builder.AddPage(PageSize.A4);
        double top = page.PageSize.Top - 50;

        // Line 1: regular text followed closely by bold text
        page.AddText("This is regular text and", 12, new PdfPoint(50, top), regular);
        page.AddText("bold text", 12, new PdfPoint(210, top), bold);

        // Line 2: italic text followed by regular (different X offset to avoid grid)
        page.AddText("Also some", 12, new PdfPoint(50, top - 20), regular);
        page.AddText("italic text", 12, new PdfPoint(115, top - 20), italic);
        page.AddText("mixed in.", 12, new PdfPoint(180, top - 20), regular);

        // Line 3: bold-italic
        page.AddText("And", 12, new PdfPoint(50, top - 40), regular);
        page.AddText("bold italic text", 12, new PdfPoint(80, top - 40), boldItalic);
        page.AddText("here.", 12, new PdfPoint(175, top - 40), regular);

        File.WriteAllBytes(Path.Combine(directory, "emphasis.pdf"), builder.Build());
    }

    /// <summary>Comprehensive document with all content types.</summary>
    public static void GenerateMixedContent(string directory)
    {
        PdfDocumentBuilder builder = new();
        PdfDocumentBuilder.AddedFont regular = builder.AddStandard14Font(Standard14Font.Helvetica);
        PdfDocumentBuilder.AddedFont bold = builder.AddStandard14Font(Standard14Font.HelveticaBold);
        PdfDocumentBuilder.AddedFont italic = builder.AddStandard14Font(Standard14Font.HelveticaOblique);

        PdfPageBuilder page = builder.AddPage(PageSize.A4);
        double top = page.PageSize.Top - 50;

        // Heading
        page.AddText("Chapter One", 24, new PdfPoint(50, top), bold);

        // Paragraph
        page.AddText("This is an introductory paragraph with", 12, new PdfPoint(50, top - 40), regular);
        page.AddText("emphasized", 12, new PdfPoint(310, top - 40), italic);
        page.AddText("content.", 12, new PdfPoint(380, top - 40), regular);

        // List
        page.AddText("\u2022 First item", 12, new PdfPoint(50, top - 70), regular);
        page.AddText("\u2022 Second item", 12, new PdfPoint(50, top - 88), regular);

        // Sub-heading
        page.AddText("Section 1.1", 18, new PdfPoint(50, top - 120), bold);

        // More body
        page.AddText("Body text under the sub-section heading.", 12, new PdfPoint(50, top - 150), regular);

        File.WriteAllBytes(Path.Combine(directory, "mixed-content.pdf"), builder.Build());
    }

    /// <summary>A PDF with no text layer (just a drawn rectangle to represent an image).</summary>
    public static void GenerateNoText(string directory)
    {
        PdfDocumentBuilder builder = new();
        PdfPageBuilder page = builder.AddPage(PageSize.A4);
        // Draw a filled rectangle to simulate an image page — no text at all
        page.DrawRectangle(new PdfPoint(50, 400), 200, 200);

        File.WriteAllBytes(Path.Combine(directory, "no-text.pdf"), builder.Build());
    }

    /// <summary>A valid PDF with a single empty page (no content).</summary>
    public static void GenerateEmpty(string directory)
    {
        PdfDocumentBuilder builder = new();
        builder.AddPage(PageSize.A4); // Empty page

        File.WriteAllBytes(Path.Combine(directory, "empty.pdf"), builder.Build());
    }

    /// <summary>PDF with 6 distinct heading sizes plus body text.</summary>
    public static void GenerateSixHeadingLevels(string directory)
    {
        PdfDocumentBuilder builder = new();
        PdfDocumentBuilder.AddedFont regular = builder.AddStandard14Font(Standard14Font.Helvetica);
        PdfDocumentBuilder.AddedFont bold = builder.AddStandard14Font(Standard14Font.HelveticaBold);

        PdfPageBuilder page = builder.AddPage(PageSize.A4);
        double top = page.PageSize.Top - 50;

        page.AddText("Heading Level 1", 36, new PdfPoint(50, top), bold);
        page.AddText("Body text.", 12, new PdfPoint(50, top - 50), regular);
        page.AddText("Heading Level 2", 30, new PdfPoint(50, top - 80), bold);
        page.AddText("Body text.", 12, new PdfPoint(50, top - 110), regular);
        page.AddText("Heading Level 3", 24, new PdfPoint(50, top - 140), bold);
        page.AddText("Body text.", 12, new PdfPoint(50, top - 170), regular);
        page.AddText("Heading Level 4", 20, new PdfPoint(50, top - 200), bold);
        page.AddText("Body text.", 12, new PdfPoint(50, top - 230), regular);
        page.AddText("Heading Level 5", 16, new PdfPoint(50, top - 260), bold);
        page.AddText("Body text.", 12, new PdfPoint(50, top - 290), regular);
        page.AddText("Heading Level 6", 14, new PdfPoint(50, top - 320), bold);
        page.AddText("Body text.", 12, new PdfPoint(50, top - 350), regular);

        File.WriteAllBytes(Path.Combine(directory, "six-heading-levels.pdf"), builder.Build());
    }

    /// <summary>Heading with italic words to test bold suppression and italic preservation.</summary>
    public static void GenerateHeadingWithEmphasis(string directory)
    {
        PdfDocumentBuilder builder = new();
        PdfDocumentBuilder.AddedFont regular = builder.AddStandard14Font(Standard14Font.Helvetica);
        PdfDocumentBuilder.AddedFont bold = builder.AddStandard14Font(Standard14Font.HelveticaBold);
        PdfDocumentBuilder.AddedFont italic = builder.AddStandard14Font(Standard14Font.HelveticaOblique);
        PdfDocumentBuilder.AddedFont boldItalic = builder.AddStandard14Font(Standard14Font.HelveticaBoldOblique);

        PdfPageBuilder page = builder.AddPage(PageSize.A4);
        double top = page.PageSize.Top - 50;

        // Heading 1: all bold (bold should be suppressed)
        page.AddText("Bold Heading Only", 24, new PdfPoint(50, top), bold);
        page.AddText("Body text here.", 12, new PdfPoint(50, top - 40), regular);

        // Heading 2: bold with italic word
        page.AddText("Heading with", 20, new PdfPoint(50, top - 70), bold);
        page.AddText("italic", 20, new PdfPoint(190, top - 70), boldItalic);
        page.AddText("word", 20, new PdfPoint(240, top - 70), bold);
        page.AddText("Body text here.", 12, new PdfPoint(50, top - 100), regular);

        File.WriteAllBytes(Path.Combine(directory, "heading-with-emphasis.pdf"), builder.Build());
    }

    /// <summary>PDF with nested lists (unordered nested under unordered).</summary>
    public static void GenerateNestedLists(string directory)
    {
        PdfDocumentBuilder builder = new();
        PdfDocumentBuilder.AddedFont regular = builder.AddStandard14Font(Standard14Font.Helvetica);

        PdfPageBuilder page = builder.AddPage(PageSize.A4);
        double top = page.PageSize.Top - 50;

        // Top-level items at X=50
        page.AddText("\u2022 Top item one", 12, new PdfPoint(50, top), regular);
        // Nested items at X=80 (indented by 30pt)
        page.AddText("\u2022 Nested item A", 12, new PdfPoint(80, top - 18), regular);
        page.AddText("\u2022 Nested item B", 12, new PdfPoint(80, top - 36), regular);
        page.AddText("\u2022 Top item two", 12, new PdfPoint(50, top - 54), regular);

        File.WriteAllBytes(Path.Combine(directory, "nested-lists.pdf"), builder.Build());
    }

    /// <summary>PDF with a list item that has a continuation line.</summary>
    public static void GenerateMultiLineListItems(string directory)
    {
        PdfDocumentBuilder builder = new();
        PdfDocumentBuilder.AddedFont regular = builder.AddStandard14Font(Standard14Font.Helvetica);

        PdfPageBuilder page = builder.AddPage(PageSize.A4);
        double top = page.PageSize.Top - 50;

        // First item with continuation
        page.AddText("\u2022 First item start", 12, new PdfPoint(50, top), regular);
        // Continuation line at approximately the same X as content start (~62pt)
        page.AddText("continuation of first", 12, new PdfPoint(62, top - 18), regular);
        // Second item
        page.AddText("\u2022 Second item", 12, new PdfPoint(50, top - 36), regular);

        File.WriteAllBytes(Path.Combine(directory, "multi-line-list.pdf"), builder.Build());
    }

    /// <summary>Table with bold and italic text in cells.</summary>
    public static void GenerateTableWithEmphasis(string directory)
    {
        PdfDocumentBuilder builder = new();
        PdfDocumentBuilder.AddedFont regular = builder.AddStandard14Font(Standard14Font.Helvetica);
        PdfDocumentBuilder.AddedFont bold = builder.AddStandard14Font(Standard14Font.HelveticaBold);
        PdfDocumentBuilder.AddedFont italic = builder.AddStandard14Font(Standard14Font.HelveticaOblique);

        PdfPageBuilder page = builder.AddPage(PageSize.A4);
        double top = page.PageSize.Top - 50;

        // Header
        page.AddText("Feature", 12, new PdfPoint(50, top), bold);
        page.AddText("Status", 12, new PdfPoint(250, top), bold);

        // Row 1 with italic cell
        page.AddText("Login", 12, new PdfPoint(50, top - 20), regular);
        page.AddText("Done", 12, new PdfPoint(250, top - 20), italic);

        // Row 2
        page.AddText("Signup", 12, new PdfPoint(50, top - 40), regular);
        page.AddText("Pending", 12, new PdfPoint(250, top - 40), bold);

        File.WriteAllBytes(Path.Combine(directory, "table-with-emphasis.pdf"), builder.Build());
    }

    /// <summary>Table where data rows have fewer columns than header.</summary>
    public static void GenerateTableWithVaryingColumns(string directory)
    {
        PdfDocumentBuilder builder = new();
        PdfDocumentBuilder.AddedFont regular = builder.AddStandard14Font(Standard14Font.Helvetica);
        PdfDocumentBuilder.AddedFont bold = builder.AddStandard14Font(Standard14Font.HelveticaBold);

        PdfPageBuilder page = builder.AddPage(PageSize.A4);
        double top = page.PageSize.Top - 50;

        // Header: 3 columns
        page.AddText("Col1", 12, new PdfPoint(50, top), bold);
        page.AddText("Col2", 12, new PdfPoint(200, top), bold);
        page.AddText("Col3", 12, new PdfPoint(350, top), bold);

        // Row 1: 3 columns
        page.AddText("A", 12, new PdfPoint(50, top - 20), regular);
        page.AddText("B", 12, new PdfPoint(200, top - 20), regular);
        page.AddText("C", 12, new PdfPoint(350, top - 20), regular);

        // Row 2: only 2 columns (missing Col3)
        page.AddText("D", 12, new PdfPoint(50, top - 40), regular);
        page.AddText("E", 12, new PdfPoint(200, top - 40), regular);

        File.WriteAllBytes(Path.Combine(directory, "table-varying-columns.pdf"), builder.Build());
    }

    /// <summary>PDF with all text at the same font size — no headings.</summary>
    public static void GenerateNoHeadings(string directory)
    {
        PdfDocumentBuilder builder = new();
        PdfDocumentBuilder.AddedFont regular = builder.AddStandard14Font(Standard14Font.Helvetica);

        PdfPageBuilder page = builder.AddPage(PageSize.A4);
        double top = page.PageSize.Top - 50;

        page.AddText("First line of text at normal size.", 12, new PdfPoint(50, top), regular);
        page.AddText("Second line of text at normal size.", 12, new PdfPoint(50, top - 20), regular);
        page.AddText("Third line of text at normal size.", 12, new PdfPoint(50, top - 40), regular);

        File.WriteAllBytes(Path.Combine(directory, "no-headings.pdf"), builder.Build());
    }

    /// <summary>List items containing bold and italic words.</summary>
    public static void GenerateListWithEmphasis(string directory)
    {
        PdfDocumentBuilder builder = new();
        PdfDocumentBuilder.AddedFont regular = builder.AddStandard14Font(Standard14Font.Helvetica);
        PdfDocumentBuilder.AddedFont bold = builder.AddStandard14Font(Standard14Font.HelveticaBold);
        PdfDocumentBuilder.AddedFont italic = builder.AddStandard14Font(Standard14Font.HelveticaOblique);

        PdfPageBuilder page = builder.AddPage(PageSize.A4);
        double top = page.PageSize.Top - 50;

        // Bullet item with a bold word
        page.AddText("\u2022 Item with", 12, new PdfPoint(50, top), regular);
        page.AddText("bold", 12, new PdfPoint(130, top), bold);
        page.AddText("word", 12, new PdfPoint(162, top), regular);

        // Bullet item with an italic word
        page.AddText("\u2022 Item with", 12, new PdfPoint(50, top - 18), regular);
        page.AddText("italic", 12, new PdfPoint(130, top - 18), italic);
        page.AddText("word", 12, new PdfPoint(165, top - 18), regular);

        File.WriteAllBytes(Path.Combine(directory, "list-with-emphasis.pdf"), builder.Build());
    }

    /// <summary>
    /// Two-column layout: left column has text, right column has text.
    /// Words from both columns share the same Y positions to simulate a real
    /// multi-column PDF where the content stream may interleave columns.
    /// </summary>
    public static void GenerateTwoColumn(string directory)
    {
        PdfDocumentBuilder builder = new();
        PdfDocumentBuilder.AddedFont regular = builder.AddStandard14Font(Standard14Font.Helvetica);

        PdfPageBuilder page = builder.AddPage(PageSize.A4);
        double top = page.PageSize.Top - 50;

        // Left column at X=50, right column at X=320.
        // The gap between columns (~150pt) is well above the 60pt threshold.
        // Interleave the writing order to simulate a real PDF content stream.
        for (int row = 0; row < 8; row++)
        {
            double y = top - (row * 20);
            // Write right column first (simulating interleaved content stream)
            page.AddText($"Right column line {row + 1}.", 12, new PdfPoint(320, y), regular);
            // Then left column
            page.AddText($"Left column line {row + 1}.", 12, new PdfPoint(50, y), regular);
        }

        File.WriteAllBytes(Path.Combine(directory, "two-column.pdf"), builder.Build());
    }

    /// <summary>
    /// Two-column layout with a full-width heading spanning both columns
    /// at the top, followed by two-column body content.
    /// </summary>
    public static void GenerateTwoColumnWithFullWidthHeader(string directory)
    {
        PdfDocumentBuilder builder = new();
        PdfDocumentBuilder.AddedFont regular = builder.AddStandard14Font(Standard14Font.Helvetica);
        PdfDocumentBuilder.AddedFont bold = builder.AddStandard14Font(Standard14Font.HelveticaBold);

        PdfPageBuilder page = builder.AddPage(PageSize.A4);
        double top = page.PageSize.Top - 50;

        // Full-width heading (no gap = spans both column regions)
        page.AddText("Full Width Heading", 24, new PdfPoint(50, top), bold);

        // Two-column body content below the heading
        for (int row = 0; row < 6; row++)
        {
            double y = top - 50 - (row * 20);
            page.AddText($"Left body line {row + 1}.", 12, new PdfPoint(50, y), regular);
            page.AddText($"Right body line {row + 1}.", 12, new PdfPoint(320, y), regular);
        }

        File.WriteAllBytes(Path.Combine(directory, "two-column-with-header.pdf"), builder.Build());
    }

    /// <summary>
    /// PDF with headings that are short all-bold lines at body font size.
    /// These should be detected as style-based headings.
    /// </summary>
    public static void GenerateBoldHeadings(string directory)
    {
        PdfDocumentBuilder builder = new();
        PdfDocumentBuilder.AddedFont regular = builder.AddStandard14Font(Standard14Font.Helvetica);
        PdfDocumentBuilder.AddedFont bold = builder.AddStandard14Font(Standard14Font.HelveticaBold);

        PdfPageBuilder page = builder.AddPage(PageSize.A4);
        double top = page.PageSize.Top - 50;

        // Bold heading at body size
        page.AddText("The First Section", 12, new PdfPoint(50, top), bold);
        // Body paragraph
        page.AddText("This is body text under the first section heading.", 12, new PdfPoint(50, top - 25), regular);
        page.AddText("More body content continues here.", 12, new PdfPoint(50, top - 45), regular);

        // Another bold heading
        page.AddText("The Second Section", 12, new PdfPoint(50, top - 75), bold);
        page.AddText("Body text under the second section.", 12, new PdfPoint(50, top - 100), regular);

        File.WriteAllBytes(Path.Combine(directory, "bold-headings.pdf"), builder.Build());
    }

    /// <summary>
    /// PDF with headings in a different font family from body text.
    /// Uses Times for headings and Helvetica for body.
    /// </summary>
    public static void GenerateMixedFontHeadings(string directory)
    {
        PdfDocumentBuilder builder = new();
        PdfDocumentBuilder.AddedFont bodyFont = builder.AddStandard14Font(Standard14Font.Helvetica);
        PdfDocumentBuilder.AddedFont headingFont = builder.AddStandard14Font(Standard14Font.TimesRoman);

        PdfPageBuilder page = builder.AddPage(PageSize.A4);
        double top = page.PageSize.Top - 50;

        // Heading in Times (different family from body)
        page.AddText("Chapter Overview", 12, new PdfPoint(50, top), headingFont);
        // Body in Helvetica
        page.AddText("This paragraph uses the standard body font.", 12, new PdfPoint(50, top - 25), bodyFont);
        page.AddText("Additional body text for context.", 12, new PdfPoint(50, top - 45), bodyFont);

        // Another heading in Times
        page.AddText("Key Principles", 12, new PdfPoint(50, top - 75), headingFont);
        page.AddText("More body text follows.", 12, new PdfPoint(50, top - 100), bodyFont);

        File.WriteAllBytes(Path.Combine(directory, "mixed-font-headings.pdf"), builder.Build());
    }
}
