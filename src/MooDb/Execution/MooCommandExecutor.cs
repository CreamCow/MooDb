using Microsoft.Data.SqlClient;
using System.Data;

namespace MooDb.Execution;

internal sealed class MooCommandExecutor
{
    private readonly int _defaultCommandTimeoutSeconds;

    public MooCommandExecutor(int defaultCommandTimeoutSeconds)
    {
        _defaultCommandTimeoutSeconds = defaultCommandTimeoutSeconds;
    }

    public async Task<TResult> ExecuteAsync<TResult>(
        MooExecutionContext context,
        string commandText,
        CommandType commandType,
        IReadOnlyList<SqlParameter>? parameters,
        int? commandTimeoutSeconds,
        Func<SqlCommand, Task<TResult>> execute,
        CancellationToken cancellationToken)
    {
        var connection = context.Connection;

        try
        {
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var command = CreateCommand(
                context,
                commandText,
                commandType,
                parameters,
                commandTimeoutSeconds);

            return await execute(command);
        }
        finally
        {
            if (context.OwnsConnection)
            {
                await connection.DisposeAsync();
            }
        }
    }


    private SqlCommand CreateCommand(
        MooExecutionContext context,
        string commandText,
        CommandType commandType,
        IReadOnlyList<SqlParameter>? parameters,
        int? commandTimeoutSeconds)
    {
        var command = context.Connection.CreateCommand();

        command.CommandText = commandText;
        command.CommandType = commandType;

        if (context.Transaction is not null)
        {
            command.Transaction = context.Transaction;
        }

        command.CommandTimeout = ResolveCommandTimeout(commandTimeoutSeconds);

        if (parameters is not null)
        {
            foreach (var parameter in parameters)
            {
                command.Parameters.Add(CloneParameter(parameter));
            }
        }

        return command;
    }


    private int ResolveCommandTimeout(int? commandTimeoutSeconds)
    {
        return commandTimeoutSeconds ?? _defaultCommandTimeoutSeconds;
    }


    /// <summary>
    /// Creates a copy of the supplied <see cref="SqlParameter"/> for use on a command.
    /// </summary>
    /// <remarks>
    /// SqlParameter instances are mutable and become associated with a specific SqlCommand
    /// once added to its Parameters collection. Reusing the same instance across multiple
    /// commands can lead to unexpected behaviour and runtime errors.
    ///
    /// MooDb treats input parameters as caller-owned. To ensure parameters can be safely
    /// reused (e.g. when using MooParams across multiple calls), we clone each parameter
    /// before attaching it to a command.
    ///
    /// All relevant metadata is preserved, including type information, size, precision,
    /// scale, direction, and structured (TVP) settings.
    /// </remarks>
    private static SqlParameter CloneParameter(SqlParameter source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        var clone = new SqlParameter
        {
            ParameterName = source.ParameterName,
            Value = source.Value ?? DBNull.Value,
            Direction = source.Direction,

            SqlDbType = source.SqlDbType,
            DbType = source.DbType,

            Size = source.Size,
            Precision = source.Precision,
            Scale = source.Scale,

            IsNullable = source.IsNullable,

            SourceColumn = source.SourceColumn,
            SourceVersion = source.SourceVersion,
            SourceColumnNullMapping = source.SourceColumnNullMapping,

            TypeName = source.TypeName,
            UdtTypeName = source.UdtTypeName
        };

        return clone;
    }
}