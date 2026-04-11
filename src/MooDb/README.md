![MooDb](https://raw.githubusercontent.com/CreamCow/MooDb/main/images/moodb-logo-and-wordmark-png.png)

# MooDb™

MooDb is a small, explicit micro ORM for SQL Server.

It is designed for developers who want full control over their database, with a clean and predictable API over ADO.NET, without the weight or hidden behaviour of a large ORM.

MooDb is **stored procedure first**, supports **raw SQL when needed**, and keeps database access explicit and easy to reason about.

MooDb is free to use, now and always, in both personal and commercial projects.

## Why MooDb

MooDb is built around a few simple ideas:

- **Stored procedure first** by default
- **Raw SQL is available** through an explicit `Sql` surface
- **Small public API** that is easy to learn and hard to misuse
- **Predictable mapping** for classes, constructor-based models, and simple types
- **Strict auto-mapping** when you want result shapes checked more closely
- **Custom mapping** when you want full control
- **Multiple results** without making the caller manage live readers
- **Transactions** with the same general shape as the main entry point
- **Bulk loading** for fast back-office and batch data movement
- **No forced abstraction layer** for ordinary usage

MooDb stays close to ADO.NET while removing repetitive plumbing and ceremony.

## Installation

```xml
<PackageReference Include="MooDb" Version="x.y.z" />
```

MooDb targets SQL Server and is built on `Microsoft.Data.SqlClient`.

## Quick start

```csharp
using MooDb;

// lightweight entry point
var db = new MooDbContext(connectionString);

var user = await db.SingleAsync<User>(
    "dbo.usp_User_GetById",
    new MooParams()
        .AddInt("@UserId", 42));
```

MooDb is designed to keep database access explicit and predictable.

For raw SQL, use the explicit `Sql` surface:

```csharp
var user = await db.Sql.SingleAsync<User>(
    "select UserId, DisplayName from dbo.tbl_User where UserId = @UserId",
    new MooParams()
        .AddInt("@UserId", 42));
```

## Main concepts

- `MooDbContext` is the main entry point (not a unit of work)
- `MooDbContext.Sql` is the raw SQL escape hatch
- `MooTransaction` is the transactional entry point
- `MooParams` builds SQL Server parameters fluently
- `IMooMultiReader` handles multiple results from one execution
- `MooBulk` handles SQL Server bulk loading
- `MooDbContextOptions` controls timeout and strict auto-mapping
- `AddMooDbContextFactory()` registers factory support for dependency injection

## Lifetime and disposal

`MooDbContext` is not a unit of work and is not disposed.

Each call executes using its own connection internally.  
When you need a unit of work, use a transaction:

```csharp
await using var tran = await db.BeginTransactionAsync();
```

`MooTransaction` is the disposable unit.  
If it is disposed without being committed, it is rolled back automatically.

## Documentation

Full documentation is available at:

https://moodb.net

Recommended starting points:

- Start Here
- Philosophy
- Creating a MooDb Instance
- Your First Stored Procedure Call

Licensed under MIT. MooDb will always be free to use.
