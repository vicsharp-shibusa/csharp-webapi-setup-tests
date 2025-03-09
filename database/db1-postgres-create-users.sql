CREATE USER api_command_user WITH PASSWORD '18Holesin1';
CREATE USER api_query_user WITH PASSWORD '18Holesin1';

GRANT CONNECT ON DATABASE api_performance_tests TO api_command_user;
GRANT USAGE ON SCHEMA public TO api_command_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO api_command_user;
GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA public TO api_command_user;

-- Grant read-only access to the query user
GRANT CONNECT ON DATABASE api_performance_tests TO api_query_user;
GRANT USAGE ON SCHEMA public TO api_query_user;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO api_query_user;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO api_query_user;

ALTER DEFAULT PRIVILEGES IN SCHEMA public
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO api_command_user;

ALTER DEFAULT PRIVILEGES IN SCHEMA public
GRANT SELECT ON TABLES TO api_query_user;
