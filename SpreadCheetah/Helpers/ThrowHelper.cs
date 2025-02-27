using System.Diagnostics.CodeAnalysis;

namespace SpreadCheetah.Helpers;

internal static class ThrowHelper
{
    [DoesNotReturn]
    public static void ColumnNumberInvalid(string? paramName, int number) => throw new ArgumentOutOfRangeException(paramName, number, "The column number must be greater than 0 and can't be larger than 16384.");

    [DoesNotReturn]
    public static void SingleCellReferenceInvalid(string? paramName) => throw new ArgumentException("Invalid reference for a cell.", paramName);

    [DoesNotReturn]
    public static void CellRangeReferenceInvalid(string? paramName) => throw new ArgumentException("Invalid reference for a cell range.", paramName);

    [DoesNotReturn]
    public static void SingleCellOrCellRangeReferenceInvalid(string? paramName) => throw new ArgumentException("Invalid reference for a cell or a range of cells.", paramName);

    [DoesNotReturn]
    public static void EnumValueInvalid<T>(string? paramName, T value) => throw new ArgumentOutOfRangeException(paramName, value, "The value is not a valid enum value.");

    [DoesNotReturn]
    public static void MaxNumberOfDataValidations() => throw new SpreadCheetahException("Can't add more than " + SpreadsheetConstants.MaxNumberOfDataValidations + " data validations to a worksheet.");

    [DoesNotReturn]
    public static void NoActiveWorksheet() => throw new SpreadCheetahException("There is no active worksheet.");

    [DoesNotReturn]
    public static void SpreadsheetMustContainWorksheet() => throw new SpreadCheetahException("Spreadsheet must contain at least one worksheet.");

    [DoesNotReturn]
    public static void StartWorksheetNotAllowedAfterFinish() => throw new SpreadCheetahException("Can't start another worksheet after " + nameof(Spreadsheet.FinishAsync) + " has been called.");

    [DoesNotReturn]
    public static void ValueIsNegative<T>(string? paramName, T value) => throw new ArgumentOutOfRangeException(paramName, value, "The value can not be negative.");

    [DoesNotReturn]
    public static void WorksheetNameAlreadyExists(string? paramName) => throw new ArgumentException("A worksheet with the given name already exists.", paramName);

    [DoesNotReturn]
    public static void WorksheetNameEmptyOrWhiteSpace(string? paramName) => throw new ArgumentException("The name can not be empty or consist only of whitespace.", paramName);

    [DoesNotReturn]
    public static void WorksheetNameInvalidCharacters(string? paramName, string invalidChars) => throw new ArgumentException("The name can not contain any of the following characters: " + invalidChars, paramName);

    [DoesNotReturn]
    public static void WorksheetNameStartsOrEndsWithSingleQuote(string? paramName) => throw new ArgumentException("The name can not start or end with a single quote.", paramName);

    [DoesNotReturn]
    public static void WorksheetNameTooLong(string? paramName) => throw new ArgumentException("The name can not be more than 31 characters.", paramName);
}
