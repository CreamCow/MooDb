using Microsoft.Data.SqlClient;
using MooDb.Tests.Integration.Infrastructure.Fixtures;
using MooDb.Tests.Integration.Infrastructure.Models;

namespace MooDb.Tests.Integration.Tests.DataTypes;

[Collection("MooDb")]
public sealed class AllTypesMappingTests
{
    private readonly MooDbFixture _fixture;

    public AllTypesMappingTests(MooDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SingleAsync_WhenReadingAllSupportedTypes_MapsExpectedValues()
    {
        // Arrange
        await _fixture.ResetAsync();

        var id = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var dateValue = new DateTime(2024, 03, 04);
        var dateTimeValue = new DateTime(2024, 03, 04, 05, 06, 07);
        var smallDateTimeValue = new DateTime(2024, 03, 04, 05, 06, 00);
        var dateTime2Value = new DateTime(2024, 03, 04, 05, 06, 07, 890);
        var dateTimeOffsetValue = new DateTimeOffset(2024, 03, 04, 05, 06, 07, 890, TimeSpan.FromHours(1));
        var timeValue = new TimeSpan(12, 34, 56);

        await _fixture.ExecuteSqlAsync(
            """
            INSERT INTO [Tests].[tbl_AllTypes]
            (
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
            )
            VALUES
            (
                @BitValue,
                @TinyIntValue,
                @SmallIntValue,
                @IntValue,
                @BigIntValue,
                @RealValue,
                @FloatValue,
                @UniqueIdentifierValue,
                @DateValue,
                @DateTimeValue,
                @SmallDateTimeValue,
                @DateTime2Value,
                @DateTimeOffsetValue,
                @TimeValue,
                @DecimalValue,
                @MoneyValue,
                @SmallMoneyValue,
                @CharValue,
                @VarCharValue,
                @NCharValue,
                @NVarCharValue,
                @BinaryValue,
                @VarBinaryValue,
                @StatusById,
                @StatusByName
            );
            """,
            [
                new SqlParameter("@BitValue", true),
                new SqlParameter("@TinyIntValue", (byte)7),
                new SqlParameter("@SmallIntValue", (short)32000),
                new SqlParameter("@IntValue", 123456),
                new SqlParameter("@BigIntValue", 9876543210L),
                new SqlParameter("@RealValue", 1.25f),
                new SqlParameter("@FloatValue", 2.5d),
                new SqlParameter("@UniqueIdentifierValue", id),
                new SqlParameter("@DateValue", dateValue),
                new SqlParameter("@DateTimeValue", dateTimeValue),
                new SqlParameter("@SmallDateTimeValue", smallDateTimeValue),
                new SqlParameter("@DateTime2Value", dateTime2Value),
                new SqlParameter("@DateTimeOffsetValue", dateTimeOffsetValue),
                new SqlParameter("@TimeValue", timeValue),
                new SqlParameter("@DecimalValue", 123.4567m),
                new SqlParameter("@MoneyValue", 45.67m),
                new SqlParameter("@SmallMoneyValue", 8.90m),
                new SqlParameter("@CharValue", "ABC"),
                new SqlParameter("@VarCharValue", "Hello"),
                new SqlParameter("@NCharValue", "XYZ"),
                new SqlParameter("@NVarCharValue", "World"),
                new SqlParameter("@BinaryValue", new byte[] { 1, 2, 3, 4 }),
                new SqlParameter("@VarBinaryValue", new byte[] { 5, 6, 7, 8 }),
                new SqlParameter("@StatusById", 2),
                new SqlParameter("@StatusByName", "Active")
            ]);

        var db = _fixture.CreateMooDbContext();

        // Act
        var row = await db.SingleAsync<TestAllTypesRow>("Tests.usp_AllTypes_GetLatest");

        // Assert
        Assert.NotNull(row);
        Assert.True(row.BitValue);
        Assert.Equal((byte)7, row.TinyIntValue);
        Assert.Equal((short)32000, row.SmallIntValue);
        Assert.Equal(123456, row.IntValue);
        Assert.Equal(9876543210L, row.BigIntValue);
        Assert.Equal(1.25f, row.RealValue);
        Assert.Equal(2.5d, row.FloatValue);
        Assert.Equal(id, row.UniqueIdentifierValue);
        Assert.Equal(DateOnly.FromDateTime(dateValue), row.DateValue);
        Assert.Equal(dateTimeValue, row.DateTimeValue);
        Assert.Equal(smallDateTimeValue, row.SmallDateTimeValue);
        Assert.Equal(dateTime2Value, row.DateTime2Value);
        Assert.Equal(dateTimeOffsetValue, row.DateTimeOffsetValue);
        Assert.Equal(TimeOnly.FromTimeSpan(timeValue), row.TimeValue);
        Assert.Equal(123.4567m, row.DecimalValue);
        Assert.Equal(45.67m, row.MoneyValue);
        Assert.Equal(8.90m, row.SmallMoneyValue);
        Assert.Equal("ABC", row.CharValue);
        Assert.Equal("Hello", row.VarCharValue);
        Assert.Equal("XYZ", row.NCharValue.TrimEnd());
        Assert.Equal("World", row.NVarCharValue);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, row.BinaryValue);
        Assert.Equal(new byte[] { 5, 6, 7, 8 }, row.VarBinaryValue);
        Assert.Equal(TestStatus.Suspended, row.StatusById);
        Assert.Equal(TestStatus.Active, row.StatusByName);
    }
}
