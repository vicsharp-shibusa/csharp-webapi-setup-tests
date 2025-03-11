-- Create logins (equivalent to PostgreSQL users)
CREATE LOGIN api_command_user WITH PASSWORD = '18Holesin1';
CREATE LOGIN api_query_user WITH PASSWORD = '18Holesin1';

-- Create database users linked to the logins
USE api_performance_tests;
GO

CREATE USER api_command_user FOR LOGIN api_command_user;
CREATE USER api_query_user FOR LOGIN api_query_user;
GO

-- Grant permissions to api_command_user (read/write)
ALTER ROLE db_datareader ADD MEMBER api_command_user;
ALTER ROLE db_datawriter ADD MEMBER api_command_user;
GRANT EXECUTE TO api_command_user; -- If stored procedures are used
GO

-- Grant permissions to api_query_user (read-only)
ALTER ROLE db_datareader ADD MEMBER api_query_user;
GO

-- Set default privileges for future tables
USE api_performance_tests;
GO

ALTER ROLE db_datareader ADD MEMBER api_command_user;
ALTER ROLE db_datawriter ADD MEMBER api_command_user;
ALTER ROLE db_datareader ADD MEMBER api_query_user;
GO