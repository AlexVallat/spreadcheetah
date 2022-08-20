namespace SpreadCheetah.Test.Helpers;

internal static class TestData
{
    private static readonly Type[] CellTypesArray = new[] { typeof(Cell), typeof(DataCell), typeof(StyledCell) };
    private static readonly Type[] StyledCellTypesArray = new[] { typeof(Cell), typeof(StyledCell) };
    private static readonly CellType[] StyledCellTypesEnumArray = new[] { CellType.StyledCell, CellType.Cell };
    private static readonly CellValueType[] CellValueTypesArray = Enum.GetValues(typeof(CellValueType)).Cast<CellValueType>().ToArray();

    public static IEnumerable<object?[]> CellTypes() => CellTypesArray.Select(x => new object[] { x });
    public static IEnumerable<object?[]> StyledCellTypes() => StyledCellTypesArray.Select(x => new object[] { x });

    public static IEnumerable<object?[]> CombineWithCellTypes(params object?[] values)
    {
        return values.SelectMany(_ => CellTypesArray, (value, type) => new object?[] { value, type });
    }

    public static IEnumerable<object?[]> CombineWithCellTypes(params (object?, object?)[] values)
    {
        return values.SelectMany(_ => CellTypesArray, (value, type) => new object?[] { value.Item1, value.Item2, type });
    }

    public static IEnumerable<object?[]> CombineWithStyledCellTypes(params object?[] values)
    {
        return values.SelectMany(_ => StyledCellTypesArray, (value, type) => new object?[] { value, type });
    }

    public static IEnumerable<object?[]> StyledCellAndValueTypes()
    {
        foreach (var valueType in CellValueTypesArray)
        {
            foreach (var cellType in StyledCellTypesEnumArray)
            {
                yield return new object?[] { valueType, true, cellType };
                yield return new object?[] { valueType, false, cellType };
            }
        }
    }
}
