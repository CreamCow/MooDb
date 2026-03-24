using System.Data;
using Microsoft.Data.SqlClient;

namespace MooDb.Execution;

/// <summary>
/// Executes SQL Server commands for MooDb.
/// </summary>
/// <remarks>
/// <para>
/// Centralises command execution, connection opening, command creation, parameter cloning,
/// timeout resolution, and output parameter propagation.
/// </para>
/// <para>
/// This type is internal to MooDb and is shared by the stored procedure, SQL text,
/// and transaction execution surfaces.
/// </para>
/// </remarks>
internal sealed class MooCommandExecutor
{
    // Fields
    private readonly int _defaultCommandTimeoutSeconds;


    // Constructors
    /// <summary>
    /// Creates a new command executor with the specified default timeout.
    /// </summary>
    /// <param name="defaultCommandTimeoutSeconds">The default command timeout, in seconds.</param>
    internal MooCommandExecutor(int defaultCommandTimeoutSeconds)
    {
        if (defaultCommandTimeoutSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(defaultCommandTimeoutSeconds),
                "Command timeout must be zero or greater.");
        }

        _defaultCommandTimeoutSeconds = defaultCommandTimeoutSeconds;
    }


    // Public API
    /// <summary>
    /// Executes a database command within the supplied execution context.
    /// </summary>
    /// <typeparam name="TResult">The result produced by the supplied execution delegate.</typeparam>
    /// <param name="context">The execution context containing the connection, transaction, and ownership information.</param>
    /// <param name="commandText">The SQL text or stored procedure name to execute.</param>
    /// <param name="commandType">The command type to use.</param>
    /// <param name="parameters">Optional command parameters.</param>
    /// <param name="commandTimeoutSeconds">An optional per-command timeout override, in seconds.</param>
    /// <param name="execute">The delegate that executes the prepared <see cref="SqlCommand"/>.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    internal async Task<TResult> ExecuteAsync<TResult>(
        MooExecutionContext context,
        string commandText,
        CommandType commandType,
        IReadOnlyList<SqlParameter>? parameters,
        int? commandTimeoutSeconds,
        Func<SqlCommand, Task<TResult>> execute,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        MooGuard.AgainstNullOrWhiteSpace(commandText, nameof(commandText), "Command text");
        ArgumentNullException.ThrowIfNull(execute);

        var connection = context.Connection;
        List<(SqlParameter Source, SqlParameter Copy)>? parameterCopies = null;

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
                commandTimeoutSeconds,
                out parameterCopies);

            var result = await execute(command);

            CopyOutputValuesBack(parameterCopies);

            return result;
        }
        finally
        {
            if (context.OwnsConnection)
            {
                await connection.DisposeAsync();
            }
        }
    }


    // Internal helpers


    // Private helpers
    private SqlCommand CreateCommand(
        MooExecutionContext context,
        string commandText,
        CommandType commandType,
        IReadOnlyList<SqlParameter>? parameters,
        int? commandTimeoutSeconds,
        out List<(SqlParameter Source, SqlParameter Copy)> parameterCopies)
    {
        var command = context.Connection.CreateCommand();

        command.CommandText = commandText;
        command.CommandType = commandType;
        command.CommandTimeout = ResolveCommandTimeout(commandTimeoutSeconds);

        if (context.Transaction is not null)
        {
            command.Transaction = context.Transaction;
        }

        parameterCopies = new List<(SqlParameter Source, SqlParameter Copy)>();

        if (parameters is not null)
        {
            foreach (var parameter in parameters)
            {
                var copy = CloneParameter(parameter);
                command.Parameters.Add(copy);
                parameterCopies.Add((parameter, copy));
            }
        }

        return command;
    }

    private int ResolveCommandTimeout(int? commandTimeoutSeconds)
    {
        if (commandTimeoutSeconds is < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(commandTimeoutSeconds),
                "Command timeout must be zero or greater.");
        }

        return commandTimeoutSeconds ?? _defaultCommandTimeoutSeconds;
    }

    /// <summary>
    /// Creates a copy of the supplied <see cref="SqlParameter"/> for use on a command.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="SqlParameter"/> instances are mutable and become associated with a specific
    /// <see cref="SqlCommand"/> once added to its parameter collection.
    /// </para>
    /// <para>
    /// Reusing the same instance across multiple commands can lead to unexpected behaviour
    /// and runtime errors.
    /// </para>
    /// <para>
    /// MooDb treats input parameters as caller-owned. To ensure parameters can be safely reused,
    /// each parameter is cloned before being attached to a command.
    /// </para>
    /// <para>
    /// All relevant metadata is preserved, including type information, size, precision, scale,
    /// direction, and table-valued parameter metadata such as SQL Server type names.
    /// </para>
    /// </remarks>
    private static SqlParameter CloneParameter(SqlParameter source)
    {
        ArgumentNullException.ThrowIfNull(source);

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

    private static void CopyOutputValuesBack(
        IEnumerable<(SqlParameter Source, SqlParameter Copy)> parameterCopies)
    {
        foreach (var (source, copy) in parameterCopies)
        {
            if (source.Direction is ParameterDirection.Output
                or ParameterDirection.InputOutput
                or ParameterDirection.ReturnValue)
            {
                source.Value = copy.Value;
            }
        }
    }
}