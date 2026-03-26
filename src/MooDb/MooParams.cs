using System.Collections;
using System.Data;
using Microsoft.Data.SqlClient;

namespace MooDb;

/// <summary>
/// Represents a collection of SQL Server parameters for use with MooDb commands.
/// </summary>
/// <remarks>
/// <para>
/// Provides a fluent, strongly typed API for building input, output, and input/output parameters.
/// </para>
/// <para>
/// MooParams is designed to reflect SQL Server parameter intent precisely. Where size, precision,
/// or scale materially affect database behaviour, those values must be supplied explicitly.
/// </para>
/// <para>
/// Output and input/output parameter values remain available after command execution and can be
/// retrieved using typed getter methods.
/// </para>
/// <para>
/// Raw <see cref="SqlParameter"/> instances can also be added directly for advanced scenarios
/// not yet covered by MooParams.
/// </para>
/// </remarks>
public sealed class MooParams : IReadOnlyList<SqlParameter>
{
    // Fields
    private readonly List<SqlParameter> _parameters = [];
    private readonly Dictionary<string, SqlParameter> _lookup =
        new(StringComparer.OrdinalIgnoreCase);


    // Constructors
    /// <summary>
    /// Creates an empty parameter collection.
    /// </summary>
    public MooParams()
    {
    }


    // Public API
    /// <summary>
    /// Adds a raw SQL parameter to the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is intended as an escape hatch for advanced scenarios that require direct
    /// <see cref="SqlParameter"/> control.
    /// </para>
    /// </remarks>
    public MooParams Add(SqlParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        ValidateName(parameter.ParameterName);
        AddInternal(parameter);
        return this;
    }


    /// <summary>
    /// Adds a <c>bit</c> parameter to the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Supports input, output, input/output, and return-value usage via the supplied direction.
    /// </para>
    /// </remarks>
    public MooParams AddBit(
        string name,
        bool? value,
        ParameterDirection direction = ParameterDirection.Input)
        => AddSimple(name, SqlDbType.Bit, value, direction);


    /// <summary>
    /// Adds a <c>tinyint</c> parameter to the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Supports input, output, input/output, and return-value usage via the supplied direction.
    /// </para>
    /// </remarks>
    public MooParams AddTinyInt(
        string name,
        byte? value,
        ParameterDirection direction = ParameterDirection.Input)
        => AddSimple(name, SqlDbType.TinyInt, value, direction);


    /// <summary>
    /// Adds a <c>smallint</c> parameter to the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Supports input, output, input/output, and return-value usage via the supplied direction.
    /// </para>
    /// </remarks>
    public MooParams AddSmallInt(
        string name,
        short? value,
        ParameterDirection direction = ParameterDirection.Input)
        => AddSimple(name, SqlDbType.SmallInt, value, direction);


    /// <summary>
    /// Adds an <c>int</c> parameter to the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Supports input, output, input/output, and return-value usage via the supplied direction.
    /// </para>
    /// </remarks>
    public MooParams AddInt(
        string name,
        int? value,
        ParameterDirection direction = ParameterDirection.Input)
        => AddSimple(name, SqlDbType.Int, value, direction);


    /// <summary>
    /// Adds a <c>bigint</c> parameter to the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Supports input, output, input/output, and return-value usage via the supplied direction.
    /// </para>
    /// </remarks>
    public MooParams AddBigInt(
        string name,
        long? value,
        ParameterDirection direction = ParameterDirection.Input)
        => AddSimple(name, SqlDbType.BigInt, value, direction);


    /// <summary>
    /// Adds a <c>real</c> parameter to the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Supports input, output, input/output, and return-value usage via the supplied direction.
    /// </para>
    /// </remarks>
    public MooParams AddReal(
        string name,
        float? value,
        ParameterDirection direction = ParameterDirection.Input)
        => AddSimple(name, SqlDbType.Real, value, direction);


    /// <summary>
    /// Adds a <c>float</c> parameter to the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Supports input, output, input/output, and return-value usage via the supplied direction.
    /// </para>
    /// </remarks>
    public MooParams AddFloat(
        string name,
        double? value,
        ParameterDirection direction = ParameterDirection.Input)
        => AddSimple(name, SqlDbType.Float, value, direction);


    /// <summary>
    /// Adds a <c>uniqueidentifier</c> parameter to the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Supports input, output, input/output, and return-value usage via the supplied direction.
    /// </para>
    /// </remarks>
    public MooParams AddUniqueIdentifier(
        string name,
        Guid? value,
        ParameterDirection direction = ParameterDirection.Input)
        => AddSimple(name, SqlDbType.UniqueIdentifier, value, direction);


    /// <summary>
    /// Adds a <c>date</c> parameter to the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The value is supplied as <see cref="DateTime"/> and sent to SQL Server as a <c>date</c> parameter.
    /// </para>
    /// </remarks>
    public MooParams AddDate(
        string name,
        DateTime? value,
        ParameterDirection direction = ParameterDirection.Input)
        => AddSimple(name, SqlDbType.Date, value, direction);


    /// <summary>
    /// Adds a <c>datetime</c> parameter to the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use <see cref="AddDateTime2(string, DateTime?, byte, ParameterDirection)"/> when scale precision matters.
    /// </para>
    /// </remarks>
    public MooParams AddDateTime(
        string name,
        DateTime? value,
        ParameterDirection direction = ParameterDirection.Input)
        => AddSimple(name, SqlDbType.DateTime, value, direction);


    /// <summary>
    /// Adds a <c>smalldatetime</c> parameter to the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The value is supplied as <see cref="DateTime"/> and sent to SQL Server as a <c>smalldatetime</c> parameter.
    /// </para>
    /// </remarks>
    public MooParams AddSmallDateTime(
        string name,
        DateTime? value,
        ParameterDirection direction = ParameterDirection.Input)
        => AddSimple(name, SqlDbType.SmallDateTime, value, direction);


    /// <summary>
    /// Adds a <c>datetime2</c> parameter to the collection using the default SQL Server scale of 7.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use the overload that accepts an explicit scale when database precision must be controlled precisely.
    /// </para>
    /// </remarks>
    public MooParams AddDateTime2(
        string name,
        DateTime? value,
        ParameterDirection direction = ParameterDirection.Input)
        => AddDateTime2(name, value, scale: 7, direction);


    /// <summary>
    /// Adds a <c>datetime2</c> parameter to the collection with an explicit scale.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this overload when the database contract requires a specific fractional seconds precision,
    /// such as <c>datetime2(0)</c>.
    /// </para>
    /// </remarks>
    public MooParams AddDateTime2(
        string name,
        DateTime? value,
        byte scale,
        ParameterDirection direction = ParameterDirection.Input)
    {
        ValidateName(name);
        ValidateScale(scale);

        var parameter = new SqlParameter(name, SqlDbType.DateTime2)
        {
            Value = value.HasValue ? value.Value : DBNull.Value,
            Direction = direction,
            Scale = scale,
            IsNullable = true
        };

        AddInternal(parameter);
        return this;
    }


    /// <summary>
    /// Adds a <c>datetimeoffset</c> parameter to the collection using the default SQL Server scale of 7.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use the overload that accepts an explicit scale when database precision must be controlled precisely.
    /// </para>
    /// </remarks>
    public MooParams AddDateTimeOffset(
        string name,
        DateTimeOffset? value,
        ParameterDirection direction = ParameterDirection.Input)
        => AddDateTimeOffset(name, value, scale: 7, direction);


    /// <summary>
    /// Adds a <c>datetimeoffset</c> parameter to the collection with an explicit scale.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this overload when the database contract requires a specific fractional seconds precision.
    /// </para>
    /// </remarks>
    public MooParams AddDateTimeOffset(
        string name,
        DateTimeOffset? value,
        byte scale,
        ParameterDirection direction = ParameterDirection.Input)
    {
        ValidateName(name);
        ValidateScale(scale);

        var parameter = new SqlParameter(name, SqlDbType.DateTimeOffset)
        {
            Value = value.HasValue ? value.Value : DBNull.Value,
            Direction = direction,
            Scale = scale,
            IsNullable = true
        };

        AddInternal(parameter);
        return this;
    }


    /// <summary>
    /// Adds a <c>time</c> parameter to the collection using the default SQL Server scale of 7.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use the overload that accepts an explicit scale when database precision must be controlled precisely.
    /// </para>
    /// </remarks>
    public MooParams AddTime(
        string name,
        TimeSpan? value,
        ParameterDirection direction = ParameterDirection.Input)
        => AddTime(name, value, scale: 7, direction);


    /// <summary>
    /// Adds a <c>time</c> parameter to the collection with an explicit scale.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this overload when the database contract requires a specific fractional seconds precision.
    /// </para>
    /// </remarks>
    public MooParams AddTime(
        string name,
        TimeSpan? value,
        byte scale,
        ParameterDirection direction = ParameterDirection.Input)
    {
        ValidateName(name);
        ValidateScale(scale);

        var parameter = new SqlParameter(name, SqlDbType.Time)
        {
            Value = value.HasValue ? value.Value : DBNull.Value,
            Direction = direction,
            Scale = scale,
            IsNullable = true
        };

        AddInternal(parameter);
        return this;
    }


    /// <summary>
    /// Adds a <c>decimal</c> parameter to the collection with explicit precision and scale.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Precision and scale are always required for decimal parameters so database intent remains explicit.
    /// </para>
    /// </remarks>
    public MooParams AddDecimal(
        string name,
        decimal? value,
        byte precision,
        byte scale,
        ParameterDirection direction = ParameterDirection.Input)
    {
        ValidateName(name);
        ValidatePrecisionAndScale(precision, scale);

        var parameter = new SqlParameter(name, SqlDbType.Decimal)
        {
            Value = value.HasValue ? value.Value : DBNull.Value,
            Direction = direction,
            Precision = precision,
            Scale = scale,
            IsNullable = true
        };

        AddInternal(parameter);
        return this;
    }


    /// <summary>
    /// Adds a <c>money</c> parameter to the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The value is supplied as <see cref="decimal"/> and sent to SQL Server as a <c>money</c> parameter.
    /// </para>
    /// </remarks>
    public MooParams AddMoney(
        string name,
        decimal? value,
        ParameterDirection direction = ParameterDirection.Input)
        => AddSimple(name, SqlDbType.Money, value, direction);


    /// <summary>
    /// Adds a <c>smallmoney</c> parameter to the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The value is supplied as <see cref="decimal"/> and sent to SQL Server as a <c>smallmoney</c> parameter.
    /// </para>
    /// </remarks>
    public MooParams AddSmallMoney(
        string name,
        decimal? value,
        ParameterDirection direction = ParameterDirection.Input)
        => AddSimple(name, SqlDbType.SmallMoney, value, direction);


    /// <summary>
    /// Adds a fixed-length <c>char</c> parameter to the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The size is required because fixed-length character parameters are part of the database contract.
    /// </para>
    /// </remarks>
    public MooParams AddChar(
        string name,
        string? value,
        int size,
        ParameterDirection direction = ParameterDirection.Input)
        => AddFixedLengthString(name, SqlDbType.Char, value, size, direction);


    /// <summary>
    /// Adds a variable-length <c>varchar</c> parameter to the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The size is required. Use <c>-1</c> to represent <c>varchar(max)</c>.
    /// </para>
    /// </remarks>
    public MooParams AddVarChar(
        string name,
        string? value,
        int size,
        ParameterDirection direction = ParameterDirection.Input)
        => AddVariableLengthString(name, SqlDbType.VarChar, value, size, direction);


    /// <summary>
    /// Adds a fixed-length <c>nchar</c> parameter to the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The size is required because fixed-length Unicode character parameters are part of the database contract.
    /// </para>
    /// </remarks>
    public MooParams AddNChar(
        string name,
        string? value,
        int size,
        ParameterDirection direction = ParameterDirection.Input)
        => AddFixedLengthString(name, SqlDbType.NChar, value, size, direction);


    /// <summary>
    /// Adds a variable-length <c>nvarchar</c> parameter to the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The size is required. Use <c>-1</c> to represent <c>nvarchar(max)</c>.
    /// </para>
    /// </remarks>
    public MooParams AddNVarChar(
        string name,
        string? value,
        int size,
        ParameterDirection direction = ParameterDirection.Input)
        => AddVariableLengthString(name, SqlDbType.NVarChar, value, size, direction);


    /// <summary>
    /// Adds a fixed-length <c>binary</c> parameter to the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The size is required because fixed-length binary parameters are part of the database contract.
    /// </para>
    /// </remarks>
    public MooParams AddBinary(
        string name,
        byte[]? value,
        int size,
        ParameterDirection direction = ParameterDirection.Input)
        => AddFixedLengthBinary(name, SqlDbType.Binary, value, size, direction);


    /// <summary>
    /// Adds a variable-length <c>varbinary</c> parameter to the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The size is required. Use <c>-1</c> to represent <c>varbinary(max)</c>.
    /// </para>
    /// </remarks>
    public MooParams AddVarBinary(
        string name,
        byte[]? value,
        int size,
        ParameterDirection direction = ParameterDirection.Input)
        => AddVariableLengthBinary(name, SqlDbType.VarBinary, value, size, direction);


    /// <summary>
    /// Adds an <c>xml</c> parameter to the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// XML values are supplied as strings and sent to SQL Server as <c>xml</c> parameters.
    /// </para>
    /// </remarks>
    public MooParams AddXml(
        string name,
        string? value,
        ParameterDirection direction = ParameterDirection.Input)
        => AddSimple(name, SqlDbType.Xml, value, direction);


    /// <summary>
    /// Adds a <c>sql_variant</c> parameter to the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this only when the database contract intentionally requires <c>sql_variant</c>.
    /// </para>
    /// </remarks>
    public MooParams AddVariant(
        string name,
        object? value,
        ParameterDirection direction = ParameterDirection.Input)
        => AddSimple(name, SqlDbType.Variant, value, direction);


    /// <summary>
    /// Adds a table-valued parameter to the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Table-valued parameters are used to pass sets of rows to SQL Server using a user-defined table type.
    /// </para>
    /// <para>
    /// The <paramref name="typeName"/> must match the SQL Server user-defined table type name exactly, for example <c>Tests.udt_UserSeed</c>.
    /// </para>
    /// <para>
    /// Table-valued parameters are input-only and are sent to SQL Server using the SQL Server table-valued parameter mechanism.
    /// </para>
    /// </remarks>
    public MooParams AddTableValuedParameter(
        string name,
        object value,
        string typeName)
    {
        ValidateName(name);
        ArgumentNullException.ThrowIfNull(value);
        MooGuard.AgainstNullOrWhiteSpace(typeName, nameof(typeName), "Table-valued parameter type name");

        var parameter = new SqlParameter(name, SqlDbType.Structured)
        {
            Value = value,
            Direction = ParameterDirection.Input,
            TypeName = typeName
        };

        AddInternal(parameter);
        return this;
    }


    /// <summary>
    /// Gets a <c>bit</c> output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Throws if the parameter does not exist, does not contain a value, or cannot be read as <see cref="bool"/>.
    /// </para>
    /// </remarks>
    public bool GetBit(string name) => GetRequiredStruct<bool>(name);


    /// <summary>
    /// Gets a nullable <c>bit</c> output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns <c>null</c> when the parameter value is <see cref="DBNull"/>.
    /// </para>
    /// </remarks>
    public bool? GetNullableBit(string name) => GetNullableStruct<bool>(name);


    /// <summary>
    /// Gets a <c>tinyint</c> output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Throws if the parameter does not exist, does not contain a value, or cannot be read as <see cref="byte"/>.
    /// </para>
    /// </remarks>
    public byte GetTinyInt(string name) => GetRequiredStruct<byte>(name);


    /// <summary>
    /// Gets a nullable <c>tinyint</c> output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns <c>null</c> when the parameter value is <see cref="DBNull"/>.
    /// </para>
    /// </remarks>
    public byte? GetNullableTinyInt(string name) => GetNullableStruct<byte>(name);


    /// <summary>
    /// Gets a <c>smallint</c> output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Throws if the parameter does not exist, does not contain a value, or cannot be read as <see cref="short"/>.
    /// </para>
    /// </remarks>
    public short GetSmallInt(string name) => GetRequiredStruct<short>(name);


    /// <summary>
    /// Gets a nullable <c>smallint</c> output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns <c>null</c> when the parameter value is <see cref="DBNull"/>.
    /// </para>
    /// </remarks>
    public short? GetNullableSmallInt(string name) => GetNullableStruct<short>(name);


    /// <summary>
    /// Gets an <c>int</c> output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Throws if the parameter does not exist, does not contain a value, or cannot be read as <see cref="int"/>.
    /// </para>
    /// </remarks>
    public int GetInt(string name) => GetRequiredStruct<int>(name);


    /// <summary>
    /// Gets a nullable <c>int</c> output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns <c>null</c> when the parameter value is <see cref="DBNull"/>.
    /// </para>
    /// </remarks>
    public int? GetNullableInt(string name) => GetNullableStruct<int>(name);


    /// <summary>
    /// Gets a <c>bigint</c> output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Throws if the parameter does not exist, does not contain a value, or cannot be read as <see cref="long"/>.
    /// </para>
    /// </remarks>
    public long GetBigInt(string name) => GetRequiredStruct<long>(name);


    /// <summary>
    /// Gets a nullable <c>bigint</c> output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns <c>null</c> when the parameter value is <see cref="DBNull"/>.
    /// </para>
    /// </remarks>
    public long? GetNullableBigInt(string name) => GetNullableStruct<long>(name);


    /// <summary>
    /// Gets a <c>real</c> output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Throws if the parameter does not exist, does not contain a value, or cannot be read as <see cref="float"/>.
    /// </para>
    /// </remarks>
    public float GetReal(string name) => GetRequiredStruct<float>(name);


    /// <summary>
    /// Gets a nullable <c>real</c> output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns <c>null</c> when the parameter value is <see cref="DBNull"/>.
    /// </para>
    /// </remarks>
    public float? GetNullableReal(string name) => GetNullableStruct<float>(name);


    /// <summary>
    /// Gets a <c>float</c> output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Throws if the parameter does not exist, does not contain a value, or cannot be read as <see cref="double"/>.
    /// </para>
    /// </remarks>
    public double GetFloat(string name) => GetRequiredStruct<double>(name);


    /// <summary>
    /// Gets a nullable <c>float</c> output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns <c>null</c> when the parameter value is <see cref="DBNull"/>.
    /// </para>
    /// </remarks>
    public double? GetNullableFloat(string name) => GetNullableStruct<double>(name);


    /// <summary>
    /// Gets a <c>uniqueidentifier</c> output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Throws if the parameter does not exist, does not contain a value, or cannot be read as <see cref="Guid"/>.
    /// </para>
    /// </remarks>
    public Guid GetUniqueIdentifier(string name) => GetRequiredStruct<Guid>(name);


    /// <summary>
    /// Gets a nullable <c>uniqueidentifier</c> output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns <c>null</c> when the parameter value is <see cref="DBNull"/>.
    /// </para>
    /// </remarks>
    public Guid? GetNullableUniqueIdentifier(string name) => GetNullableStruct<Guid>(name);


    /// <summary>
    /// Gets a <c>date</c> output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Throws if the parameter does not exist, does not contain a value, or cannot be read as <see cref="DateTime"/>.
    /// </para>
    /// </remarks>
    public DateTime GetDate(string name) => GetRequiredStruct<DateTime>(name);


    /// <summary>
    /// Gets a nullable <c>date</c> output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns <c>null</c> when the parameter value is <see cref="DBNull"/>.
    /// </para>
    /// </remarks>
    public DateTime? GetNullableDate(string name) => GetNullableStruct<DateTime>(name);


    /// <summary>
    /// Gets a <c>datetime</c> output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Throws if the parameter does not exist, does not contain a value, or cannot be read as <see cref="DateTime"/>.
    /// </para>
    /// </remarks>
    public DateTime GetDateTime(string name) => GetRequiredStruct<DateTime>(name);


    /// <summary>
    /// Gets a nullable <c>datetime</c> output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns <c>null</c> when the parameter value is <see cref="DBNull"/>.
    /// </para>
    /// </remarks>
    public DateTime? GetNullableDateTime(string name) => GetNullableStruct<DateTime>(name);


    /// <summary>
    /// Gets a <c>datetimeoffset</c> output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Throws if the parameter does not exist, does not contain a value, or cannot be read as <see cref="DateTimeOffset"/>.
    /// </para>
    /// </remarks>
    public DateTimeOffset GetDateTimeOffset(string name) => GetRequiredStruct<DateTimeOffset>(name);


    /// <summary>
    /// Gets a nullable <c>datetimeoffset</c> output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns <c>null</c> when the parameter value is <see cref="DBNull"/>.
    /// </para>
    /// </remarks>
    public DateTimeOffset? GetNullableDateTimeOffset(string name) => GetNullableStruct<DateTimeOffset>(name);


    /// <summary>
    /// Gets a <c>time</c> output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Throws if the parameter does not exist, does not contain a value, or cannot be read as <see cref="TimeSpan"/>.
    /// </para>
    /// </remarks>
    public TimeSpan GetTime(string name) => GetRequiredStruct<TimeSpan>(name);


    /// <summary>
    /// Gets a nullable <c>time</c> output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns <c>null</c> when the parameter value is <see cref="DBNull"/>.
    /// </para>
    /// </remarks>
    public TimeSpan? GetNullableTime(string name) => GetNullableStruct<TimeSpan>(name);


    /// <summary>
    /// Gets a decimal-compatible output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this for <c>decimal</c>, <c>money</c>, and <c>smallmoney</c> output values.
    /// </para>
    /// </remarks>
    public decimal GetDecimal(string name) => GetRequiredStruct<decimal>(name);


    /// <summary>
    /// Gets a nullable decimal-compatible output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this for <c>decimal</c>, <c>money</c>, and <c>smallmoney</c> output values.
    /// </para>
    /// </remarks>
    public decimal? GetNullableDecimal(string name) => GetNullableStruct<decimal>(name);


    /// <summary>
    /// Gets a string-compatible output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this for <c>char</c>, <c>varchar</c>, <c>nchar</c>, <c>nvarchar</c>, and <c>xml</c> output values.
    /// </para>
    /// </remarks>
    public string GetString(string name) => GetRequiredReference<string>(name);


    /// <summary>
    /// Gets a nullable string-compatible output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns <c>null</c> when the parameter value is <see cref="DBNull"/>.
    /// </para>
    /// </remarks>
    public string? GetNullableString(string name) => GetNullableReference<string>(name);


    /// <summary>
    /// Gets a binary-compatible output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this for <c>binary</c> and <c>varbinary</c> output values.
    /// </para>
    /// </remarks>
    public byte[] GetBinary(string name) => GetRequiredReference<byte[]>(name);


    /// <summary>
    /// Gets a nullable binary-compatible output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns <c>null</c> when the parameter value is <see cref="DBNull"/>.
    /// </para>
    /// </remarks>
    public byte[]? GetNullableBinary(string name) => GetNullableReference<byte[]>(name);


    /// <summary>
    /// Gets a raw output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Throws if the parameter does not exist or does not contain a value.
    /// </para>
    /// </remarks>
    public object GetValue(string name)
    {
        var value = GetRawValue(name);

        return value is null ? throw new InvalidOperationException(MooErrorMessages.ParameterHasNoValue(name)) : value;
    }


    /// <summary>
    /// Gets a nullable raw output value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns <c>null</c> when the parameter value is <see cref="DBNull"/>.
    /// </para>
    /// </remarks>
    public object? GetNullableValue(string name) => GetRawValue(name);


    /// <summary>
    /// Gets the parameter at the specified index.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This allows <see cref="MooParams"/> to be used as an <see cref="IReadOnlyList{T}"/>
    /// when passed into MooDb execution methods.
    /// </para>
    /// </remarks>
    public SqlParameter this[int index] => _parameters[index];


    /// <summary>
    /// Gets the number of parameters in the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This allows <see cref="MooParams"/> to be used as an <see cref="IReadOnlyList{T}"/>
    /// when passed into MooDb execution methods.
    /// </para>
    /// </remarks>
    public int Count => _parameters.Count;


    /// <summary>
    /// Returns an enumerator for the parameter collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This allows <see cref="MooParams"/> to be enumerated as an <see cref="IReadOnlyList{T}"/>.
    /// </para>
    /// </remarks>
    public IEnumerator<SqlParameter> GetEnumerator() => _parameters.GetEnumerator();


    /// <summary>
    /// Returns a non-generic enumerator for the parameter collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This allows <see cref="MooParams"/> to be enumerated through the non-generic
    /// <see cref="IEnumerable"/> interface.
    /// </para>
    /// </remarks>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    // Internal helpers


    // Private helpers
    private MooParams AddSimple(
        string name,
        SqlDbType sqlDbType,
        object? value,
        ParameterDirection direction)
    {
        ValidateName(name);

        var parameter = new SqlParameter(name, sqlDbType)
        {
            Value = value ?? DBNull.Value,
            Direction = direction,
            IsNullable = true
        };

        AddInternal(parameter);
        return this;
    }

    private MooParams AddFixedLengthString(
        string name,
        SqlDbType sqlDbType,
        string? value,
        int size,
        ParameterDirection direction)
    {
        ValidateName(name);
        ValidateFixedLengthSize(size);

        var parameter = new SqlParameter(name, sqlDbType, size)
        {
            Value = (object?)value ?? DBNull.Value,
            Direction = direction,
            IsNullable = true
        };

        AddInternal(parameter);
        return this;
    }

    private MooParams AddVariableLengthString(
        string name,
        SqlDbType sqlDbType,
        string? value,
        int size,
        ParameterDirection direction)
    {
        ValidateName(name);
        ValidateVariableLengthSize(size);

        var parameter = new SqlParameter(name, sqlDbType, size)
        {
            Value = (object?)value ?? DBNull.Value,
            Direction = direction,
            IsNullable = true
        };

        AddInternal(parameter);
        return this;
    }

    private MooParams AddFixedLengthBinary(
        string name,
        SqlDbType sqlDbType,
        byte[]? value,
        int size,
        ParameterDirection direction)
    {
        ValidateName(name);
        ValidateFixedLengthSize(size);

        var parameter = new SqlParameter(name, sqlDbType, size)
        {
            Value = (object?)value ?? DBNull.Value,
            Direction = direction,
            IsNullable = true
        };

        AddInternal(parameter);
        return this;
    }

    private MooParams AddVariableLengthBinary(
        string name,
        SqlDbType sqlDbType,
        byte[]? value,
        int size,
        ParameterDirection direction)
    {
        ValidateName(name);
        ValidateVariableLengthSize(size);

        var parameter = new SqlParameter(name, sqlDbType, size)
        {
            Value = (object?)value ?? DBNull.Value,
            Direction = direction,
            IsNullable = true
        };

        AddInternal(parameter);
        return this;
    }

    private void AddInternal(SqlParameter parameter)
    {
        if (_lookup.ContainsKey(parameter.ParameterName))
        {
            throw new InvalidOperationException(MooErrorMessages.DuplicateParameter(parameter.ParameterName));
        }

        _parameters.Add(parameter);
        _lookup.Add(parameter.ParameterName, parameter);
    }

    private T GetRequiredStruct<T>(string name) where T : struct
    {
        var value = GetRawValue(name);

        if (value is null)
        {
            throw new InvalidOperationException(MooErrorMessages.ParameterHasNoValue(name));
        }

        if (value is T typed)
            return typed;

        throw new InvalidOperationException(MooErrorMessages.ParameterTypeMismatch(name, value.GetType().Name, typeof(T).Name));
    }

    private T? GetNullableStruct<T>(string name) where T : struct
    {
        var value = GetRawValue(name);

        if (value is null)
            return null;

        if (value is T typed)
            return typed;

        throw new InvalidOperationException(MooErrorMessages.ParameterTypeMismatch(name, value.GetType().Name, typeof(T).Name));
    }

    private T GetRequiredReference<T>(string name) where T : class
    {
        var value = GetRawValue(name) ?? throw new InvalidOperationException(MooErrorMessages.ParameterHasNoValue(name));
        if (value is T typed)
            return typed;

        throw new InvalidOperationException(MooErrorMessages.ParameterTypeMismatch(name, value.GetType().Name, typeof(T).Name));
    }

    private T? GetNullableReference<T>(string name) where T : class
    {
        var value = GetRawValue(name);

        if (value is null)
            return null;

        if (value is T typed)
            return typed;

        throw new InvalidOperationException(MooErrorMessages.ParameterTypeMismatch(name, value.GetType().Name, typeof(T).Name));
    }

    private object? GetRawValue(string name)
    {
        ValidateName(name);

        if (!_lookup.TryGetValue(name, out var parameter))
        {
            throw new InvalidOperationException(MooErrorMessages.ParameterNotFound(name));
        }

        return parameter.Value is DBNull ? null : parameter.Value;
    }

    private static void ValidateName(string name)
    {
        MooGuard.AgainstNullOrWhiteSpace(name, nameof(name), "Parameter name");
    }

    private static void ValidateScale(byte scale)
    {
        if (scale > 7)
        {
            throw new ArgumentOutOfRangeException(
                nameof(scale),
                "Scale must be between 0 and 7.");
        }
    }

    private static void ValidatePrecisionAndScale(byte precision, byte scale)
    {
        if (precision is < 1 or > 38)
        {
            throw new ArgumentOutOfRangeException(
                nameof(precision),
                "Precision must be between 1 and 38.");
        }

        if (scale > precision)
        {
            throw new ArgumentOutOfRangeException(
                nameof(scale),
                "Scale cannot be greater than precision.");
        }
    }

    private static void ValidateFixedLengthSize(int size)
    {
        if (size < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                "Size must be greater than zero.");
        }
    }

    private static void ValidateVariableLengthSize(int size)
    {
        if (size != -1 && size < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                "Size must be greater than zero or -1 for MAX.");
        }
    }
}