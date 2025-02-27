using SpreadCheetah.Helpers;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace SpreadCheetah.MetadataXml;

[StructLayout(LayoutKind.Auto)]
internal struct WorksheetRelsXml : IXmlWriter
{
    public static ValueTask WriteAsync(
        ZipArchive archive,
        CompressionLevel compressionLevel,
        SpreadsheetBuffer buffer,
        int worksheetIndex,
        int notesFilesIndex,
        CancellationToken token)
    {
        var entryName = StringHelper.Invariant($"xl/worksheets/_rels/sheet{worksheetIndex}.xml.rels");
        var entry = archive.CreateEntry(entryName, compressionLevel);
        var writer = new WorksheetRelsXml(notesFilesIndex);
        return writer.WriteAsync(entry, buffer, token);
    }

    private static ReadOnlySpan<byte> Header =>
        """<?xml version="1.0" encoding="utf-8"?>"""u8 +
        """<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">"""u8;

    private static ReadOnlySpan<byte> CommentStart => """<Relationship Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/comments" Target="../comments"""u8;
    private static ReadOnlySpan<byte> VmlDrawingStart => """<Relationship Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/vmlDrawing" Target="../drawings/vmlDrawing"""u8;
    private static ReadOnlySpan<byte> EndQuoteEndTag => "\"/>"u8;
    private static ReadOnlySpan<byte> Footer => "</Relationships>"u8;

    private readonly int _notesFilesIndex;
    private Element _next;

    private WorksheetRelsXml(int notesFilesIndex)
    {
        _notesFilesIndex = notesFilesIndex;
    }

    public bool TryWrite(Span<byte> bytes, out int bytesWritten)
    {
        bytesWritten = 0;

        if (_next == Element.Header && !Advance(Header.TryCopyTo(bytes, ref bytesWritten))) return false;
        if (_next == Element.VmlDrawing && !Advance(TryWriteVmlDrawing(bytes, ref bytesWritten))) return false;
        if (_next == Element.Comments && !Advance(TryWriteComments(bytes, ref bytesWritten))) return false;
        if (_next == Element.Footer && !Advance(Footer.TryCopyTo(bytes, ref bytesWritten))) return false;

        return true;
    }

    private bool Advance(bool success)
    {
        if (success)
            ++_next;

        return success;
    }

    private readonly bool TryWriteVmlDrawing(Span<byte> bytes, ref int bytesWritten)
    {
        var notesFilesIndex = _notesFilesIndex;
        var span = bytes.Slice(bytesWritten);
        var written = 0;

        var vmlId = (notesFilesIndex - 1) * 2 + 1;
        if (!VmlDrawingStart.TryCopyTo(span, ref written)) return false;
        if (!SpanHelper.TryWrite(notesFilesIndex, span, ref written)) return false;
        if (!""".vml" Id="rId"""u8.TryCopyTo(span, ref written)) return false;
        if (!SpanHelper.TryWrite(vmlId, span, ref written)) return false;
        if (!EndQuoteEndTag.TryCopyTo(span, ref written)) return false;

        bytesWritten += written;
        return true;
    }

    private readonly bool TryWriteComments(Span<byte> bytes, ref int bytesWritten)
    {
        var notesFilesIndex = _notesFilesIndex;
        var span = bytes.Slice(bytesWritten);
        var written = 0;

        var commentId = (notesFilesIndex - 1) * 2 + 2;
        if (!CommentStart.TryCopyTo(span, ref written)) return false;
        if (!SpanHelper.TryWrite(notesFilesIndex, span, ref written)) return false;
        if (!""".xml" Id="rId"""u8.TryCopyTo(span, ref written)) return false;
        if (!SpanHelper.TryWrite(commentId, span, ref written)) return false;
        if (!EndQuoteEndTag.TryCopyTo(span, ref written)) return false;

        bytesWritten += written;
        return true;
    }

    private enum Element
    {
        Header,
        VmlDrawing,
        Comments,
        Footer,
        Done
    }
}
