using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;

namespace MooDb.Tests.Integration.Infrastructure.Fixtures;

public sealed class MooDbFixture : IAsyncLifetime
{
    private const string SqlServerInstance = "PHILLIPS-PC";
    private const string DatabaseName = "db_MooDb";
    private const string DacpacFileName = "MooDb.Tests.Database.dacpac";

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        ConnectionString = BuildTestDatabaseConnectionString();

        var masterConnectionString = BuildMasterConnectionString();
        var dacpacPath = ResolveDacpacPath();

        await RecreateDatabaseAsync(masterConnectionString);
        await PublishDacpacAsync(dacpacPath);
        await VerifyDeploymentAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public MooDb CreateMooDb()
    {
        return new MooDb(ConnectionString);
    }

    public MooDb CreateStrictMooDb()
    {
        return new MooDb(ConnectionString, new MooDbOptions
        {
            StrictAutoMapping = true
        });
    }

    public async Task<SqlConnection> CreateOpenConnectionAsync()
    {
        var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    public Task ResetAsync()
    {
        return ExecuteSqlAsync("EXEC [Tests].[usp_ResetDatabase];");
    }

    public Task<int> ExecuteSqlAsync(string sql)
    {
        return ExecuteSqlAsync(sql, Enumerable.Empty<SqlParameter>());
    }

    public async Task<int> ExecuteSqlAsync(string sql, IEnumerable<SqlParameter> parameters)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandType = CommandType.Text;

        foreach (var parameter in parameters)
        {
            command.Parameters.Add(CloneParameter(parameter));
        }

        return await command.ExecuteNonQueryAsync();
    }

    public Task<T?> ScalarSqlAsync<T>(string sql)
    {
        return ScalarSqlAsync<T>(sql, Enumerable.Empty<SqlParameter>());
    }

    public async Task<T?> ScalarSqlAsync<T>(string sql, IEnumerable<SqlParameter> parameters)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandType = CommandType.Text;

        foreach (var parameter in parameters)
        {
            command.Parameters.Add(CloneParameter(parameter));
        }

        var result = await command.ExecuteScalarAsync();

        if (result is null || result is DBNull)
        {
            return default;
        }

        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        return (T?)Convert.ChangeType(result, targetType);
    }

    private static string BuildMasterConnectionString()
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = SqlServerInstance,
            InitialCatalog = "master",
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            MultipleActiveResultSets = false
        };

        return builder.ConnectionString;
    }

    private static string BuildTestDatabaseConnectionString()
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = SqlServerInstance,
            InitialCatalog = DatabaseName,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            MultipleActiveResultSets = false
        };

        return builder.ConnectionString;
    }

    private static string ResolveDacpacPath()
    {
        var directPath = Path.Combine(AppContext.BaseDirectory, DacpacFileName);

        if (File.Exists(directPath))
        {
            return directPath;
        }

        var solutionRoot = FindSolutionRoot();

        var matches = Directory
            .EnumerateFiles(solutionRoot, DacpacFileName, SearchOption.AllDirectories)
            .Select(path => new FileInfo(path))
            .Where(file =>
                file.DirectoryName is not null &&
                !file.DirectoryName.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ToList();

        if (matches.Count > 0)
        {
            return matches[0].FullName;
        }

        throw new FileNotFoundException(
            $"Could not find '{DacpacFileName}' anywhere under '{solutionRoot}'. Build the database project first.");
    }

    private static string FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var testsPath = Path.Combine(directory.FullName, "tests");
            var srcPath = Path.Combine(directory.FullName, "src");

            if (Directory.Exists(testsPath) && Directory.Exists(srcPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the solution root from AppContext.BaseDirectory.");
    }

    private static async Task RecreateDatabaseAsync(string masterConnectionString)
    {
        var sql = $"""
                    IF DB_ID(N'{DatabaseName}') IS NOT NULL
                    BEGIN
                        ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        DROP DATABASE [{DatabaseName}];
                    END;

                    CREATE DATABASE [{DatabaseName}];
                    """;

        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandType = CommandType.Text;

        await command.ExecuteNonQueryAsync();
    }

    private static async Task PublishDacpacAsync(string dacpacPath)
    {
        var sqlPackagePath = ResolveSqlPackagePath();

        var targetConnectionString = BuildTestDatabaseConnectionString();

        var startInfo = new ProcessStartInfo
        {
            FileName = sqlPackagePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("/Action:Publish");
        startInfo.ArgumentList.Add($"/SourceFile:{dacpacPath}");
        startInfo.ArgumentList.Add($"/TargetConnectionString:{targetConnectionString}");
        startInfo.ArgumentList.Add("/p:BlockOnPossibleDataLoss=False");
        startInfo.ArgumentList.Add("/p:DropObjectsNotInSource=False");

        using var process = new Process { StartInfo = startInfo };

        process.Start();

        var standardOutput = await process.StandardOutput.ReadToEndAsync();
        var standardError = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"SqlPackage publish failed with exit code {process.ExitCode}.{Environment.NewLine}" +
                $"Executable: {sqlPackagePath}{Environment.NewLine}" +
                $"DACPAC: {dacpacPath}{Environment.NewLine}" +
                $"Output:{Environment.NewLine}{standardOutput}{Environment.NewLine}" +
                $"Error:{Environment.NewLine}{standardError}");
        }
    }

    private static string ResolveSqlPackagePath()
    {
        var pathVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

        foreach (var segment in pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(segment.Trim(), "SqlPackage.exe");

            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        var candidates = new[]
        {
            Path.Combine(programFiles, "Microsoft SQL Server", "170", "DAC", "bin", "SqlPackage.exe"),
            Path.Combine(programFiles, "Microsoft SQL Server", "160", "DAC", "bin", "SqlPackage.exe"),
            Path.Combine(programFiles, "Microsoft SQL Server", "150", "DAC", "bin", "SqlPackage.exe"),
            Path.Combine(programFilesX86, "Microsoft SQL Server", "170", "DAC", "bin", "SqlPackage.exe"),
            Path.Combine(programFilesX86, "Microsoft SQL Server", "160", "DAC", "bin", "SqlPackage.exe"),
            Path.Combine(programFilesX86, "Microsoft SQL Server", "150", "DAC", "bin", "SqlPackage.exe"),
            Path.Combine(programFiles, "Microsoft Visual Studio", "2022", "Community", "Common7", "IDE", "Extensions", "Microsoft", "SQLDB", "DAC", "SqlPackage.exe"),
            Path.Combine(programFiles, "Microsoft Visual Studio", "2022", "Professional", "Common7", "IDE", "Extensions", "Microsoft", "SQLDB", "DAC", "SqlPackage.exe"),
            Path.Combine(programFiles, "Microsoft Visual Studio", "2022", "Enterprise", "Common7", "IDE", "Extensions", "Microsoft", "SQLDB", "DAC", "SqlPackage.exe")
        };

        var match = candidates.FirstOrDefault(File.Exists);

        if (match is not null)
        {
            return match;
        }

        throw new FileNotFoundException(
            "Could not find SqlPackage.exe. Install SqlPackage or SQL Server Data Tools and ensure SqlPackage.exe is available.");
    }

    private async Task VerifyDeploymentAsync()
    {
        var objectId = await ScalarSqlAsync<int?>(
            "SELECT OBJECT_ID(N'[Tests].[usp_ResetDatabase]');");

        if (objectId is null or 0)
        {
            throw new InvalidOperationException(
                "Database deployment verification failed. Expected [Tests].[usp_ResetDatabase] to exist.");
        }
    }

    private static SqlParameter CloneParameter(SqlParameter parameter)
    {
        return (SqlParameter)((ICloneable)parameter).Clone();
    }
}