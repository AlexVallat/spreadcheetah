using SpreadCheetah.Helpers;
using SpreadCheetah.Styling;
using System.Buffers.Text;

namespace SpreadCheetah.CellValueWriters.Number;

internal abstract class NumberCellValueWriterBase : CellValueWriter
{
    private static readonly int FormulaCellElementLength =
        StyledCellHelper.BeginStyledNumberCell.Length +
        SpreadsheetConstants.StyleIdMaxDigits +
        FormulaCellHelper.EndStyleBeginFormula.Length +
        FormulaCellHelper.EndFormulaBeginCachedValue.Length +
        FormulaCellHelper.EndCachedValueEndCell.Length;

    protected abstract int MaxNumberLength { get; }
    protected abstract int GetStyleId(StyleId styleId);
    protected abstract bool TryWriteValue(in DataCell cell, Span<byte> destination, out int bytesWritten);

    private int GetValueBytes(in DataCell cell, Span<byte> destination)
    {
        TryWriteValue(cell, destination, out var bytesWritten);
        return bytesWritten;
    }

    // <c><v>
    private static ReadOnlySpan<byte> BeginDataCell() => new[]
    {
        (byte)'<', (byte)'c', (byte)'>', (byte)'<', (byte)'v', (byte)'>'
    };

    private bool GetBytes(string formulaText, in DataCell cachedValue, int? styleId, SpreadsheetBuffer buffer)
    {
        var bytes = buffer.GetSpan();
        int bytesWritten;

        if (styleId is null)
        {
            bytesWritten = SpanHelper.GetBytes(FormulaCellHelper.BeginNumberFormulaCell, bytes);
        }
        else
        {
            bytesWritten = SpanHelper.GetBytes(StyledCellHelper.BeginStyledNumberCell, bytes);
            bytesWritten += Utf8Helper.GetBytes(styleId.Value, bytes.Slice(bytesWritten));
            bytesWritten += SpanHelper.GetBytes(FormulaCellHelper.EndStyleBeginFormula, bytes.Slice(bytesWritten));
        }

        bytesWritten += Utf8Helper.GetBytes(formulaText, bytes.Slice(bytesWritten), false);
        bytesWritten += SpanHelper.GetBytes(FormulaCellHelper.EndFormulaBeginCachedValue, bytes.Slice(bytesWritten));
        bytesWritten += GetValueBytes(cachedValue, bytes.Slice(bytesWritten));
        bytesWritten += SpanHelper.GetBytes(FormulaCellHelper.EndCachedValueEndCell, bytes.Slice(bytesWritten));
        buffer.Advance(bytesWritten);
        return true;
    }

    protected bool TryWriteCell(in DataCell cell, SpreadsheetBuffer buffer)
    {
        var bytes = buffer.GetSpan();

        if (BeginDataCell().TryCopyTo(bytes)
            && TryWriteValue(cell, bytes.Slice(BeginDataCell().Length), out var valueLength)
            && DataCellHelper.EndDefaultCell.TryCopyTo(bytes.Slice(BeginDataCell().Length + valueLength)))
        {
            buffer.Advance(BeginDataCell().Length + DataCellHelper.EndDefaultCell.Length + valueLength);
            return true;
        }

        return false;
    }

    protected bool TryWriteCell(in DataCell cell, int styleId, SpreadsheetBuffer buffer)
    {
        var bytes = buffer.GetSpan();
        var part1 = StyledCellHelper.BeginStyledNumberCell.Length;
        var part3 = StyledCellHelper.EndStyleBeginValue.Length;
        var part5 = DataCellHelper.EndDefaultCell.Length;

        if (StyledCellHelper.BeginStyledNumberCell.TryCopyTo(bytes)
            && Utf8Formatter.TryFormat(styleId, bytes.Slice(part1), out var part2)
            && StyledCellHelper.EndStyleBeginValue.TryCopyTo(bytes.Slice(part1 + part2))
            && TryWriteValue(cell, bytes.Slice(part1 + part2 + part3), out var part4)
            && DataCellHelper.EndDefaultCell.TryCopyTo(bytes.Slice(part1 + part2 + part3 + part4)))
        {
            buffer.Advance(part1 + part2 + part3 + part4 + part5);
            return true;
        }

        return false;
    }

    public override bool TryWriteCell(in DataCell cell, StyleId styleId, SpreadsheetBuffer buffer)
    {
        return TryWriteCell(cell, GetStyleId(styleId), buffer);
    }

    protected bool TryWriteCell(string formulaText, in DataCell cachedValue, int? styleId, SpreadsheetBuffer buffer)
    {
        var remaining = buffer.FreeCapacity;

        // Try with approximate formula text length
        var bytesNeeded = FormulaCellElementLength + MaxNumberLength + formulaText.Length * Utf8Helper.MaxBytePerChar;
        if (bytesNeeded <= remaining)
            return GetBytes(formulaText, cachedValue, styleId, buffer);

        // Try with more accurate length
        bytesNeeded = FormulaCellElementLength + MaxNumberLength + Utf8Helper.GetByteCount(formulaText);
        return bytesNeeded <= remaining && GetBytes(formulaText, cachedValue, styleId, buffer);
    }

    public override bool WriteStartElement(SpreadsheetBuffer buffer)
    {
        buffer.Advance(SpanHelper.GetBytes(BeginDataCell(), buffer.GetSpan()));
        return true;
    }

    public override bool WriteStartElement(StyleId styleId, SpreadsheetBuffer buffer)
    {
        var bytes = buffer.GetSpan();
        var bytesWritten = SpanHelper.GetBytes(StyledCellHelper.BeginStyledNumberCell, bytes);
        bytesWritten += Utf8Helper.GetBytes(GetStyleId(styleId), bytes.Slice(bytesWritten));
        bytesWritten += SpanHelper.GetBytes(StyledCellHelper.EndStyleBeginValue, bytes.Slice(bytesWritten));
        buffer.Advance(bytesWritten);
        return true;
    }

    protected static bool WriteFormulaStartElement(int? styleId, SpreadsheetBuffer buffer)
    {
        if (styleId is null)
        {
            buffer.Advance(SpanHelper.GetBytes(FormulaCellHelper.BeginNumberFormulaCell, buffer.GetSpan()));
            return true;
        }

        var bytes = buffer.GetSpan();
        var bytesWritten = SpanHelper.GetBytes(StyledCellHelper.BeginStyledNumberCell, bytes);
        bytesWritten += Utf8Helper.GetBytes(styleId.Value, bytes.Slice(bytesWritten));
        bytesWritten += SpanHelper.GetBytes(FormulaCellHelper.EndStyleBeginFormula, bytes.Slice(bytesWritten));
        buffer.Advance(bytesWritten);
        return true;
    }

    public override bool CanWriteValuePieceByPiece(in DataCell cell) => true;

    public override bool WriteValuePieceByPiece(in DataCell cell, SpreadsheetBuffer buffer, ref int valueIndex)
    {
        if (MaxNumberLength > buffer.FreeCapacity) return false;
        buffer.Advance(GetValueBytes(cell, buffer.GetSpan()));
        return true;
    }

    public override bool TryWriteEndElement(SpreadsheetBuffer buffer)
    {
        var cellEnd = DataCellHelper.EndDefaultCell;
        var bytes = buffer.GetSpan();
        if (cellEnd.Length >= bytes.Length)
            return false;

        buffer.Advance(SpanHelper.GetBytes(cellEnd, bytes));
        return true;
    }

    public override bool TryWriteEndElement(in Cell cell, SpreadsheetBuffer buffer)
    {
        if (cell.Formula is null)
            return TryWriteEndElement(buffer);

        var cellEnd = FormulaCellHelper.EndCachedValueEndCell;
        if (cellEnd.Length > buffer.FreeCapacity)
            return false;

        buffer.Advance(SpanHelper.GetBytes(cellEnd, buffer.GetSpan()));
        return true;
    }
}
