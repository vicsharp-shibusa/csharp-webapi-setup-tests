# C# WebApi Setup Tests

## Current Status 2025-03-10

**Please Note: this file is a work in progress, as is this project.**

The first test architecture `Test.Alpha` and the control program, `TestControl.Cli`, are stable, but not complete.
This first iteration focuses on configuration, control, logging, and graceful shutdown.
Next step is the inner workings of the `Worker` class; then we'll have a legit test methinks.

## Quick Start

1. Get your database sorted. See the `database/` directory in this repo.
2. Set up your connection strings for your target api to talk to your database.
3. Set up your environment variables.
4. Run the target API.
5. Created/edit your configuration file to align with your api. See `src/apps/TestControl.Cli/configs`.
6. Run the CLI control program.

### Database

The `database/` directory contains scripts for both PostgreSQL and MS Sql Server. Each database has at least 3 scripts:

1. Create the tables.
2. Create the indexes.
3. Create the users.

The names of the files and their purposes are self-explanatory I hope.
I would run the files in the order above.
I separated them like I did in case I wanted to run tests without the table indexes.

### Connection Strings

I think you can handle the connection strings for your chosen database, but there are two (2) of them: one for commands and one for queries. They use different users (see the appropriate file in the `database/` directory). You are welcome to make them the same connection string.

The `TestControl.Cli` will read a `secrets.json` file, and this is where I recommend you put your connection strings.
Mine looks like this:

```json
{
  "ConnectionStrings": {
    "db1PostgreSQLCommand": "User ID=api_command_user;Password=18Holesin1;Host=127.0.0.1;Port=5432;Database=api_performance_tests;",
    "db1PostgreSQLQuery": "User ID=api_query_user;Password=18Holesin1;Host=127.0.0.1;Port=5432;Database=api_performance_tests;"
  }
}
```
The key for each connection string is important.
It consists of three (3) parts: 1) a "db version," 2) a "db engine," and 3) whether it's intended for commands or queries.
If you're using MS SQL Server, your keys would be `db1MSSQLCommand` and `db1MSSQLQuery`.

The `db1` is part of database versioning, but that's a future topic.

### Environment Variables

The test subject api requires two environment variables: `DB_ENGINE` and `DB_VERSION`.
These values correspond to the connection string as you might expect.
if the valus provided to the control program via the configuration file do not align with the test api, the app will throw an exception.

### Configuration File

Take a look at `src/apps/TestControl.Cli/configs` for examples of configuration files.

The following snippet shows the configuration elements that must align with your test api.

```json
{
  "apiBaseUrl": "https://localhost:5260",
  "apiName": "Test.Alpha",
  "databaseEngine": "PostgreSQL",
  "databaseVersion": "db1"
}
```

The `failureHandling` section addresses the constraints for response time handling.
The interpretation of the json below is: _if the average response time of the last `100` calls to the test api exceeds `999` milliseconds, stop the test._

```json
{
  "failureHandling": {
    "periodAverageResponseTime": 100,
    "averageResponseTimeThresholdMs": 999
  }
}
```

I'll break down the rest of the configuration later; the above should get you started.

---

## General Notes

This project is a simple, hand-rolled performance and scability tester for web apis.
The name of the project has "csharp" in it, but any technology can be employed for a new test subject api, as long as the test subject provides all necessary endpoints.

The `TestControl.Cli` is the control mechanism.
It consumes a configuration file that aligns with the `TestConfig` class.
This configuration drives the target, the scope, and pace of the test.
You can find sample configuration files in the `src/apps/TestControl.Cli/configs` directory.

The web apis being tested are located in the `src/hosts` directory.
See notes on the specific tests below.

The test subject apis in this repo do not use any security; there is no authorization component.
This is by design; I didn't not want to deal with my different worker objects having to address keeping their tokens current.
A real security component would definitely change the outcomes of the test, but as long as none of the test subjects have this constraint, I believe the test remains fair.

Each api has a scoped `OperationContext` because it makes sense that _something_ is scoped to the HTTP request.

### Testing Methodology

The test begins by creating `Admin`, 'Organization`, and `Worker` objects at a predictable pace.
On a schedule (see the configuration file), these objects are created until the limit is reached.
This limit (the max number of `Admin` instances) is the _first milestone_.

Each `Admin` and `Worker` instance has at least one internal timer on which they perform some action.
For `Admin` objects, the `Admin` runs queries against his organizations and users.
For `Worker` objects, they create and update `Transactions` as well as run queries.
After the _first milestone_ is achieved, the cycle time for these timers begins to shrink.

There is no expected scenario in which any web api architecture can survive the test.
The point is to finish the test with failure.
There are four ways the test can end:

1. The user hits CTRL-C.
2. The response time threshold is reached.
3. An individual `Admin` or `Worker` cycle does not complete within its allotted time.
4. An `Exception` is thrown.

The test attempts to avoid rapid-fire calls to the API until its required as a result of interval compression.
Up to the _first milestone_, the pace is fairly steady, but the number of `Admin` and `Worker` objects (and their workloads) are increasing according to the values in the configuration file.


#### Response Time Threshold

The response time threshold is defined by the `failureHandling` section of the config:

```json
"failureHandling": {
  "periodAverageResponseTime": 100,
  "averageResponseTimeThresholdMs": 500
}
```

The `averageResponseTimeThresholdMs` is the average of the most recent `periodAverageResponseTime` number of response times.
For example, in the example above, the average response time of the last `100` responses cannot exceed `500` milliseconds.

---

## List of Endpoints in the Test Subject API

- [HttpGet("api/user/{userId}")]
- [HttpGet("api/user/by-email/{email}")]
- [HttpGet("api/users/by-org/{customerOrgId}")]
- [HttpPost("api/user")]
- [HttpPost("api/test/initialize")]
- [HttpPost("api/test/warmup")]
- [HttpGet("api/status")]
- [HttpGet("api/name")]
- [HttpGet("api/org/{customerOrganizationId}")]
- [HttpPost("api/org")]

## Best Practices

I recommend restarting the api between tests, otherwise the memory consumption stats may be skewed.