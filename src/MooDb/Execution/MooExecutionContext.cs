using Microsoft.Data.SqlClient;

namespace MooDb.Execution;

internal sealed class MooExecutionContext
{
    public SqlConnection Connection { get; }
    public SqlTransaction? Transaction { get; }
    public bool OwnsConnection { get; }

    public MooExecutionContext(
        SqlConnection connection,
        SqlTransaction? transaction,
        bool ownsConnection)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        Transaction = transaction;
        OwnsConnection = ownsConnection;
    }
}
