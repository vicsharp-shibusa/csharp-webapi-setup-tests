# C# WebApi Setup Tests

## Current Status 2025-03-19

- This is the most stable version yet.
The `Test.Alpha` architecture holds up pretty well, except that PostgreSQL can't handle all the concurrent connections I'm throwing at it.

- In "Brute Force" mode, the test eventually fails because the **database can't handle the volume of concurrent connections**.
I **pulled Dapper** out and rolled my own set of IDbConnection extensions.
All the follows is related to chasing that rabbit.

- **Made DbProperties disposable.**
In `Test.Alpha`, the repositories (the data access components) and DbProperties are `Scoped`, therefore the db connections they hold are closed and disposed of at the end of the HttpRequest cycle.

- Increased use of cancellation tokens.

- The current version still **doesn't exit gracefully in all circumstances** - going to tackle that next.

## Quick Start

1. Get your database sorted. See the "Database" section below and the `database/` directory in this repo.
2. Set up your connection strings for your target api to talk to your database (see below).
3. Set up your environment variables (see below).
4. Launch the target API.
5. Created/edit your configuration file to align with your api. See `src/apps/TestControl.Cli/configs`.
6. Execute the CLI control program.

### Command

Here's the content of the `launchSettings.json` in my IDE.
`-v` for verbose output, `--save-logs` for writing to the output directory specified by `-l`, and `-c` for intake of the configuration file.

```json
{
  "profiles": {
    "TestControl.Cli": {
      "commandName": "Project",
      "commandLineArgs": "-v -c ./configs/default-config.json -l /webapi-testing --save-logs"
    }
  }
}
```

### Database

The `database/` directory contains scripts for both PostgreSQL and MS Sql Server. Each database has at least 3 scripts:

1. Create the tables.
2. Create the indexes.
3. Create the users.

The names of the files and their purposes are self-explanatory I hope.
I would run the files in the order above.
I separated them like I did in case I wanted to run tests without the table indexes.

### Connection Strings

I think you can handle the connection strings for your chosen database, but there are two (2) of them: one for commands and one for queries. They use different users (see the appropriate file in the `database/` directory).
You are welcome to make them the same connection string; performance will likely degrade if you do.

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
These values correspond to the connection strings as you might expect.
if the values provided to the control program via the configuration file do not align with the test api, the app will throw an exception.

### Configuration File

Take a look at `src/apps/TestControl.Cli/configs` for examples of configuration files.

The following snippet shows the configuration elements that must align with your test api.

```json
  "api": {
    "apiBaseUrl": "https://localhost:5260",
    "apiName": "Test.Alpha",
    "databaseEngine": "PostgreSQL",
    "databaseVersion": "db1"
  }
```

The `responseThreshold` section addresses the constraints for response time handling.
The interpretation of the json below is: _if the average response time of the last `100` calls to the test api exceeds `999` milliseconds, stop the test._

```json
  "responseThreshold": {
    "averageResponseTimePeriod": 100,
    "averageResponseTimeThresholdMs": 999
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
This is by design; I did not want to deal with my different worker objects having to keep their tokens current.
A real security component would definitely change the outcomes of the test, but as long as none of the test subjects have this constraint, I believe the test remains fair.

Each api has a scoped `OperationContext` because it makes sense that _something_ is scoped to the HTTP request in each of the subject test apis.

### Testing Methodology

The test begins by creating `Admin`, `Organization`, and `Worker` objects at a predictable pace.
On a schedule (see the configuration file), these objects are created until the limit is reached.
This limit (the max number of `Admin` instances) is the _first milestone_.

Each `Admin` and `Worker` instance has at least one internal timer on which they perform some action.
For `Admin` objects, the `Admin` runs queries against his organizations and users.
For `Worker` objects, they create and update `Transactions`.
After the _first milestone_ is achieved, both the cycle time (frequency) and the allotted time for the work begins to shrink.

There is no expected scenario in which any web api architecture can survive the test.
The point is to finish the test with failure.
There are five ways the test can end:

1. The user hits CTRL-C.
2. The `testDurationMinutes` time limit is reached (note that a value of `0` here will cause the test to run indefinitely).
3. The HTTP response time threshold is reached (calls to the api take longer than defined by the configuration).
4. An individual `Admin` or `Worker` cycle does not complete within its allotted time.
5. An `Exception` is thrown. The application has zero tolerance for exceptions.

The test attempts to avoid rapid-fire calls to the API until its required as a result of interval compression.
Up to the _first milestone_, the pace is steady, but the number of `Admin` and `Worker` objects (and their workloads) are increasing according to the values in the configuration file.

#### Response Time Threshold

The response time threshold is defined by the `responseThreshold` section of the config:

```json
  "responseThreshold": {
    "averageResponseTimePeriod": 100,
    "averageResponseTimeThresholdMs": 999
  }
```

The `averageResponseTimeThresholdMs` is the average of the most recent `averageResponseTimePeriod` number of response times.
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