CREATE PROCEDURE [Tests].[usp_AllTypes_GetLatest]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        [AllTypesId],
        [BitValue],
        [TinyIntValue],
        [SmallIntValue],
        [IntValue],
        [BigIntValue],
        [RealValue],
        [FloatValue],
        [UniqueIdentifierValue],
        [DateValue],
        [DateTimeValue],
        [SmallDateTimeValue],
        [DateTime2Value],
        [DateTimeOffsetValue],
        [TimeValue],
        [DecimalValue],
        [MoneyValue],
        [SmallMoneyValue],
        [CharValue],
        [VarCharValue],
        [NCharValue],
        [NVarCharValue],
        [BinaryValue],
        [VarBinaryValue],
        [StatusById],
        [StatusByName]
    FROM [Tests].[tbl_AllTypes]
    ORDER BY [AllTypesId] DESC;
END;
