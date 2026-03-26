namespace MooDb.Tests.Integration.Infrastructure.Models;

public sealed class TestAllTypesRow
{
    public int AllTypesId { get; set; }
    public bool BitValue { get; set; }
    public byte TinyIntValue { get; set; }
    public short SmallIntValue { get; set; }
    public int IntValue { get; set; }
    public long BigIntValue { get; set; }
    public float RealValue { get; set; }
    public double FloatValue { get; set; }
    public Guid UniqueIdentifierValue { get; set; }
    public DateOnly DateValue { get; set; }
    public DateTime DateTimeValue { get; set; }
    public DateTime SmallDateTimeValue { get; set; }
    public DateTime DateTime2Value { get; set; }
    public DateTimeOffset DateTimeOffsetValue { get; set; }
    public TimeOnly TimeValue { get; set; }
    public decimal DecimalValue { get; set; }
    public decimal MoneyValue { get; set; }
    public decimal SmallMoneyValue { get; set; }
    public string CharValue { get; set; } = string.Empty;
    public string VarCharValue { get; set; } = string.Empty;
    public string NCharValue { get; set; } = string.Empty;
    public string NVarCharValue { get; set; } = string.Empty;
    public byte[] BinaryValue { get; set; } = [];
    public byte[] VarBinaryValue { get; set; } = [];
    public TestStatus StatusById { get; set; }
    public TestStatus StatusByName { get; set; }
}
