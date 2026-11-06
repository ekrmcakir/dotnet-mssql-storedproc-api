using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InventoryHub.Infrastructure.Data;

public class DbInitializer
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(IConfiguration configuration, ILogger<DbInitializer> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        var rawConnectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not configured.");

        // Build master connection string to check/create database
        var builder = new SqlConnectionStringBuilder(rawConnectionString);
        var targetDatabase = builder.InitialCatalog;
        builder.InitialCatalog = "master";
        var masterConnectionString = builder.ConnectionString;

        _logger.LogInformation("Verifying database existence for '{Database}'...", targetDatabase);

        // 1. Ensure Database Exists (Retry up to 10 times while SQL container finishes booting)
        bool dbReady = false;
        for (int i = 0; i < 15; i++)
        {
            try
            {
                using var masterConn = new SqlConnection(masterConnectionString);
                await masterConn.OpenAsync();

                using var cmd = masterConn.CreateCommand();
                cmd.CommandText = $@"
                    IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = '{targetDatabase}')
                    BEGIN
                        CREATE DATABASE [{targetDatabase}];
                    END";
                await cmd.ExecuteNonQueryAsync();
                dbReady = true;
                break;
            }
            catch (SqlException ex)
            {
                _logger.LogWarning("SQL Server not ready yet (Attempt {Attempt}/15): {Message}. Retrying...", i + 1, ex.Message);
                await Task.Delay(2000);
            }
        }

        if (!dbReady)
        {
            throw new InvalidOperationException($"Could not connect to SQL Server to initialize database '{targetDatabase}'.");
        }

        // 2. Connect to target database and execute scripts if tables do not exist
        using var targetConn = new SqlConnection(rawConnectionString);
        await targetConn.OpenAsync();

        bool hasTables;
        using (var checkCmd = targetConn.CreateCommand())
        {
            checkCmd.CommandText = "SELECT COUNT(1) FROM sys.tables WHERE name = 'Products'";
            hasTables = ((int?)await checkCmd.ExecuteScalarAsync() ?? 0) > 0;
        }

        if (hasTables)
        {
            _logger.LogInformation("Database '{Database}' is already initialized.", targetDatabase);
            return;
        }

        _logger.LogInformation("Initializing schema, stored procedures, and seed data for '{Database}'...", targetDatabase);

        // Locate database scripts directory
        var baseDir = AppContext.BaseDirectory;
        var projectRoot = FindProjectRoot(baseDir);
        var databaseDir = Path.Combine(projectRoot, "database");

        var scriptFiles = new[]
        {
            Path.Combine(databaseDir, "01_init_schema.sql"),
            Path.Combine(databaseDir, "02_stored_procedures.sql"),
            Path.Combine(databaseDir, "03_seed_data.sql")
        };

        foreach (var file in scriptFiles)
        {
            if (!File.Exists(file))
            {
                _logger.LogWarning("Script file '{File}' not found, skipping.", file);
                continue;
            }

            _logger.LogInformation("Executing script: {FileName}...", Path.GetFileName(file));
            var script = await File.ReadAllTextAsync(file);
            await ExecuteScriptBatchesAsync(targetConn, script);
        }

        _logger.LogInformation("Database '{Database}' initialization and seeding completed successfully!", targetDatabase);
    }

    private static async Task ExecuteScriptBatchesAsync(SqlConnection connection, string fullScript)
    {
        // Split T-SQL script by GO batch separator
        var batches = Regex.Split(fullScript, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

        foreach (var batch in batches)
        {
            var trimmed = batch.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            // Remove USE Database commands since we are already connected to target database
            if (trimmed.StartsWith("USE ", StringComparison.OrdinalIgnoreCase)) continue;

            using var cmd = connection.CreateCommand();
            cmd.CommandText = trimmed;
            cmd.CommandTimeout = 60;
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static string FindProjectRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "InventoryHub.sln")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return startDir;
    }
}

