using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PdfToMarkdown.Services;

/// <summary>
/// Reads a text-based PDF file, extracts its content, classifies structural
/// elements, and converts them directly to Markdown during parsing.
/// Writes converted content incrementally through the provided <see cref="IMarkdownWriter"/>.
/// </summary>
internal sealed partial class PdfMarkdownConverter(ILogger<PdfMarkdownConverter> logger) : IPdfMarkdownConverter
{
    private readonly ILogger<PdfMarkdownConverter> _logger = logger;

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting PDF conversion: {PdfPath}")]
    private partial void LogConversionStart(string pdfPath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Pre-analysis complete: bodyFontSize={BodyFontSize}, bodyFontFamily={BodyFontFamily}, headingLevels={HeadingLevelCount}")]
    private partial void LogPreAnalysisComplete(double bodyFontSize, string bodyFontFamily, int headingLevelCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Completed PDF conversion: {PdfPath} — {PageCount} pages")]
    private partial void LogConversionComplete(string pdfPath, int pageCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Page {PageNumber}: detected {BlockCount} spatial text block(s)")]
    private partial void LogBlockDetection(int pageNumber, int blockCount);

    /// <summary>
    /// Regex matching ordered list prefixes like "1.", "2)", "a.", "b)", etc.
    /// </summary>
    [GeneratedRegex(@"^(\d+[.)\]]|[a-zA-Z][.)\]])\s", RegexOptions.Compiled)]
    private static partial Regex OrderedListPrefixRegex();

    /// <summary>
    /// Characters treated as unordered list bullet markers.
    /// </summary>
    private static readonly char[] BulletChars = ['•', '‣', '▪', '-', '*', '–', '◦', '●'];

    /// <summary>
    /// Regex matching two or more consecutive whitespace characters.
    /// </summary>
    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex MultipleWhitespaceRegex();

    /// <summary>
    /// Minimum horizontal gap (in PDF points) between words to consider them
    /// as belonging to separate table columns.
    /// </summary>
    private const double ColumnGapThreshold = 60.0;

    /// <summary>
    /// Maximum difference in X position (in PDF points) for column starts
    /// across rows to be considered aligned.
    /// </summary>
    private const double ColumnAlignmentTolerance = 20.0;

    /// <summary>
    /// Maximum number of words on a line for it to qualify as a style-based heading.
    /// </summary>
    private const int MaxStyleHeadingWords = 8;

    /// <inheritdoc />
    public async Task ConvertAsync(string pdfPath, IMarkdownWriter writer, CancellationToken cancellationToken = default)
    {
        LogConversionStart(pdfPath);

        PdfDocument document;
        try
        {
            document = PdfDocument.Open(pdfPath);
        }
        catch (FileNotFoundException)
        {
            throw new McpException($"PDF file not found: {pdfPath}");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("No file exists", StringComparison.OrdinalIgnoreCase))
        {
            throw new McpException($"PDF file not found: {pdfPath}");
        }
        catch (IOException ex)
        {
            throw new McpException($"Cannot read PDF file: {pdfPath} — {ex.Message}");
        }
        catch (Exception ex) when (ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase)
                                   || ex.Message.Contains("encrypt", StringComparison.OrdinalIgnoreCase))
        {
            throw new McpException($"PDF is encrypted or password-protected and cannot be processed: {pdfPath}");
        }

        using (document)
        {
            // Pre-analysis pass: collect font size and name statistics
            List<double> allFontSizes = [];
            List<(double FontSize, string FontName)> allFontData = [];
            for (int i = 1; i <= document.NumberOfPages; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Page page = document.GetPage(i);
                foreach (Letter letter in page.Letters)
                {
                    double size = Math.Round(letter.FontSize, 1);
                    allFontSizes.Add(size);
                    allFontData.Add((size, letter.FontName ?? ""));
                }
            }

            double bodyFontSize = DetermineBodyFontSize(allFontSizes);
            string bodyFontFamily = DetermineBodyFontFamily(allFontData, bodyFontSize);
            Dictionary<int, int> headingLevelMap = BuildHeadingLevelMap(allFontSizes, bodyFontSize);
            int styleHeadingLevel = Math.Min(
                headingLevelMap.Count > 0 ? headingLevelMap.Values.Max() + 1 : 2,
                6);
            LogPreAnalysisComplete(bodyFontSize, bodyFontFamily, headingLevelMap.Count);

            // Conversion pass: process each page and write Markdown incrementally
            bool hasAnyText = false;

            for (int i = 1; i <= document.NumberOfPages; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Page page = document.GetPage(i);
                IEnumerable<Word> words = page.GetWords();
                List<TextLine> rawLines = GroupWordsIntoLines(words);

                if (rawLines.Count > 0)
                {
                    hasAnyText = true;
                }

                // Spatial block analysis: split lines into segments, cluster into blocks,
                // then sort blocks for correct reading order (handles columns, headings, etc.)
                double adaptiveGap = CalculateAdaptiveColumnGap(rawLines);
                List<TextBlock> blocks = BuildSpatialBlocks(rawLines, adaptiveGap);
                blocks = SortBlocksForReadingOrder(blocks);

                if (blocks.Count > 1)
                {
                    LogBlockDetection(i, blocks.Count);
                }

                StringBuilder pageBuilder = new();
                foreach (TextBlock block in blocks)
                {
                    string blockMarkdown = ConvertPageToMarkdown(block.Lines, bodyFontSize, headingLevelMap, bodyFontFamily, styleHeadingLevel);
                    if (!string.IsNullOrEmpty(blockMarkdown))
                    {
                        pageBuilder.Append(blockMarkdown);
                    }
                }

                string pageMarkdown = pageBuilder.ToString();

                if (!string.IsNullOrEmpty(pageMarkdown))
                {
                    await writer.WritePageAsync(pageMarkdown, cancellationToken).ConfigureAwait(false);
                }
            }

            if (!hasAnyText)
            {
                throw new McpException($"PDF contains no extractable text content: {pdfPath}");
            }

            LogConversionComplete(pdfPath, document.NumberOfPages);
        }
    }

    /// <summary>
    /// Converts classified lines from a single page into Markdown syntax.
    /// </summary>
    private static string ConvertPageToMarkdown(List<TextLine> lines, double bodyFontSize,
        Dictionary<int, int> headingLevelMap, string bodyFontFamily, int styleHeadingLevel)
    {
        if (lines.Count == 0)
        {
            return "";
        }

        StringBuilder sb = new();
        int i = 0;

        while (i < lines.Count)
        {
            TextLine line = lines[i];
            string lineText = line.GetText();
            double lineFontSize = line.GetPrimaryFontSize();
            int fontKey = (int)(Math.Round(lineFontSize, 1) * 10);

            // 1. Check for heading (by font size)
            if (headingLevelMap.TryGetValue(fontKey, out int headingLevel))
            {
                string headingPrefix = new('#', headingLevel);
                string headingText = CollapseWhitespace(FormatHeadingEmphasis(line.Words));
                sb.AppendLine();
                sb.Append(headingPrefix);
                sb.Append(' ');
                sb.AppendLine(headingText);
                sb.AppendLine();
                i++;
                continue;
            }

            // 2. Check for table (look-ahead at multiple lines — before style headings
            //    to avoid misidentifying bold table headers as headings)
            if (TryDetectTable(lines, i, headingLevelMap, out int tableEnd, out List<List<List<Word>>> tableRows))
            {
                EmitTable(sb, tableRows);
                i = tableEnd;
                continue;
            }

            // 3. Check for style-based heading (bold or different font family, short line)
            if (IsStyleBasedHeading(line, bodyFontSize, headingLevelMap, bodyFontFamily))
            {
                string headingPrefix = new('#', styleHeadingLevel);
                string headingText = CollapseWhitespace(FormatHeadingEmphasis(line.Words));
                sb.AppendLine();
                sb.Append(headingPrefix);
                sb.Append(' ');
                sb.AppendLine(headingText);
                sb.AppendLine();
                i++;
                continue;
            }

            // 4. Check for list items (unordered or ordered)
            if (IsUnorderedListItem(lineText) || IsOrderedListItem(lineText))
            {
                sb.AppendLine();
                EmitListItems(sb, lines, ref i, headingLevelMap);
                sb.AppendLine();
                continue;
            }

            // 4. Default: paragraph — collect consecutive body-text lines
            StringBuilder paragraphText = new();
            while (i < lines.Count)
            {
                TextLine pLine = lines[i];
                double pFontSize = pLine.GetPrimaryFontSize();
                int pFontKey = (int)(Math.Round(pFontSize, 1) * 10);
                string pText = pLine.GetText();

                if (headingLevelMap.ContainsKey(pFontKey) || IsUnorderedListItem(pText) || IsOrderedListItem(pText))
                {
                    break;
                }

                // Stop at style-based headings
                if (IsStyleBasedHeading(pLine, bodyFontSize, headingLevelMap, bodyFontFamily))
                {
                    break;
                }

                // Stop if we hit a table region
                if (TryDetectTable(lines, i, headingLevelMap, out _, out _))
                {
                    break;
                }

                if (paragraphText.Length > 0)
                {
                    paragraphText.Append(' ');
                }

                paragraphText.Append(FormatInlineEmphasis(pLine.Words));
                i++;
            }

            if (paragraphText.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine(paragraphText.ToString());
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats inline emphasis (bold, italic, bold-italic) for a sequence of words.
    /// Groups consecutive words with the same font properties and wraps them
    /// in Markdown emphasis syntax.
    /// </summary>
    private static string FormatInlineEmphasis(List<Word> words)
    {
        if (words.Count == 0)
        {
            return "";
        }

        StringBuilder sb = new();
        bool? lastBold = null;
        bool? lastItalic = null;
        List<string> currentTexts = [];

        foreach (Word word in words)
        {
            bool isBold = IsBoldFont(word);
            bool isItalic = IsItalicFont(word);

            if (lastBold is not null && (isBold != lastBold || isItalic != lastItalic))
            {
                FlushEmphasis(sb, currentTexts, lastBold.Value, lastItalic!.Value);
                currentTexts = [];
            }

            lastBold = isBold;
            lastItalic = isItalic;
            currentTexts.Add(word.Text);
        }

        if (currentTexts.Count > 0 && lastBold is not null)
        {
            FlushEmphasis(sb, currentTexts, lastBold.Value, lastItalic!.Value);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Writes accumulated text with appropriate Markdown emphasis wrapping.
    /// </summary>
    private static void FlushEmphasis(StringBuilder sb, List<string> texts, bool isBold, bool isItalic)
    {
        if (texts.Count == 0)
        {
            return;
        }

        if (sb.Length > 0)
        {
            sb.Append(' ');
        }

        string text = string.Join(" ", texts);

        if (isBold && isItalic)
        {
            sb.Append("***");
            sb.Append(text);
            sb.Append("***");
        }
        else if (isBold)
        {
            sb.Append("**");
            sb.Append(text);
            sb.Append("**");
        }
        else if (isItalic)
        {
            sb.Append('*');
            sb.Append(text);
            sb.Append('*');
        }
        else
        {
            sb.Append(text);
        }
    }

    /// <summary>
    /// Formats inline emphasis for heading context. Bold is suppressed since
    /// headings are inherently bold in Markdown. Italic is preserved.
    /// Bold-italic is rendered as italic only.
    /// </summary>
    private static string FormatHeadingEmphasis(List<Word> words)
    {
        if (words.Count == 0)
        {
            return "";
        }

        StringBuilder sb = new();
        bool? lastItalic = null;
        List<string> currentTexts = [];

        foreach (Word word in words)
        {
            bool isItalic = IsItalicFont(word);

            if (lastItalic is not null && isItalic != lastItalic)
            {
                FlushHeadingEmphasis(sb, currentTexts, lastItalic.Value);
                currentTexts = [];
            }

            lastItalic = isItalic;
            currentTexts.Add(word.Text);
        }

        if (currentTexts.Count > 0 && lastItalic is not null)
        {
            FlushHeadingEmphasis(sb, currentTexts, lastItalic.Value);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Writes accumulated heading text with appropriate Markdown emphasis wrapping.
    /// Bold is suppressed; only italic is applied.
    /// </summary>
    private static void FlushHeadingEmphasis(StringBuilder sb, List<string> texts, bool isItalic)
    {
        if (texts.Count == 0)
        {
            return;
        }

        if (sb.Length > 0)
        {
            sb.Append(' ');
        }

        string text = string.Join(" ", texts);

        if (isItalic)
        {
            sb.Append('*');
            sb.Append(text);
            sb.Append('*');
        }
        else
        {
            sb.Append(text);
        }
    }

    /// <summary>
    /// Trims leading/trailing whitespace and collapses multiple internal
    /// whitespace characters to a single space.
    /// </summary>
    private static string CollapseWhitespace(string text)
    {
        return MultipleWhitespaceRegex().Replace(text.Trim(), " ");
    }

    /// <summary>
    /// Emits list items (unordered and ordered) with nesting, emphasis, and
    /// multi-line continuation support.
    /// </summary>
    private static void EmitListItems(StringBuilder sb, List<TextLine> lines, ref int i,
        Dictionary<int, int> headingLevelMap)
    {
        double baseX = lines[i].GetMinX();
        Dictionary<int, int> orderedCounters = [];

        while (i < lines.Count)
        {
            TextLine line = lines[i];
            string lineText = line.GetText();
            double fontSize = line.GetPrimaryFontSize();
            int fontKey = (int)(Math.Round(fontSize, 1) * 10);

            // Stop at headings
            if (headingLevelMap.ContainsKey(fontKey))
            {
                break;
            }

            bool isUnordered = IsUnorderedListItem(lineText);
            bool isOrdered = IsOrderedListItem(lineText);

            if (!isUnordered && !isOrdered)
            {
                break;
            }

            // Calculate nesting level based on X position
            double lineX = line.GetMinX();
            int nestLevel = (int)Math.Max(0, (lineX - baseX + 10) / 30.0);
            string indent = new(' ', nestLevel * 2);

            // Get content words (skip prefix) and format with emphasis
            List<Word> contentWords = GetListItemContentWords(line);
            string itemText = FormatInlineEmphasis(contentWords);

            // Check for continuation lines (multi-line list items)
            double contentStartX = line.Words.Count > 1
                ? line.Words[1].BoundingBox.Left
                : lineX + 15;

            while (i + 1 < lines.Count)
            {
                TextLine nextLine = lines[i + 1];
                string nextText = nextLine.GetText();
                int nextFontKey = (int)(Math.Round(nextLine.GetPrimaryFontSize(), 1) * 10);

                if (IsUnorderedListItem(nextText) || IsOrderedListItem(nextText) ||
                    headingLevelMap.ContainsKey(nextFontKey))
                {
                    break;
                }

                // Continuation: indented at or past content start
                double nextX = nextLine.GetMinX();
                if (Math.Abs(nextX - contentStartX) < 10)
                {
                    itemText += " " + FormatInlineEmphasis(nextLine.Words);
                    i++;
                }
                else
                {
                    break;
                }
            }

            if (isUnordered)
            {
                sb.Append(indent);
                sb.Append("- ");
                sb.AppendLine(itemText);
            }
            else
            {
                if (!orderedCounters.TryGetValue(nestLevel, out int counter))
                {
                    counter = 0;
                }

                counter++;
                orderedCounters[nestLevel] = counter;

                sb.Append(indent);
                sb.Append(counter);
                sb.Append(". ");
                sb.AppendLine(itemText);
            }

            i++;
        }
    }

    /// <summary>
    /// Gets the content words of a list item, skipping the bullet or number prefix word.
    /// </summary>
    private static List<Word> GetListItemContentWords(TextLine line)
    {
        // The first word is the bullet character or number prefix — skip it
        return line.Words.Count > 1 ? line.Words.Skip(1).ToList() : [];
    }

    /// <summary>
    /// Attempts to detect a table starting at the given line index.
    /// Returns true if a table is detected, with the end index and row data.
    /// </summary>
    private static bool TryDetectTable(List<TextLine> lines, int startIndex,
        Dictionary<int, int> headingLevelMap,
        out int endIndex, out List<List<List<Word>>> tableRows)
    {
        endIndex = startIndex;
        tableRows = [];

        // Collect consecutive body-text lines that could be table rows
        List<List<List<Word>>> candidates = [];

        for (int idx = startIndex; idx < lines.Count; idx++)
        {
            TextLine line = lines[idx];
            string lineText = line.GetText();
            double fontSize = line.GetPrimaryFontSize();
            int fontKey = (int)(Math.Round(fontSize, 1) * 10);

            // Stop at headings or list items
            if (headingLevelMap.ContainsKey(fontKey) ||
                IsUnorderedListItem(lineText) || IsOrderedListItem(lineText))
            {
                break;
            }

            List<List<Word>> cells = GroupWordsIntoCells(line);
            if (cells.Count < 2)
            {
                break;
            }

            candidates.Add(cells);
        }

        if (candidates.Count < 2)
        {
            return false;
        }

        // Verify column alignment across rows
        List<double> firstColXPositions = candidates
            .Where(row => row.Count > 0 && row[0].Count > 0)
            .Select(row => row[0][0].BoundingBox.Left)
            .ToList();

        if (firstColXPositions.Count >= 2)
        {
            double range = firstColXPositions.Max() - firstColXPositions.Min();
            if (range > ColumnAlignmentTolerance)
            {
                return false;
            }
        }

        endIndex = startIndex + candidates.Count;
        tableRows = candidates;
        return true;
    }

    /// <summary>
    /// Groups words in a line into cells based on horizontal gaps exceeding the column threshold.
    /// </summary>
    private static List<List<Word>> GroupWordsIntoCells(TextLine line)
    {
        List<Word> sorted = [.. line.Words.OrderBy(w => w.BoundingBox.Left)];
        List<List<Word>> cells = [[]];
        Word? prevWord = null;

        foreach (Word word in sorted)
        {
            if (prevWord is not null)
            {
                double gap = word.BoundingBox.Left - prevWord.BoundingBox.Right;
                if (gap > ColumnGapThreshold)
                {
                    cells.Add([]);
                }
            }

            cells[^1].Add(word);
            prevWord = word;
        }

        return cells;
    }

    /// <summary>
    /// Emits a Markdown pipe-delimited table.
    /// </summary>
    private static void EmitTable(StringBuilder sb, List<List<List<Word>>> tableRows)
    {
        if (tableRows.Count == 0)
        {
            return;
        }

        int columnCount = tableRows[0].Count;

        sb.AppendLine();

        // Header row
        EmitTableRow(sb, tableRows[0], columnCount);

        // Separator row
        sb.Append('|');
        for (int c = 0; c < columnCount; c++)
        {
            sb.Append("---|");
        }
        sb.AppendLine();

        // Data rows
        for (int r = 1; r < tableRows.Count; r++)
        {
            EmitTableRow(sb, tableRows[r], columnCount);
        }

        sb.AppendLine();
    }

    /// <summary>
    /// Emits a single table row. Pads shorter rows with empty cells,
    /// clips longer rows to the column count.
    /// </summary>
    private static void EmitTableRow(StringBuilder sb, List<List<Word>> cells, int columnCount)
    {
        sb.Append('|');
        for (int c = 0; c < columnCount; c++)
        {
            sb.Append(' ');
            if (c < cells.Count && cells[c].Count > 0)
            {
                string cellText = FormatInlineEmphasis(cells[c]);
                sb.Append(cellText.Trim());
            }
            sb.Append(" |");
        }
        sb.AppendLine();
    }

    /// <summary>
    /// Determines the most common font size in the document, assumed to be the body text size.
    /// </summary>
    private static double DetermineBodyFontSize(List<double> allFontSizes)
    {
        if (allFontSizes.Count == 0)
        {
            return 12.0;
        }

        return allFontSizes
            .GroupBy(s => s)
            .OrderByDescending(g => g.Count())
            .First()
            .Key;
    }

    /// <summary>
    /// Builds a mapping from font sizes larger than body text to heading levels 1–6.
    /// Largest distinct font size maps to H1, next to H2, etc.
    /// </summary>
    private static Dictionary<int, int> BuildHeadingLevelMap(List<double> allFontSizes, double bodyFontSize)
    {
        List<double> headingSizes = allFontSizes
            .Distinct()
            .Where(s => s > bodyFontSize + 0.5)
            .OrderByDescending(s => s)
            .Take(6)
            .ToList();

        Dictionary<int, int> map = [];
        for (int i = 0; i < headingSizes.Count; i++)
        {
            map[(int)(headingSizes[i] * 10)] = i + 1;
        }

        return map;
    }

    /// <summary>
    /// Groups PdfPig Words into lines based on vertical position proximity.
    /// Words are first sorted by Y (top-to-bottom) then X (left-to-right) to
    /// handle PDFs whose content stream order does not match reading order.
    /// </summary>
    private static List<TextLine> GroupWordsIntoLines(IEnumerable<Word> words)
    {
        List<Word> allWords = words.ToList();
        if (allWords.Count == 0)
        {
            return [];
        }

        // Sort by Y descending (top of page first in PDF coordinates), then X ascending
        allWords.Sort((a, b) =>
        {
            int yCompare = b.BoundingBox.Bottom.CompareTo(a.BoundingBox.Bottom);
            return yCompare != 0 ? yCompare : a.BoundingBox.Left.CompareTo(b.BoundingBox.Left);
        });

        List<TextLine> lines = [];
        TextLine currentLine = new(Math.Round(allWords[0].BoundingBox.Bottom, 1));
        currentLine.Words.Add(allWords[0]);
        lines.Add(currentLine);

        for (int i = 1; i < allWords.Count; i++)
        {
            double y = Math.Round(allWords[i].BoundingBox.Bottom, 1);

            if (Math.Abs(currentLine.Y - y) > 2.0)
            {
                currentLine = new TextLine(y);
                lines.Add(currentLine);
            }

            currentLine.Words.Add(allWords[i]);
        }

        return lines;
    }

    /// <summary>
    /// Builds spatial text blocks from page lines by splitting lines at horizontal gaps
    /// and clustering spatially close segments based on vertical proximity and horizontal overlap.
    /// This naturally handles multi-column layouts, heading isolation, and paragraph separation.
    /// </summary>
    private static List<TextBlock> BuildSpatialBlocks(List<TextLine> rawLines, double horizontalGapThreshold)
    {
        if (rawLines.Count == 0)
        {
            return [];
        }

        // 1. Detect consistent column boundaries — gaps at the same X position
        //    across many lines indicate column structure; table cell gaps which
        //    appear in only a few rows are ignored.
        List<double> columnBoundaries = DetectConsistentColumnBoundaries(rawLines, horizontalGapThreshold);

        // 2. Split each line into segments only at detected column boundaries
        List<TextLine> segments = [];
        foreach (TextLine line in rawLines)
        {
            segments.AddRange(SplitLineAtColumnBoundaries(line, horizontalGapThreshold, columnBoundaries));
        }

        // 3. Calculate typical line spacing for vertical gap threshold
        double typicalLineSpacing = CalculateTypicalLineSpacing(rawLines);
        double verticalGapThreshold = typicalLineSpacing * 1.5;

        // 4. Build blocks by assigning each segment to the best matching block
        List<TextBlock> blocks = [];

        foreach (TextLine segment in segments)
        {
            TextBlock? bestBlock = null;
            double bestVerticalDistance = double.MaxValue;

            foreach (TextBlock block in blocks)
            {
                // Vertical distance from the block's bottom line to the segment
                double verticalDistance = Math.Abs(block.MinY - segment.Y);

                if (verticalDistance <= verticalGapThreshold && verticalDistance < bestVerticalDistance)
                {
                    if (HasSignificantHorizontalOverlap(segment, block))
                    {
                        bestBlock = block;
                        bestVerticalDistance = verticalDistance;
                    }
                }
            }

            if (bestBlock is not null)
            {
                bestBlock.AddLine(segment);
            }
            else
            {
                TextBlock newBlock = new();
                newBlock.AddLine(segment);
                blocks.Add(newBlock);
            }
        }

        return blocks;
    }

    /// <summary>
    /// Detects consistent column boundaries by finding horizontal gap positions
    /// that appear across many lines. Only gap clusters with enough support
    /// are treated as real column structure — this prevents table cell gaps
    /// or incidental spacing from being misidentified as columns.
    /// </summary>
    private static List<double> DetectConsistentColumnBoundaries(List<TextLine> lines, double gapThreshold)
    {
        if (lines.Count < 4)
        {
            return [];
        }

        List<double> allGapCenters = [];
        int linesWithMultipleWords = 0;

        foreach (TextLine line in lines)
        {
            if (line.Words.Count < 2)
            {
                continue;
            }

            linesWithMultipleWords++;

            for (int w = 1; w < line.Words.Count; w++)
            {
                double gap = line.Words[w].BoundingBox.Left - line.Words[w - 1].BoundingBox.Right;
                if (gap > gapThreshold)
                {
                    double gapCenter = (line.Words[w - 1].BoundingBox.Right + line.Words[w].BoundingBox.Left) / 2.0;
                    allGapCenters.Add(gapCenter);
                }
            }
        }

        if (allGapCenters.Count == 0 || linesWithMultipleWords < 4)
        {
            return [];
        }

        // Cluster gap centers that are close together
        allGapCenters.Sort();
        List<List<double>> clusters = [];
        List<double> currentCluster = [allGapCenters[0]];

        for (int i = 1; i < allGapCenters.Count; i++)
        {
            if (allGapCenters[i] - currentCluster[^1] <= ColumnAlignmentTolerance)
            {
                currentCluster.Add(allGapCenters[i]);
            }
            else
            {
                clusters.Add(currentCluster);
                currentCluster = [allGapCenters[i]];
            }
        }

        clusters.Add(currentCluster);

        // Require strong support: at least 8 lines or 30% of multi-word lines,
        // whichever is larger. This avoids treating table cell gaps as column boundaries.
        double threshold = Math.Max(8, linesWithMultipleWords * 0.30);
        List<double> boundaries = [];

        foreach (List<double> cluster in clusters)
        {
            if (cluster.Count >= threshold)
            {
                boundaries.Add(cluster.Average());
            }
        }

        boundaries.Sort();
        return boundaries;
    }

    /// <summary>
    /// Splits a line into segments only at gaps that align with detected column boundaries.
    /// Gaps that don't align with column boundaries (e.g., table cell gaps) are preserved.
    /// </summary>
    private static List<TextLine> SplitLineAtColumnBoundaries(TextLine line, double gapThreshold, List<double> columnBoundaries)
    {
        if (line.Words.Count < 2 || columnBoundaries.Count == 0)
        {
            return [line];
        }

        List<Word> sorted = [.. line.Words.OrderBy(w => w.BoundingBox.Left)];
        List<int> splitPoints = [];

        for (int i = 1; i < sorted.Count; i++)
        {
            double gap = sorted[i].BoundingBox.Left - sorted[i - 1].BoundingBox.Right;
            if (gap > gapThreshold)
            {
                double gapCenter = (sorted[i - 1].BoundingBox.Right + sorted[i].BoundingBox.Left) / 2.0;

                // Only split if this gap aligns with a known column boundary
                if (columnBoundaries.Any(b => Math.Abs(gapCenter - b) <= ColumnAlignmentTolerance * 2))
                {
                    splitPoints.Add(i);
                }
            }
        }

        if (splitPoints.Count == 0)
        {
            return [line];
        }

        List<TextLine> segments = [];
        int start = 0;

        foreach (int splitAt in splitPoints)
        {
            TextLine segment = new(line.Y);
            segment.Words.AddRange(sorted.GetRange(start, splitAt - start));
            segments.Add(segment);
            start = splitAt;
        }

        TextLine lastSegment = new(line.Y);
        lastSegment.Words.AddRange(sorted.GetRange(start, sorted.Count - start));
        segments.Add(lastSegment);

        return segments;
    }

    /// <summary>
    /// Calculates the typical (median) vertical spacing between consecutive lines.
    /// Used to determine the threshold for spatial block separation.
    /// </summary>
    private static double CalculateTypicalLineSpacing(List<TextLine> lines)
    {
        if (lines.Count < 2)
        {
            return 14.0;
        }

        List<double> spacings = [];
        for (int i = 1; i < lines.Count; i++)
        {
            double spacing = Math.Abs(lines[i - 1].Y - lines[i].Y);
            if (spacing > 0.5 && spacing < 100)
            {
                spacings.Add(spacing);
            }
        }

        if (spacings.Count == 0)
        {
            return 14.0;
        }

        spacings.Sort();
        return spacings[spacings.Count / 2];
    }

    /// <summary>
    /// Determines whether a text segment has significant horizontal overlap with a block.
    /// Requires at least 30% overlap relative to the narrower element.
    /// </summary>
    private static bool HasSignificantHorizontalOverlap(TextLine segment, TextBlock block)
    {
        double segMinX = segment.Words.Min(w => w.BoundingBox.Left);
        double segMaxX = segment.Words.Max(w => w.BoundingBox.Right);
        double segWidth = segMaxX - segMinX;

        double overlapStart = Math.Max(segMinX, block.MinX);
        double overlapEnd = Math.Min(segMaxX, block.MaxX);
        double overlap = Math.Max(0, overlapEnd - overlapStart);

        double minWidth = Math.Min(segWidth, block.MaxX - block.MinX);

        return minWidth > 0 && overlap / minWidth > 0.3;
    }

    /// <summary>
    /// Sorts spatial blocks for correct reading order. Full-width blocks act as
    /// separators; between them, column blocks are read left column top-to-bottom,
    /// then right column top-to-bottom.
    /// </summary>
    private static List<TextBlock> SortBlocksForReadingOrder(List<TextBlock> blocks)
    {
        if (blocks.Count <= 1)
        {
            return blocks;
        }

        double pageLeft = blocks.Min(b => b.MinX);
        double pageRight = blocks.Max(b => b.MaxX);
        double pageWidth = pageRight - pageLeft;
        double pageMid = (pageLeft + pageRight) / 2.0;

        // Check if there's a column structure: at least some narrow blocks
        // on each side of the midpoint
        bool hasLeftColumn = blocks.Any(b => (b.MaxX - b.MinX) < pageWidth * 0.6 && (b.MinX + b.MaxX) / 2.0 < pageMid);
        bool hasRightColumn = blocks.Any(b => (b.MaxX - b.MinX) < pageWidth * 0.6 && (b.MinX + b.MaxX) / 2.0 >= pageMid);

        if (!hasLeftColumn || !hasRightColumn)
        {
            // No column structure — simple top-to-bottom sort
            return [.. blocks.OrderByDescending(b => b.MaxY)];
        }

        // Multi-column layout: process top-to-bottom, accumulate column blocks,
        // flush left-then-right when a full-width block or Y gap appears
        List<TextBlock> allSorted = [.. blocks.OrderByDescending(b => b.MaxY)];
        List<TextBlock> result = [];
        List<TextBlock> leftBatch = [];
        List<TextBlock> rightBatch = [];

        foreach (TextBlock block in allSorted)
        {
            double blockWidth = block.MaxX - block.MinX;
            double centerX = (block.MinX + block.MaxX) / 2.0;

            if (blockWidth > pageWidth * 0.6)
            {
                // Full-width block: flush column batches, then add this block
                FlushColumnBatches(result, leftBatch, rightBatch);
                result.Add(block);
            }
            else if (centerX < pageMid)
            {
                leftBatch.Add(block);
            }
            else
            {
                rightBatch.Add(block);
            }
        }

        FlushColumnBatches(result, leftBatch, rightBatch);
        return result;
    }

    /// <summary>
    /// Flushes accumulated column blocks into the result: left column first
    /// (top-to-bottom), then right column (top-to-bottom).
    /// </summary>
    private static void FlushColumnBatches(List<TextBlock> result, List<TextBlock> leftBatch, List<TextBlock> rightBatch)
    {
        // Batches are already in top-to-bottom order (from the outer loop)
        result.AddRange(leftBatch);
        result.AddRange(rightBatch);
        leftBatch.Clear();
        rightBatch.Clear();
    }

    /// <summary>
    /// Determines whether a word uses a bold font.
    /// </summary>
    private static bool IsBoldFont(Word word)
    {
        if (word.Letters.Count == 0)
        {
            return false;
        }

        string fontName = word.Letters[0].FontName ?? "";
        return fontName.Contains("Bold", StringComparison.OrdinalIgnoreCase)
               || fontName.Contains("Heavy", StringComparison.OrdinalIgnoreCase)
               || fontName.Contains("Black", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether a word uses an italic font.
    /// </summary>
    private static bool IsItalicFont(Word word)
    {
        if (word.Letters.Count == 0)
        {
            return false;
        }

        string fontName = word.Letters[0].FontName ?? "";
        return fontName.Contains("Italic", StringComparison.OrdinalIgnoreCase)
               || fontName.Contains("Oblique", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether a line qualifies as a style-based heading.
    /// A line is a style-based heading if it is short (≤ <see cref="MaxStyleHeadingWords"/> words) and either:
    /// (a) uses a different font family than the body text, or
    /// (b) consists entirely of bold words at the body font size.
    /// </summary>
    private static bool IsStyleBasedHeading(TextLine line, double bodyFontSize,
        Dictionary<int, int> headingLevelMap, string bodyFontFamily)
    {
        if (line.Words.Count == 0 || line.Words.Count > MaxStyleHeadingWords)
        {
            return false;
        }

        double fontSize = line.GetPrimaryFontSize();
        int fontKey = (int)(Math.Round(fontSize, 1) * 10);

        // Already a font-size heading — skip.
        if (headingLevelMap.ContainsKey(fontKey))
        {
            return false;
        }

        // Check if the line uses a different font family than body text
        if (!string.IsNullOrEmpty(bodyFontFamily))
        {
            string lineFamily = GetConsistentFontFamily(line);
            if (!string.IsNullOrEmpty(lineFamily) &&
                !lineFamily.Equals(bodyFontFamily, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // All-bold short line at body font size → heading
        if (Math.Abs(fontSize - bodyFontSize) < 0.5 && line.Words.All(IsBoldFont))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns the normalized font family of the line if all words share the same family.
    /// Returns empty string if words use mixed families.
    /// </summary>
    private static string GetConsistentFontFamily(TextLine line)
    {
        if (line.Words.Count == 0)
        {
            return "";
        }

        string? firstFamily = null;

        foreach (Word word in line.Words)
        {
            if (word.Letters.Count == 0)
            {
                continue;
            }

            string family = NormalizeFontFamily(word.Letters[0].FontName ?? "");
            firstFamily ??= family;

            if (!family.Equals(firstFamily, StringComparison.OrdinalIgnoreCase))
            {
                return "";
            }
        }

        return firstFamily ?? "";
    }

    /// <summary>
    /// Normalizes a PDF font name to its base family by stripping subset prefixes
    /// and weight/style suffixes (e.g., "BCDEFG+Helvetica-BoldOblique" → "Helvetica").
    /// </summary>
    private static string NormalizeFontFamily(string fontName)
    {
        if (string.IsNullOrEmpty(fontName))
        {
            return "";
        }

        // Strip subset prefix (e.g., "BCDEFG+")
        int plusIndex = fontName.IndexOf('+');
        if (plusIndex >= 0 && plusIndex <= 7)
        {
            fontName = fontName[(plusIndex + 1)..];
        }

        // Split by separator characters
        int dashIndex = fontName.IndexOf('-');
        if (dashIndex > 0)
        {
            return fontName[..dashIndex];
        }

        int commaIndex = fontName.IndexOf(',');
        if (commaIndex > 0)
        {
            return fontName[..commaIndex];
        }

        // Strip known style words from the end
        string[] styleWords = ["BoldOblique", "BoldItalic", "Bold", "Italic", "Oblique", "Regular", "Light", "Medium", "Semibold", "Heavy", "Black"];
        foreach (string style in styleWords)
        {
            if (fontName.EndsWith(style, StringComparison.OrdinalIgnoreCase) && fontName.Length > style.Length)
            {
                return fontName[..^style.Length];
            }
        }

        return fontName;
    }

    /// <summary>
    /// Determines the most common font family at the body font size.
    /// </summary>
    private static string DetermineBodyFontFamily(List<(double FontSize, string FontName)> fontData, double bodyFontSize)
    {
        return fontData
            .Where(f => Math.Abs(f.FontSize - bodyFontSize) < 0.5)
            .GroupBy(f => NormalizeFontFamily(f.FontName))
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault() ?? "";
    }

    /// <summary>
    /// Calculates an adaptive column gap threshold based on the page’s
    /// inter-word spacing distribution. Returns a value large enough to
    /// avoid false positives from justified text but small enough to
    /// detect narrow column gutters.
    /// </summary>
    private static double CalculateAdaptiveColumnGap(List<TextLine> lines)
    {
        List<double> wordGaps = [];

        foreach (TextLine line in lines)
        {
            for (int w = 1; w < line.Words.Count; w++)
            {
                double gap = line.Words[w].BoundingBox.Left - line.Words[w - 1].BoundingBox.Right;
                if (gap > 0)
                {
                    wordGaps.Add(gap);
                }
            }
        }

        if (wordGaps.Count < 10)
        {
            return ColumnGapThreshold;
        }

        wordGaps.Sort();
        double median = wordGaps[wordGaps.Count / 2];

        // Use 4× the median word gap, but at least 25pt
        return Math.Max(median * 4, 25);
    }

    private static bool IsUnorderedListItem(string lineText)
    {
        string trimmed = lineText.TrimStart();
        return trimmed.Length > 1 && BulletChars.Contains(trimmed[0]) && char.IsWhiteSpace(trimmed[1]);
    }

    private static bool IsOrderedListItem(string lineText)
    {
        return OrderedListPrefixRegex().IsMatch(lineText.TrimStart());
    }

    /// <summary>
    /// Represents a group of words on the same vertical line.
    /// </summary>
    private sealed class TextLine(double y)
    {
        public double Y { get; } = y;
        public List<Word> Words { get; } = [];

        /// <summary>
        /// Gets the leftmost X position of the first word on this line.
        /// </summary>
        public double GetMinX()
        {
            if (Words.Count == 0)
            {
                return 0;
            }

            return Words[0].BoundingBox.Left;
        }

        public string GetText()
        {
            return string.Join(" ", Words.Select(w => w.Text));
        }

        public double GetPrimaryFontSize()
        {
            if (Words.Count == 0)
            {
                return 12.0;
            }

            return Math.Round(Words[0].Letters[0].FontSize, 1);
        }
    }

    /// <summary>
    /// Represents a spatial group of text lines that are visually close together.
    /// Lines within the same block share horizontal overlap and small vertical gaps.
    /// </summary>
    private sealed class TextBlock
    {
        public List<TextLine> Lines { get; } = [];
        public double MinX { get; private set; } = double.MaxValue;
        public double MaxX { get; private set; } = double.MinValue;
        public double MinY { get; private set; } = double.MaxValue;
        public double MaxY { get; private set; } = double.MinValue;

        /// <summary>
        /// Adds a line to this block and updates the bounding box.
        /// </summary>
        public void AddLine(TextLine line)
        {
            Lines.Add(line);
            double lineMinX = line.Words.Min(w => w.BoundingBox.Left);
            double lineMaxX = line.Words.Max(w => w.BoundingBox.Right);
            MinX = Math.Min(MinX, lineMinX);
            MaxX = Math.Max(MaxX, lineMaxX);
            MinY = Math.Min(MinY, line.Y);
            MaxY = Math.Max(MaxY, line.Y);
        }
    }
}
