# MooDb

MooDb is a small, explicit micro ORM for SQL Server.

It is designed for teams who want a clean API over `SqlConnection` and `SqlCommand` without taking on the complexity of a full ORM. MooDb is **stored procedure first**, supports **raw SQL when needed**, and keeps mapping predictable.

## Why MooDb

MooDb is built around a few simple ideas:

- **Stored procedure first** by default
- **Raw SQL is available** through an explicit `Sql` surface
- **Small public API** that is easy to learn and hard to misuse
- **Predictable mapping** for classes, records, and simple types
- **Custom mapping** when you want full control or extra performance
- **Transactions** with the same core API shape as the main entry point
- **No broad framework abstraction** forced onto consumers

MooDb aims to stay close to ADO.NET while removing repetitive plumbing.

---

## Features

- Execute commands that do not return result sets
- Read scalar values
- Read a single row or a list of rows
- Read multiple result sets sequentially
- Auto-map to classes and records
- Provide a custom mapper with `Func<SqlDataReader, T>`
- Use caller-managed or MooDb-managed connections
- Override command timeout per call or globally
- Run commands inside a transaction
- Switch to raw SQL explicitly via `db.Sql` or `transaction.Sql`

---

## Installation

Add the package reference to your project.

```xml
<PackageReference Include="MooDb" Version="x.y.z" />
```

MooDb targets SQL Server and depends on:

- `Microsoft.Data.SqlClient`

---

## Quick start

### Create a MooDb instance

Using a connection string:

```csharp
using MooDb;

var db = new MooDb(connectionString);
```

Using an existing connection:

```csharp
using Microsoft.Data.SqlClient;
using MooDb;

await using var connection = new SqlConnection(connectionString);

var db = new MooDb(connection);
```

When you pass a connection string, MooDb manages connection creation for each operation.
When you pass an existing `SqlConnection`, you manage the connection lifetime.

---

## Parameters

Use `MooParams` to build SQL Server parameters fluently.

```csharp
using MooDb;

var parameters = new MooParams()
    .AddIn("@UserId", 42)
    .AddOut("@RowsAffected", System.Data.SqlDbType.Int);
```

Pass `MooParams` anywhere an `IReadOnlyList<SqlParameter>` is accepted.

---

## Common operations

### Execute a stored procedure

```csharp
var rowsAffected = await db.ExecuteAsync(
    "Users_UpdateEmail",
    new MooParams()
        .AddIn("@UserId", 42)
        .AddIn("@Email", "ada@example.com"));
```

### Read a scalar value

```csharp
var count = await db.ScalarAsync<int>(
    "Users_CountActive");
```

Use a nullable type when absence needs to be preserved distinctly from a non-null default value:

```csharp
int? count = await db.ScalarAsync<int?>(
    "Users_CountActive");
```

### Read a single row with auto-mapping

```csharp
public sealed class User
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

var user = await db.SingleAsync<User>(
    "Users_GetById",
    new MooParams().AddIn("@UserId", 42));
```

### Read a list with auto-mapping

```csharp
var users = await db.ListAsync<User>(
    "Users_ListActive");
```

---

## Auto-mapping

MooDb auto-maps by convention.

It supports:

- classes with public settable properties
- records or other types with matching constructor parameters
- case-insensitive name matching
- compatible type conversion where possible

Example using a record:

```csharp
public sealed record UserSummary(int UserId, string Name, string Email);

var users = await db.ListAsync<UserSummary>("Users_ListActive");
```

### Strict auto-mapping

You can enable strict auto-mapping through `MooDbOptions`.

```csharp
var db = new MooDb(connectionString, new MooDbOptions
{
    StrictAutoMapping = true
});
```

When enabled, MooDb throws if the result set shape does not match the target type closely enough.

---

## Custom mapping

When convention mapping is not enough, or when you want tighter control, use a custom mapper.

```csharp
var users = await db.ListAsync(
    "Users_ListActive",
    static reader => new User
    {
        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
        Name = reader.GetString(reader.GetOrdinal("Name")),
        Email = reader.GetString(reader.GetOrdinal("Email"))
    });
```

Custom mapping is useful when:

- column names do not line up with property names
- the result shape is irregular
- you want fully explicit materialisation
- you want reusable mapping logic
- you want to reduce mapping overhead on hot paths

### Reusable mapper example

```csharp
using Microsoft.Data.SqlClient;

public static class UserMap
{
    public static User Map(SqlDataReader reader) => new()
    {
        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
        Name = reader.GetString(reader.GetOrdinal("Name")),
        Email = reader.GetString(reader.GetOrdinal("Email"))
    };
}

var user = await db.SingleAsync(
    "Users_GetById",
    UserMap.Map,
    new MooParams().AddIn("@UserId", 42));

var users = await db.ListAsync(
    "Users_ListActive",
    UserMap.Map);
```

---

## Raw SQL

MooDb is stored procedure first, but raw SQL is available through the `Sql` property.

```csharp
var user = await db.Sql.SingleAsync<User>(
    """
    select UserId, Name, Email
    from dbo.Users
    where UserId = @UserId
    """,
    new MooParams().AddIn("@UserId", 42));
```

This keeps SQL text usage explicit in calling code.

---

## Multiple result sets

Use `QueryMultipleAsync` when a command returns more than one result set.

```csharp
await using var results = await db.QueryMultipleAsync(
    "Dashboard_Get",
    new MooParams().AddIn("@UserId", 42));

var user = await results.ReadSingleAsync<User>();
var notifications = await results.ReadAsync<Notification>();
var tasks = await results.ReadAsync<TaskItem>();
```

Result sets are read sequentially and cannot be revisited.

---

## Transactions

Start a transaction with `BeginTransactionAsync`.

```csharp
await using var tx = await db.BeginTransactionAsync();

await tx.ExecuteAsync(
    "Users_UpdateEmail",
    new MooParams()
        .AddIn("@UserId", 42)
        .AddIn("@Email", "new@example.com"));

await tx.ExecuteAsync(
    "Audit_Insert",
    new MooParams()
        .AddIn("@EventType", "EmailChanged")
        .AddIn("@UserId", 42));

await tx.CommitAsync();
```

Notes:

- all commands in the transaction share the same connection and SQL transaction
- if the transaction is disposed without commit, it is rolled back
- `tx.Sql` is available for raw SQL inside the same transaction

---

## Configuration

Use `MooDbOptions` to configure default behaviour.

```csharp
var db = new MooDb(connectionString, new MooDbOptions
{
    CommandTimeoutSeconds = 60,
    StrictAutoMapping = false
});
```

### Available options

- `CommandTimeoutSeconds` - default timeout applied when a per-call timeout is not supplied
- `StrictAutoMapping` - enables stricter validation of result set shape during auto-mapping

You can also override the timeout per call:

```csharp
var users = await db.ListAsync<User>(
    "Users_ListActive",
    commandTimeoutSeconds: 120);
```

---

## API shape

The primary public API is intentionally small.

### `MooDb`

Stored procedure entry point:

- `ExecuteAsync`
- `ScalarAsync<T>`
- `SingleAsync<T>`
- `SingleAsync<T>(..., Func<SqlDataReader, T> map, ...)`
- `ListAsync<T>`
- `ListAsync<T>(..., Func<SqlDataReader, T> map, ...)`
- `QueryMultipleAsync`
- `BeginTransactionAsync`
- `Sql`

### `MooTransaction`

Transaction entry point with the same core query surface, plus:

- `CommitAsync`
- `Sql`

### `MooResults`

Sequential access to multiple result sets.

### `MooParams`

Fluent parameter builder for SQL Server parameters.

### `MooDbOptions`

Configuration for command timeout and mapping behaviour.

---

## Testing guidance

MooDb intentionally uses a concrete main entry point rather than a broad `IMooDb` interface.

For most applications, the recommended testing approach is to abstract your own repository or service boundary, for example:

```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);
}
```

Use MooDb inside the production implementation, and substitute the repository or service in unit tests.

This keeps MooDb's API smaller and easier to evolve.

---

## Design goals

MooDb is opinionated in a few deliberate ways:

- it does not try to be a full ORM
- it does not hide that you are using SQL Server
- it does not make raw SQL the default surface
- it does not require consumers to adopt framework-heavy patterns

The goal is a practical middle ground:

- less ceremony than hand-written ADO.NET
- more control than a heavy ORM
- cleaner calling code without hiding what is happening

---

## Build

```bash
dotnet build
```

## Test

```bash
dotnet test
```

---

## Status

MooDb is near-complete and focused on public API clarity, predictable behaviour, and adoption-friendly documentation.
