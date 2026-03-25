using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Data.SqlClient;
using MooDb.Execution;

namespace MooDb.Bulk;

/// <summary>
/// Provides bulk insert operations for SQL Server tables.
/// </summary>
/// <remarks>
/// <para>
/// Bulk insert copies many rows directly into a SQL Server table efficiently.
/// </para>
/// <para>
/// Use this API when you want to load rows straight into a table rather than pass them
/// into stored procedure logic using table-valued parameters.
/// </para>
/// </remarks>
public sealed class MooBulk
{
    private readonly Func<MooExecutionContext> _contextFactory;

    internal MooBulk(Func<MooExecutionContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    /// <summary>
    /// Bulk inserts the supplied <see cref="DataTable"/> into the specified destination table.
    /// </summary>
    /// <param name="tableName">The destination table name.</param>
    /// <param name="dataTable">The rows to insert.</param>
    /// <param name="options">Optional bulk insert settings.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    public async Task WriteToTableAsync(
        string tableName,
        DataTable dataTable,
        MooBulkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        MooGuard.AgainstNullOrWhiteSpace(tableName, nameof(tableName), "Table name");
        ArgumentNullException.ThrowIfNull(dataTable);

        options ??= new MooBulkOptions();

        var context = _contextFactory();
        var connection = context.Connection;
        var transaction = context.Transaction;

        try
        {
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(options.PreparationSql))
            {
                await ExecuteSqlAsync(
                    connection,
                    transaction,
                    options.PreparationSql,
                    cancellationToken);
            }

            using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
            {
                bulkCopy.DestinationTableName = tableName;

                if (options.BatchSize.HasValue)
                {
                    bulkCopy.BatchSize = options.BatchSize.Value;
                }

                if (options.BulkCopyTimeoutSeconds.HasValue)
                {
                    bulkCopy.BulkCopyTimeout = options.BulkCopyTimeoutSeconds.Value;
                }

                foreach (DataColumn column in dataTable.Columns)
                {
                    bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                }

                await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(options.CleanupSql))
            {
                await ExecuteSqlAsync(
                    connection,
                    transaction,
                    options.CleanupSql,
                    cancellationToken);
            }
        }
        finally
        {
            if (context.OwnsConnection)
            {
                await connection.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Bulk inserts the supplied rows into the specified destination table.
    /// </summary>
    /// <typeparam name="T">The row type.</typeparam>
    /// <param name="tableName">The destination table name.</param>
    /// <param name="rows">The rows to insert.</param>
    /// <param name="options">Optional bulk insert settings.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    public Task WriteToTableAsync<T>(
        string tableName,
        IEnumerable<T> rows,
        MooBulkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        MooGuard.AgainstNullOrWhiteSpace(tableName, nameof(tableName), "Table name");
        ArgumentNullException.ThrowIfNull(rows);

        var dataTable = CreateDataTable(rows);

        return WriteToTableAsync(tableName, dataTable, options, cancellationToken);
    }

    private static async Task ExecuteSqlAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandType = CommandType.Text;
        command.CommandText = sql;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static DataTable CreateDataTable<T>(IEnumerable<T> rows)
    {
        var map = TypedBulkMap<T>.Instance;
        var table = new DataTable();

        foreach (var column in map.Columns)
        {
            table.Columns.Add(column.Name, column.ColumnType);
        }

        foreach (var row in rows)
        {
            ArgumentNullException.ThrowIfNull(row);

            var values = new object?[map.Columns.Length];

            for (var i = 0; i < map.Columns.Length; i++)
            {
                values[i] = map.Columns[i].GetValue(row) ?? DBNull.Value;
            }

            table.Rows.Add(values);
        }

        return table;
    }

    private sealed class TypedBulkMap<T>
    {
        internal static readonly TypedBulkMap<T> Instance = Create();

        internal required BulkColumn<T>[] Columns { get; init; }

        private static TypedBulkMap<T> Create()
        {
            var properties = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .ToArray();

            if (properties.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Type '{typeof(T).Name}' must expose at least one public readable instance property for bulk insert.");
            }

            var columns = properties
                .Select(CreateBulkColumn)
                .ToArray();

            return new TypedBulkMap<T>
            {
                Columns = columns
            };
        }

        private static BulkColumn<T> CreateBulkColumn(PropertyInfo property)
        {
            var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var columnType = propertyType.IsEnum
                ? Enum.GetUnderlyingType(propertyType)
                : propertyType;

            var getter = CreateGetter(property, propertyType.IsEnum);

            return new BulkColumn<T>(property.Name, columnType, getter);
        }

        private static Func<T, object?> CreateGetter(PropertyInfo property, bool convertEnum)
        {
            var instance = Expression.Parameter(typeof(T), "instance");
            var propertyAccess = Expression.Property(instance, property);
            Expression body = propertyAccess;

            if (convertEnum)
            {
                var nullableEnumType = Nullable.GetUnderlyingType(property.PropertyType);

                if (nullableEnumType is not null)
                {
                    var underlyingType = Enum.GetUnderlyingType(nullableEnumType);

                    body = Expression.Condition(
                        Expression.Equal(propertyAccess, Expression.Constant(null, property.PropertyType)),
                        Expression.Constant(null, typeof(object)),
                        Expression.Convert(Expression.Convert(propertyAccess, underlyingType), typeof(object)));

                    return Expression.Lambda<Func<T, object?>>(body, instance).Compile();
                }

                var enumUnderlyingType = Enum.GetUnderlyingType(property.PropertyType);
                body = Expression.Convert(Expression.Convert(propertyAccess, enumUnderlyingType), typeof(object));

                return Expression.Lambda<Func<T, object?>>(body, instance).Compile();
            }

            body = Expression.Convert(body, typeof(object));

            return Expression.Lambda<Func<T, object?>>(body, instance).Compile();
        }
    }

    private sealed record BulkColumn<T>(string Name, Type ColumnType, Func<T, object?> GetValue);
}