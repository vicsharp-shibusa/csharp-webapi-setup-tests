﻿using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Data;
using TestControl.AppServices;
using TestControl.Infrastructure;
using TestControl.Infrastructure.Database;
using TestControl.Infrastructure.SubjectApiPublic;

namespace Test.Alpha.IntegrationTests;

public abstract class TestBase
{
    protected DbProperties DbProperties { get; }
    protected readonly Random _rnd = new();
    protected SqlProvider _sqlProvider;

    protected TestBase()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("secrets.json", optional: true, reloadOnChange: false)
            .Build();

        var dbEngineSetting = configuration["databaseEngine"]
            ?? throw new InvalidOperationException("DatabaseEngine setting not found in configuration.");
        var dbVersionSetting = configuration["databaseVersion"]
            ?? throw new InvalidOperationException("DatabaseVersion setting not found in configuration.");

        if (!dbVersionSetting.StartsWith("db", StringComparison.OrdinalIgnoreCase) ||
            !int.TryParse(dbVersionSetting.Substring(2), out int version))
        {
            throw new InvalidOperationException($"Invalid database version: {dbVersionSetting}");
        }

        if (!Enum.TryParse<DbEngine>(dbEngineSetting, true, out var dbEngine))
        {
            throw new InvalidOperationException($"Invalid db engine: '{dbEngineSetting}'");
        }

        var cmdKey = $"{dbEngine}Command";
        var queryKey = $"{dbEngine}Query";

        var cmdConnectionString = configuration.GetConnectionString(cmdKey)
                           ?? throw new InvalidOperationException($"Connection string '{cmdKey}' not found.");
        var queryConnectionString = configuration.GetConnectionString(queryKey)
                           ?? throw new InvalidOperationException($"Connection string '{queryKey}' not found.");

        DbProperties = new DbProperties()
        {
            DbEngine = dbEngine,
            CommandConnection = dbEngine == DbEngine.MSSQL
                ? new SqlConnection(cmdConnectionString)
                : new NpgsqlConnection(cmdConnectionString),
            QueryConnection = dbEngine == DbEngine.MSSQL
                ? new SqlConnection(queryConnectionString)
                : new NpgsqlConnection(queryConnectionString)
        };

        _sqlProvider = StartupServices.GetSqlProvider(dbEngineSetting, version);
    }
    
}

