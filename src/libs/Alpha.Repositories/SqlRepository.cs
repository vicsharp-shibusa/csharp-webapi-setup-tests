//using TestControl.AppServices;

//namespace Alpha.Repositories;

//public static class SqlRepository
//{
//    // User Queries
//    public static string GetUserIdForEmail(DbEngine dbEngine)
//    {
//        return dbEngine == DbEngine.MSSQL
//            ? "SELECT UserId FROM Users WHERE Email = @Email"
//            : "SELECT userid FROM users WHERE email = @Email";
//    }

//    public static string GetUserByEmail(DbEngine dbEngine)
//    {
//        return dbEngine == DbEngine.MSSQL
//            ? "SELECT UserId, Name, Email, CreatedAt FROM Users WHERE Email = @Email"
//            : "SELECT userid, name, email, createdat FROM users WHERE email = @Email";
//    }

//    public static string GetUserById(DbEngine dbEngine)
//    {
//        return dbEngine == DbEngine.MSSQL
//            ? "SELECT UserId, Name, Email, CreatedAt FROM Users WHERE UserId = @UserId"
//            : "SELECT userid, name, email, createdat FROM users WHERE userid = @UserId";
//    }

//    public static string GetUsersForOrganization(DbEngine dbEngine)
//    {
//        return dbEngine == DbEngine.MSSQL
//            ? @"SELECT u.UserId, u.Name, u.Email, u.CreatedAt 
//                FROM Users u 
//                INNER JOIN OrganizationUser cou ON u.UserId = cou.UserId 
//                WHERE cou.OrganizationId = @CustomerOrgId"
//            : @"SELECT u.userid, u.name, u.email, u.createdat 
//                FROM users u 
//                INNER JOIN organizationuser cou ON u.userid = cou.userid 
//                WHERE cou.organizationid = @CustomerOrgId";
//    }

//    public static string InsertUser(DbEngine dbEngine)
//    {
//        return dbEngine == DbEngine.MSSQL
//            ? "INSERT INTO Users (UserId, Name, Email, CreatedAt) VALUES (@UserId, @Name, @Email, GETDATE())"
//            : "INSERT INTO users (userid, name, email, createdat) VALUES (@UserId, @Name, @Email, NOW())";
//    }

//    public static string UpsertUser(DbEngine dbEngine)
//    {
//        if (dbEngine == DbEngine.MSSQL)
//        {
//            return @"MERGE INTO Users AS target
//                     USING (SELECT @UserId AS UserId, @Name AS Name, @Email AS Email) AS source
//                     ON target.UserId = source.UserId
//                     WHEN MATCHED THEN
//                         UPDATE SET Name = source.Name, Email = source.Email
//                     WHEN NOT MATCHED THEN
//                         INSERT (UserId, Name, Email, CreatedAt) 
//                         VALUES (source.UserId, source.Name, source.Email, GETDATE());";
//        }
//        return @"INSERT INTO users (userid, name, email, createdat)
//                 VALUES (@UserId, @Name, @Email, NOW())
//                 ON CONFLICT (userid) DO UPDATE 
//                 SET name = excluded.name, email = excluded.email;";
//    }

//    public static string PurgeUsers(DbEngine dbEngine)
//    {
//        return dbEngine == DbEngine.MSSQL
//            ? "DELETE FROM Users"
//            : "DELETE FROM users";
//    }

//    // Organization Queries
//    public static string GetParentOrgIdForOrgId(DbEngine dbEngine)
//    {
//        return dbEngine == DbEngine.MSSQL
//            ? "SELECT ParentOrganizationId FROM Organizations WHERE OrganizationId = @OrganizationId"
//            : "SELECT parentorganizationid FROM organizations WHERE organizationid = @OrganizationId";
//    }

//    public static string GetOrganizationById(DbEngine dbEngine)
//    {
//        return dbEngine == DbEngine.MSSQL
//            ? "SELECT OrganizationId, Name, ParentOrganizationId, CreatedAt FROM Organizations WHERE OrganizationId = @OrganizationId"
//            : "SELECT organizationid, name, parentorganizationid, createdat FROM organizations WHERE organizationid = @OrganizationId";
//    }

//    public static string GetAllOrganizations(DbEngine dbEngine)
//    {
//        return dbEngine == DbEngine.MSSQL
//            ? "SELECT OrganizationId, Name, ParentOrganizationId, CreatedAt FROM Organizations"
//            : "SELECT organizationid, name, parentorganizationid, createdat FROM organizations";
//    }

//    public static string UpsertOrganization(DbEngine dbEngine)
//    {
//        if (dbEngine == DbEngine.MSSQL)
//        {
//            return @"MERGE INTO Organizations AS target
//                     USING (SELECT @OrganizationId AS OrganizationId, @Name AS Name, @ParentOrganizationId AS ParentOrganizationId) AS source
//                     ON target.OrganizationId = source.OrganizationId
//                     WHEN MATCHED THEN
//                         UPDATE SET Name = source.Name, ParentOrganizationId = source.ParentOrganizationId
//                     WHEN NOT MATCHED THEN
//                         INSERT (OrganizationId, Name, ParentOrganizationId, CreatedAt)
//                         VALUES (source.OrganizationId, source.Name, source.ParentOrganizationId, GETDATE());";
//        }
//        return @"INSERT INTO organizations (organizationid, name, parentorganizationid, createdat)
//                 VALUES (@OrganizationId, @Name, @ParentOrganizationId, NOW())
//                 ON CONFLICT (organizationid) DO UPDATE 
//                 SET name = excluded.name, parentorganizationid = excluded.parentorganizationid;";
//    }

//    public static string PurgeOrganizations(DbEngine dbEngine)
//    {
//        return dbEngine == DbEngine.MSSQL
//            ? "DELETE FROM Organizations"
//            : "DELETE FROM organizations";
//    }

//    // Transaction Queries
//    public static string GetTransactionById(DbEngine dbEngine)
//    {
//        return dbEngine == DbEngine.MSSQL
//            ? "SELECT TransactionId, UserId, OrganizationId, TransactionType, Amount, Account, Status, CreatedAt, ProcessedAt FROM Transactions WHERE TransactionId = @TransactionId"
//            : "SELECT transactionid, userid, organizationid, transactiontype, amount, account, status, createdat, processedat FROM transactions WHERE transactionid = @TransactionId";
//    }

//    public static string GetTransactionsForOrganization(DbEngine dbEngine)
//    {
//        return dbEngine == DbEngine.MSSQL
//            ? "SELECT TransactionId, UserId, OrganizationId, TransactionType, Amount, Account, Status, CreatedAt, ProcessedAt FROM Transactions WHERE OrganizationId = @OrganizationId"
//            : "SELECT transactionid, userid, organizationid, transactiontype, amount, account, status, createdat, processedat FROM transactions WHERE organizationid = @OrganizationId";
//    }

//    public static string UpsertTransaction(DbEngine dbEngine)
//    {
//        if (dbEngine == DbEngine.MSSQL)
//        {
//            return @"MERGE INTO Transactions AS target
//                     USING (SELECT @TransactionId AS TransactionId, @UserId AS UserId, @OrganizationId AS OrganizationId, 
//                            @TransactionType AS TransactionType, @Amount AS Amount, @Account AS Account, @Status AS Status, @CreatedAt AS CreatedAt, @ProcessedAt AS ProcessedAt) AS source
//                     ON target.TransactionId = source.TransactionId
//                     WHEN MATCHED THEN
//                         UPDATE SET UserId = source.UserId, OrganizationId = source.OrganizationId, TransactionType = source.TransactionType, 
//                                    Amount = source.Amount, Account = source.Account, Status = source.Status, CreatedAt = source.CreatedAt, ProcessedAt = source.ProcessedAt
//                     WHEN NOT MATCHED THEN
//                         INSERT (TransactionId, UserId, OrganizationId, TransactionType, Amount, Account, Status, CreatedAt, ProcessedAt)
//                         VALUES (source.TransactionId, source.UserId, source.OrganizationId, source.TransactionType, source.Amount, source.Account, source.Status, source.CreatedAt, source.ProcessedAt);";
//        }
//        return @"INSERT INTO transactions (transactionid, userid, organizationid, transactiontype, amount, account, status, createdat, processedat)
//                 VALUES (@TransactionId, @UserId, @OrganizationId, @TransactionType, @Amount, @Account, @Status, @CreatedAt, @ProcessedAt)
//                 ON CONFLICT (transactionid) DO UPDATE 
//                 SET userid = excluded.userid, organizationid = excluded.organizationid, transactiontype = excluded.transactiontype, 
//                     amount = excluded.amount, account = excluded.account, status = excluded.status, createdat = excluded.createdat, processedat = excluded.processedat;";
//    }

//    // Admin Report Queries
//    public static string GetTransactionSummaryReport(DbEngine dbEngine)
//    {
//        return dbEngine == DbEngine.MSSQL
//            ? "SELECT Status, COUNT(*) AS Count, SUM(Amount) AS TotalAmount FROM Transactions GROUP BY Status"
//            : "SELECT status, COUNT(*) AS count, SUM(amount) AS totalamount FROM transactions GROUP BY status";
//    }

//    public static string GetUserActivityReport(DbEngine dbEngine)
//    {
//        return dbEngine == DbEngine.MSSQL
//            ? @"SELECT u.UserId, u.Name, COUNT(t.TransactionId) AS TransactionCount
//                FROM Users u
//                LEFT JOIN Transactions t ON u.UserId = t.UserId
//                GROUP BY u.UserId, u.Name"
//            : @"SELECT u.userid, u.name, COUNT(t.transactionid) AS transactioncount
//                FROM users u
//                LEFT JOIN transactions t ON u.userid = t.userid
//                GROUP BY u.userid, u.name";
//    }

//    public static string GetOrganizationHierarchyReport(DbEngine dbEngine)
//    {
//        if (dbEngine == DbEngine.MSSQL)
//        {
//            return @"WITH OrgHierarchy AS (
//                        SELECT OrganizationId, Name, ParentOrganizationId, CreatedAt, 0 AS Level
//                        FROM Organizations
//                        WHERE ParentOrganizationId IS NULL
//                        UNION ALL
//                        SELECT c.OrganizationId, c.Name, c.ParentOrganizationId, c.CreatedAt, Level + 1
//                        FROM Organizations c
//                        INNER JOIN OrgHierarchy o ON c.ParentOrganizationId = o.OrganizationId
//                    )
//                    SELECT OrganizationId, Name, ParentOrganizationId, CreatedAt, Level FROM OrgHierarchy";
//        }
//        return @"WITH RECURSIVE OrgHierarchy AS (
//                    SELECT organizationid, name, parentorganizationid, createdat, 0 AS level
//                    FROM organizations
//                    WHERE parentorganizationid IS NULL
//                    UNION ALL
//                    SELECT c.organizationid, c.name, c.parentorganizationid, c.createdat, level + 1
//                    FROM organizations c
//                    INNER JOIN OrgHierarchy o ON c.parentorganizationid = o.organizationid
//                )
//                SELECT organizationid, name, parentorganizationid, createdat, level FROM OrgHierarchy";
//    }

//    // OrganizationUser Queries
//    public static string InsertOrganizationUser(DbEngine dbEngine)
//    {
//        return dbEngine == DbEngine.MSSQL
//            ? "INSERT INTO OrganizationUser (OrganizationId, UserId, Role, JoinedAt) VALUES (@OrganizationId, @UserId, @Role, @JoinedAt)"
//            : "INSERT INTO organizationuser (organizationid, userid, role, joinedat) VALUES (@OrganizationId, @UserId, @Role, @JoinedAt)";
//    }

//    public static string UpsertOrganizationUser(DbEngine dbEngine)
//    {
//        if (dbEngine == DbEngine.MSSQL)
//        {
//            return @"MERGE INTO OrganizationUser AS target
//                     USING (SELECT @OrganizationId AS OrganizationId, @UserId AS UserId, @Role AS Role, @JoinedAt AS JoinedAt) AS source
//                     ON target.OrganizationId = source.OrganizationId AND target.UserId = source.UserId
//                     WHEN MATCHED THEN
//                         UPDATE SET Role = source.Role, JoinedAt = source.JoinedAt
//                     WHEN NOT MATCHED THEN
//                         INSERT (OrganizationId, UserId, Role, JoinedAt) 
//                         VALUES (source.OrganizationId, source.UserId, source.Role, source.JoinedAt);";
//        }
//        return @"INSERT INTO organizationuser (organizationid, userid, role, joinedat)
//                 VALUES (@OrganizationId, @UserId, @Role, @JoinedAt)
//                 ON CONFLICT (organizationid, userid) 
//                 DO UPDATE SET role = excluded.role, joinedat = excluded.joinedat;";
//    }

//    public static string GetOrganizationsForUser(DbEngine dbEngine)
//    {
//        return dbEngine == DbEngine.MSSQL
//            ? "SELECT OrganizationId, UserId, Role, JoinedAt FROM OrganizationUser WHERE UserId = @UserId"
//            : "SELECT organizationid, userid, role, joinedat FROM organizationuser WHERE userid = @UserId";
//    }

//    public static string GetAllOrganizationsReport(DbEngine dbEngine)
//    {
//        return dbEngine == DbEngine.MSSQL
//            ? @"SELECT 
//                o.OrganizationId AS OrganizationId,
//                o.Name AS OrganizationName,
//                o.ParentOrganizationId AS ParentOrganizationId,
//                p.Name AS ParentOrganizationName,
//                o.CreatedAt AS CreatedAt
//            FROM Organizations o
//            LEFT JOIN Organizations p ON o.ParentOrganizationId = p.OrganizationId"
//            : @"SELECT 
//                o.organizationid AS OrganizationId,
//                o.name AS OrganizationName,
//                o.parentorganizationid AS ParentOrganizationId,
//                p.name AS ParentOrganizationName,
//                o.createdat AS CreatedAt
//            FROM organizations o
//            LEFT JOIN organizations p ON o.parentorganizationid = p.organizationid";
//    }

//    public static string GetTransactionsForOrganizationWithDateRange(DbEngine dbEngine) => dbEngine == DbEngine.MSSQL
//        ? @"SELECT TransactionId, UserId, OrganizationId, TransactionType, Account, Amount, Status, CreatedAt, ProcessedAt 
//            FROM Transactions 
//            WHERE OrganizationId = @OrgId AND CreatedAt >= @Start AND CreatedAt <= @Finish"
//        : @"SELECT transactionid, userid, organizationid, transactiontype, account, amount, status, createdat, processedat 
//            FROM transactions 
//            WHERE organizationid = @OrgId AND createdat >= @Start AND createdat <= @Finish";

//    public static string GetOrganizationForUser(DbEngine dbEngine) => dbEngine == DbEngine.MSSQL
//        ? @"SELECT o.OrganizationId, o.Name, o.ParentOrganizationId, o.CreatedAt
//            FROM Organizations o
//            INNER JOIN OrganizationUser cou ON o.OrganizationId = cou.OrganizationId
//            WHERE cou.UserId = @UserId"
//        : @"SELECT o.organizationid, o.name, o.parentorganizationid, o.createdat
//            FROM organizations o
//            INNER JOIN organizationuser cou ON o.organizationid = cou.organizationid
//            WHERE cou.userid = @UserId";

//    public static string GetTransactionsForUserWithDateRange(DbEngine dbEngine) => dbEngine == DbEngine.MSSQL
//        ? @"SELECT TransactionId, UserId, OrganizationId, TransactionType, Account, Amount, Status, CreatedAt, ProcessedAt
//            FROM Transactions
//            WHERE UserId = @UserId AND CreatedAt >= @Start AND CreatedAt <= @Finish"
//        : @"SELECT transactionid, userid, organizationid, transactiontype, account, amount, status, createdat, processedat
//            FROM transactions
//            WHERE userid = @UserId AND createdat >= @Start AND createdat <= @Finish";
//}