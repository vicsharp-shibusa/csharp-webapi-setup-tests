using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TestControl.AppServices"),
    InternalsVisibleTo("TestControl.Infrastructure.Tests")]
namespace TestControl.Infrastructure.Database;


/// <summary>
/// Contains the keys for referencing SQL statements from the dictionary created in
/// <see cref="SqlRepository"/>.
/// </summary>
public static class SqlKeys
{
    public static IEnumerable<string> GetKeys()
    {
        foreach (var f in typeof(SqlKeys).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (f.IsLiteral && f.DeclaringType == typeof(SqlKeys) && f.FieldType == typeof(string))
            {
                yield return (string)f.GetRawConstantValue();
            }
        }
    }

    public const string DeleteAllData = nameof(DeleteAllData);
    public const string GetAllOrganizations = nameof(GetAllOrganizations);
    public const string GetAllOrganizationsReport = nameof(GetAllOrganizationsReport);
    public const string GetTableCounts = nameof(GetTableCounts);
    public const string GetOrganizationById = nameof(GetOrganizationById);
    public const string GetOrganizationForUser = nameof(GetOrganizationForUser);
    public const string GetOrganizationHierarchyReport = nameof(GetOrganizationHierarchyReport);
    public const string GetOrganizationsForUser = nameof(GetOrganizationsForUser);
    public const string GetParentOrgIdForOrgId = nameof(GetParentOrgIdForOrgId);
    public const string GetTransactionById = nameof(GetTransactionById);
    public const string GetTransactionsForOrganization = nameof(GetTransactionsForOrganization);
    public const string GetTransactionsForOrgWithDateRange = nameof(GetTransactionsForOrgWithDateRange);
    public const string GetTransactionsForUserWithDateRange = nameof(GetTransactionsForUserWithDateRange);
    public const string GetTransactionSummaryReport = nameof(GetTransactionSummaryReport);
    public const string GetUserActivityReport = nameof(GetUserActivityReport);
    public const string GetUserByEmail = nameof(GetUserByEmail);
    public const string GetUserById = nameof(GetUserById);
    public const string GetUserIdForEmail = nameof(GetUserIdForEmail);
    public const string GetUsersForOrganization = nameof(GetUsersForOrganization);
    public const string InsertOrganizationUser = nameof(InsertOrganizationUser);
    public const string InsertUser = nameof(InsertUser);
    public const string PurgeOrganizations = nameof(PurgeOrganizations);
    public const string PurgeUsers = nameof(PurgeUsers);
    public const string UpsertOrganization = nameof(UpsertOrganization);
    public const string UpsertOrganizationUser = nameof(UpsertOrganizationUser);
    public const string UpsertTransaction = nameof(UpsertTransaction);
    public const string UpsertUser = nameof(UpsertUser);
}

/// <summary>
/// Represents the unique combination of db engine/version and the necessary SQL.
/// </summary>
internal readonly struct SqlRepoKey
{
    public SqlRepoKey(string sqlKey, DbEngine dbEngine = DbEngine.PostgreSQL, int sqlVersion = 1)
    {
        SqlKey = sqlKey ?? throw new ArgumentNullException(nameof(sqlKey));
        DbEngine = dbEngine;
        SqlVersion = Math.Max(1, sqlVersion);
    }

    public DbEngine DbEngine { get; init; } = DbEngine.PostgreSQL;
    public int SqlVersion { get; init; } = 1;
    public string SqlKey { get; init; }
}

/*
 * Repository of all SQL.
 * Consider making this a `partial` class.
 */
internal class SqlRepository
{
    private readonly Dictionary<SqlRepoKey, string> _sqlDictionary = new()
    {
        // User Queries
        { new SqlRepoKey(SqlKeys.GetUserIdForEmail, DbEngine.MSSQL, 1),
            "SELECT UserId FROM Users WHERE Email = @Email" },
        { new SqlRepoKey(SqlKeys.GetUserIdForEmail, DbEngine.PostgreSQL, 1),
            "SELECT userid FROM users WHERE email = @Email" },

        { new SqlRepoKey(SqlKeys.GetUserByEmail, DbEngine.MSSQL, 1),
            "SELECT UserId, Name, Email, CreatedAt FROM Users WHERE Email = @Email" },
        { new SqlRepoKey(SqlKeys.GetUserByEmail, DbEngine.PostgreSQL, 1),
            "SELECT userid, name, email, createdat FROM users WHERE email = @Email" },

        { new SqlRepoKey(SqlKeys.GetUserById, DbEngine.MSSQL, 1),
            "SELECT UserId, Name, Email, CreatedAt FROM Users WHERE UserId = @UserId" },
        { new SqlRepoKey(SqlKeys.GetUserById, DbEngine.PostgreSQL, 1),
            "SELECT userid, name, email, createdat FROM users WHERE userid = @UserId" },

        { new SqlRepoKey(SqlKeys.GetUsersForOrganization, DbEngine.MSSQL, 1),
            @"SELECT u.UserId, u.Name, u.Email, u.CreatedAt 
              FROM Users u 
              INNER JOIN OrganizationUser cou ON u.UserId = cou.UserId 
              WHERE cou.OrganizationId = @CustomerOrgId" },
        { new SqlRepoKey(SqlKeys.GetUsersForOrganization, DbEngine.PostgreSQL, 1),
            @"SELECT u.userid, u.name, u.email, u.createdat 
              FROM users u 
              INNER JOIN organizationuser cou ON u.userid = cou.userid 
              WHERE cou.organizationid = @CustomerOrgId" },

        { new SqlRepoKey(SqlKeys.InsertUser, DbEngine.MSSQL, 1),
            "INSERT INTO Users (UserId, Name, Email, CreatedAt) VALUES (@UserId, @Name, @Email, GETDATE())" },
        { new SqlRepoKey(SqlKeys.InsertUser, DbEngine.PostgreSQL, 1),
            "INSERT INTO users (userid, name, email, createdat) VALUES (@UserId, @Name, @Email, NOW())" },

        { new SqlRepoKey(SqlKeys.UpsertUser, DbEngine.MSSQL, 1),
            @"MERGE INTO Users AS target
              USING (SELECT @UserId AS UserId, @Name AS Name, @Email AS Email) AS source
              ON target.UserId = source.UserId
              WHEN MATCHED THEN
                  UPDATE SET Name = source.Name, Email = source.Email
              WHEN NOT MATCHED THEN
                  INSERT (UserId, Name, Email, CreatedAt) 
                  VALUES (source.UserId, source.Name, source.Email, GETDATE());" },
        { new SqlRepoKey(SqlKeys.UpsertUser, DbEngine.PostgreSQL, 1),
            @"INSERT INTO users (userid, name, email, createdat)
              VALUES (@UserId, @Name, @Email, NOW())
              ON CONFLICT (userid) DO UPDATE 
              SET name = excluded.name, email = excluded.email;" },

        { new SqlRepoKey(SqlKeys.PurgeUsers, DbEngine.MSSQL, 1),
            "DELETE FROM Users" },
        { new SqlRepoKey(SqlKeys.PurgeUsers, DbEngine.PostgreSQL, 1),
            "DELETE FROM users" },

        // Organization Queries
        { new SqlRepoKey(SqlKeys.GetParentOrgIdForOrgId, DbEngine.MSSQL, 1),
            "SELECT ParentOrganizationId FROM Organizations WHERE OrganizationId = @OrganizationId" },
        { new SqlRepoKey(SqlKeys.GetParentOrgIdForOrgId, DbEngine.PostgreSQL, 1),
            "SELECT parentorganizationid FROM organizations WHERE organizationid = @OrganizationId" },

        { new SqlRepoKey(SqlKeys.GetOrganizationById, DbEngine.MSSQL, 1),
            "SELECT OrganizationId, Name, ParentOrganizationId, CreatedAt FROM Organizations WHERE OrganizationId = @OrganizationId" },
        { new SqlRepoKey(SqlKeys.GetOrganizationById, DbEngine.PostgreSQL, 1),
            "SELECT organizationid, name, parentorganizationid, createdat FROM organizations WHERE organizationid = @OrganizationId" },

        { new SqlRepoKey(SqlKeys.GetAllOrganizations, DbEngine.MSSQL, 1),
            "SELECT OrganizationId, Name, ParentOrganizationId, CreatedAt FROM Organizations" },
        { new SqlRepoKey(SqlKeys.GetAllOrganizations, DbEngine.PostgreSQL, 1),
            "SELECT organizationid, name, parentorganizationid, createdat FROM organizations" },

        { new SqlRepoKey(SqlKeys.UpsertOrganization, DbEngine.MSSQL, 1),
            @"MERGE INTO Organizations AS target
              USING (SELECT @OrganizationId AS OrganizationId, @Name AS Name, @ParentOrganizationId AS ParentOrganizationId) AS source
              ON target.OrganizationId = source.OrganizationId
              WHEN MATCHED THEN
                  UPDATE SET Name = source.Name, ParentOrganizationId = source.ParentOrganizationId
              WHEN NOT MATCHED THEN
                  INSERT (OrganizationId, Name, ParentOrganizationId, CreatedAt)
                  VALUES (source.OrganizationId, source.Name, source.ParentOrganizationId, GETDATE());" },
        { new SqlRepoKey(SqlKeys.UpsertOrganization, DbEngine.PostgreSQL, 1),
            @"INSERT INTO organizations (organizationid, name, parentorganizationid, createdat)
              VALUES (@OrganizationId, @Name, @ParentOrganizationId, NOW())
              ON CONFLICT (organizationid) DO UPDATE 
              SET name = excluded.name, parentorganizationid = excluded.parentorganizationid;" },

        { new SqlRepoKey(SqlKeys.PurgeOrganizations, DbEngine.MSSQL, 1),
            "DELETE FROM Organizations" },
        { new SqlRepoKey(SqlKeys.PurgeOrganizations, DbEngine.PostgreSQL, 1),
            "DELETE FROM organizations" },

        // Transaction Queries
        { new SqlRepoKey(SqlKeys.GetTransactionById, DbEngine.MSSQL, 1),
            "SELECT TransactionId, UserId, OrganizationId, TransactionType, Amount, Account, Status, CreatedAt, ProcessedAt FROM Transactions WHERE TransactionId = @TransactionId" },
        { new SqlRepoKey(SqlKeys.GetTransactionById, DbEngine.PostgreSQL, 1),
            "SELECT transactionid, userid, organizationid, transactiontype, amount, account, status, createdat, processedat FROM transactions WHERE transactionid = @TransactionId" },

        { new SqlRepoKey(SqlKeys.GetTransactionsForOrganization, DbEngine.MSSQL, 1),
            "SELECT TransactionId, UserId, OrganizationId, TransactionType, Amount, Account, Status, CreatedAt, ProcessedAt FROM Transactions WHERE OrganizationId = @OrganizationId" },
        { new SqlRepoKey(SqlKeys.GetTransactionsForOrganization, DbEngine.PostgreSQL, 1),
            "SELECT transactionid, userid, organizationid, transactiontype, amount, account, status, createdat, processedat FROM transactions WHERE organizationid = @OrganizationId" },

        { new SqlRepoKey(SqlKeys.UpsertTransaction, DbEngine.MSSQL, 1),
            @"MERGE INTO Transactions AS target
              USING (SELECT @TransactionId AS TransactionId, @UserId AS UserId, @OrganizationId AS OrganizationId, 
                     @TransactionType AS TransactionType, @Amount AS Amount, @Account AS Account, @Status AS Status, @CreatedAt AS CreatedAt, @ProcessedAt AS ProcessedAt) AS source
              ON target.TransactionId = source.TransactionId
              WHEN MATCHED THEN
                  UPDATE SET UserId = source.UserId, OrganizationId = source.OrganizationId, TransactionType = source.TransactionType, 
                             Amount = source.Amount, Account = source.Account, Status = source.Status, CreatedAt = source.CreatedAt, ProcessedAt = source.ProcessedAt
              WHEN NOT MATCHED THEN
                  INSERT (TransactionId, UserId, OrganizationId, TransactionType, Amount, Account, Status, CreatedAt, ProcessedAt)
                  VALUES (source.TransactionId, source.UserId, source.OrganizationId, source.TransactionType, source.Amount, source.Account, source.Status, source.CreatedAt, source.ProcessedAt);" },
        { new SqlRepoKey(SqlKeys.UpsertTransaction, DbEngine.PostgreSQL, 1),
            @"INSERT INTO transactions (transactionid, userid, organizationid, transactiontype, amount, account, status, createdat, processedat)
              VALUES (@TransactionId, @UserId, @OrganizationId, @TransactionType, @Amount, @Account, @Status, @CreatedAt, @ProcessedAt)
              ON CONFLICT (transactionid) DO UPDATE 
              SET userid = excluded.userid, organizationid = excluded.organizationid, transactiontype = excluded.transactiontype, 
                  amount = excluded.amount, account = excluded.account, status = excluded.status, createdat = excluded.createdat, processedat = excluded.processedat;" },

        // Admin Report Queries
        { new SqlRepoKey(SqlKeys.GetTransactionSummaryReport, DbEngine.MSSQL, 1),
            "SELECT Status, COUNT(*) AS Count, SUM(Amount) AS TotalAmount FROM Transactions GROUP BY Status" },
        { new SqlRepoKey(SqlKeys.GetTransactionSummaryReport, DbEngine.PostgreSQL, 1),
            "SELECT status, COUNT(*) AS count, SUM(amount) AS totalamount FROM transactions GROUP BY status" },

        { new SqlRepoKey(SqlKeys.GetUserActivityReport, DbEngine.MSSQL, 1),
            @"SELECT u.UserId, u.Name, COUNT(t.TransactionId) AS TransactionCount
              FROM Users u
              LEFT JOIN Transactions t ON u.UserId = t.UserId
              GROUP BY u.UserId, u.Name" },
        { new SqlRepoKey(SqlKeys.GetUserActivityReport, DbEngine.PostgreSQL, 1),
            @"SELECT u.userid, u.name, COUNT(t.transactionid) AS transactioncount
              FROM users u
              LEFT JOIN transactions t ON u.userid = t.userid
              GROUP BY u.userid, u.name" },

        { new SqlRepoKey(SqlKeys.GetOrganizationHierarchyReport, DbEngine.MSSQL, 1),
            @"WITH OrgHierarchy AS (
                  SELECT OrganizationId, Name, ParentOrganizationId, CreatedAt, 0 AS Level
                  FROM Organizations
                  WHERE ParentOrganizationId IS NULL
                  UNION ALL
                  SELECT c.OrganizationId, c.Name, c.ParentOrganizationId, c.CreatedAt, Level + 1
                  FROM Organizations c
                  INNER JOIN OrgHierarchy o ON c.ParentOrganizationId = o.OrganizationId
              )
              SELECT OrganizationId, Name, ParentOrganizationId, CreatedAt, Level FROM OrgHierarchy" },
        { new SqlRepoKey(SqlKeys.GetOrganizationHierarchyReport, DbEngine.PostgreSQL, 1),
            @"WITH RECURSIVE OrgHierarchy AS (
                  SELECT organizationid, name, parentorganizationid, createdat, 0 AS level
                  FROM organizations
                  WHERE parentorganizationid IS NULL
                  UNION ALL
                  SELECT c.organizationid, c.name, c.parentorganizationid, c.createdat, level + 1
                  FROM organizations c
                  INNER JOIN OrgHierarchy o ON c.parentorganizationid = o.organizationid
              )
              SELECT organizationid, name, parentorganizationid, createdat, level FROM OrgHierarchy" },

        // OrganizationUser Queries
        { new SqlRepoKey(SqlKeys.InsertOrganizationUser, DbEngine.MSSQL, 1),
            "INSERT INTO OrganizationUser (OrganizationId, UserId, Role, JoinedAt) VALUES (@OrganizationId, @UserId, @Role, @JoinedAt)" },
        { new SqlRepoKey(SqlKeys.InsertOrganizationUser, DbEngine.PostgreSQL, 1),
            "INSERT INTO organizationuser (organizationid, userid, role, joinedat) VALUES (@OrganizationId, @UserId, @Role, @JoinedAt)" },

        { new SqlRepoKey(SqlKeys.UpsertOrganizationUser, DbEngine.MSSQL, 1),
            @"MERGE INTO OrganizationUser AS target
              USING (SELECT @OrganizationId AS OrganizationId, @UserId AS UserId, @Role AS Role, @JoinedAt AS JoinedAt) AS source
              ON target.OrganizationId = source.OrganizationId AND target.UserId = source.UserId
              WHEN MATCHED THEN
                  UPDATE SET Role = source.Role, JoinedAt = source.JoinedAt
              WHEN NOT MATCHED THEN
                  INSERT (OrganizationId, UserId, Role, JoinedAt) 
                  VALUES (source.OrganizationId, source.UserId, source.Role, source.JoinedAt);" },
        { new SqlRepoKey(SqlKeys.UpsertOrganizationUser, DbEngine.PostgreSQL, 1),
            @"INSERT INTO organizationuser (organizationid, userid, role, joinedat)
              VALUES (@OrganizationId, @UserId, @Role, @JoinedAt)
              ON CONFLICT (organizationid, userid) 
              DO UPDATE SET role = excluded.role, joinedat = excluded.joinedat;" },

        { new SqlRepoKey(SqlKeys.GetOrganizationsForUser, DbEngine.MSSQL, 1),
            "SELECT OrganizationId, UserId, Role, JoinedAt FROM OrganizationUser WHERE UserId = @UserId" },
        { new SqlRepoKey(SqlKeys.GetOrganizationsForUser, DbEngine.PostgreSQL, 1),
            "SELECT organizationid, userid, role, joinedat FROM organizationuser WHERE userid = @UserId" },

        { new SqlRepoKey(SqlKeys.GetAllOrganizationsReport, DbEngine.MSSQL, 1),
            @"SELECT 
                  o.OrganizationId AS OrganizationId,
                  o.Name AS OrganizationName,
                  o.ParentOrganizationId AS ParentOrganizationId,
                  p.Name AS ParentOrganizationName,
                  o.CreatedAt AS CreatedAt
              FROM Organizations o
              LEFT JOIN Organizations p ON o.ParentOrganizationId = p.OrganizationId" },
        { new SqlRepoKey(SqlKeys.GetAllOrganizationsReport, DbEngine.PostgreSQL, 1),
            @"SELECT 
                  o.organizationid AS OrganizationId,
                  o.name AS OrganizationName,
                  o.parentorganizationid AS ParentOrganizationId,
                  p.name AS ParentOrganizationName,
                  o.createdat AS CreatedAt
              FROM organizations o
              LEFT JOIN organizations p ON o.parentorganizationid = p.organizationid" },

        { new SqlRepoKey(SqlKeys.GetTransactionsForOrgWithDateRange, DbEngine.MSSQL, 1),
            @"SELECT TransactionId, UserId, OrganizationId, TransactionType, Account, Amount, Status, CreatedAt, ProcessedAt 
              FROM Transactions 
              WHERE OrganizationId = @OrgId AND CreatedAt >= @Start AND CreatedAt <= @Finish" },
        { new SqlRepoKey(SqlKeys.GetTransactionsForOrgWithDateRange, DbEngine.PostgreSQL, 1),
            @"SELECT transactionid, userid, organizationid, transactiontype, account, amount, status, createdat, processedat 
              FROM transactions 
              WHERE organizationid = @OrgId AND createdat >= @Start AND createdat <= @Finish" },

        { new SqlRepoKey(SqlKeys.GetOrganizationForUser, DbEngine.MSSQL, 1),
            @"SELECT o.OrganizationId, o.Name, o.ParentOrganizationId, o.CreatedAt
              FROM Organizations o
              INNER JOIN OrganizationUser cou ON o.OrganizationId = cou.OrganizationId
              WHERE cou.UserId = @UserId" },
        { new SqlRepoKey(SqlKeys.GetOrganizationForUser, DbEngine.PostgreSQL, 1),
            @"SELECT o.organizationid, o.name, o.parentorganizationid, o.createdat
              FROM organizations o
              INNER JOIN organizationuser cou ON o.organizationid = cou.organizationid
              WHERE cou.userid = @UserId" },

        { new SqlRepoKey(SqlKeys.GetTransactionsForUserWithDateRange, DbEngine.MSSQL, 1),
            @"SELECT TransactionId, UserId, OrganizationId, TransactionType, Account, Amount, Status, CreatedAt, ProcessedAt
              FROM Transactions
              WHERE UserId = @UserId AND CreatedAt >= @Start AND CreatedAt <= @Finish" },
        { new SqlRepoKey(SqlKeys.GetTransactionsForUserWithDateRange, DbEngine.PostgreSQL, 1),
            @"SELECT transactionid, userid, organizationid, transactiontype, account, amount, status, createdat, processedat
              FROM transactions
              WHERE userid = @UserId AND createdat >= @Start AND createdat <= @Finish" },
        { new SqlRepoKey(SqlKeys.GetTableCounts, DbEngine.PostgreSQL, 1),
            @"SELECT
                (SELECT COUNT(*) FROM transactions) AS Transactions,
                (SELECT COUNT(DISTINCT userid) FROM organizationuser WHERE role = 'Admin') AS Admins,
                (SELECT COUNT(DISTINCT userid) FROM organizationuser WHERE role = 'Worker') AS Workers,
                (SELECT COUNT(*) FROM organizations WHERE parentorganizationid IS NOT NULL) AS Organizations,
                (SELECT COUNT(*) FROM organizations WHERE parentorganizationid IS NULL) AS ParentOrganizations;" },
        { new SqlRepoKey(SqlKeys.GetTableCounts, DbEngine.MSSQL, 1),
            @"SELECT
                (SELECT COUNT(*) FROM Transactions) AS Transactions,
                (SELECT COUNT(DISTINCT UserId) FROM OrganizationUser WHERE Role = 'Admin') AS Admins,
                (SELECT COUNT(DISTINCT UserId) FROM OrganizationUser WHERE Role = 'Worker') AS Workers,
                (SELECT COUNT(*) FROM Organizations WHERE ParentOrganizationId IS NOT NULL) AS Organizations,
                (SELECT COUNT(*) FROM Organizations WHERE ParentOrganizationId IS NULL) AS ParentOrganizations;" },
        { new SqlRepoKey(SqlKeys.DeleteAllData, DbEngine.PostgreSQL, 1),
            @"DELETE FROM transactions;
                DELETE FROM organizationuser;
                DELETE FROM users;
                DELETE FROM organizations;" },
        { new SqlRepoKey(SqlKeys.DeleteAllData, DbEngine.MSSQL, 1),
            @"DELETE FROM Transactions;
                DELETE FROM OrganizationUser;
                DELETE FROM Users;
                DELETE FROM Organizations;" }
    };

    /// <summary>
    /// Given a database engine and a version, construct a dictionary of
    /// <see cref="SqlKeys"/> values and their corresponding SQL for the current instance.
    /// SQL does not need to be present for each engine/version combination, but every key
    /// requires an entry at the level of <paramref name="maxVersion"/> or lower.
    /// As a best practice, add any new keys at level 1 and increment versions as needed.
    /// In other words, running version 4 does not mean that you must have a statement for each
    /// key with a version of 4; it rather means that your dictionary of SQL statements will not
    /// have any versions greater than 4.
    /// </summary>
    /// <param name="dbEngine">The database engine. Should map to <see cref="DbEngine"/>.
    /// Originates in the configuration file input and/or environment variables.</param>
    /// <param name="maxVersion">The version to be used in the current instance of the test.
    /// </param>
    /// <returns>A dictionary of keys and values intended for data access layer consumption./></returns>
    /// <exception cref="Exception">Thrown if no SQL could be constructed for one of the keys.</exception>
    public IReadOnlyDictionary<string, string> BuildDictionary(DbEngine dbEngine, int maxVersion)
    {
        var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var engineSubset = _sqlDictionary.Where(d => d.Key.DbEngine == dbEngine)
                                        .GroupBy(d => d.Key.SqlKey)
                                        .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var key in SqlKeys.GetKeys())
        {
            if (!engineSubset.TryGetValue(key, out var matches))
                throw new Exception($"No SQL statement found for key '{key}' and engine '{dbEngine}'.");

            string sql = null;
            for (int v = maxVersion; v > 0; v--)
            {
                sql = matches.FirstOrDefault(m => m.Key.SqlVersion == v).Value;
                if (!string.IsNullOrWhiteSpace(sql))
                {
                    results[key] = sql;
                    break;
                }
            }

            if (sql == null)
                throw new Exception($"No SQL statement found for key '{key}', engine '{dbEngine}', up to version '{maxVersion}'.");
        }

        return results;
    }
}