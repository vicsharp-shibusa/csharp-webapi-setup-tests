using Alpha.Common;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TestControl.AppServices;
using TestControl.Infrastructure.SubjectApiPublic;

namespace Test.Alpha.Controllers;

[ApiController]
public class UserTransactionController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IReportService _reportService;
    private readonly TestMetricsService _testMetricsService;

    public UserTransactionController(IUserService userService,
        IReportService reportService,
        TestMetricsService testMetricsService)
    {
        _userService = userService;
        _reportService = reportService;
        _testMetricsService = testMetricsService;
    }

    [HttpPost("/api/transaction")]
    [HttpPut("/api/transaction")]
    public async Task<IActionResult> UpsertAsync(UserTransaction transaction)
    {
        if (HttpContext.Request.Method == "PUT")
        {
            transaction.ProcessedAt ??= DateTime.Now;
        }

        await _userService.UpsertTransactionAsync(transaction);
        return Accepted();
    }

    [HttpGet("/api/organization/{organizationId}/transactions")]
    public async Task<IActionResult> GetForOrg(
        [FromRoute]Guid organizationId,
        [FromQuery] string status = null,
        [FromQuery] string startTime = null,
        [FromQuery] string finishTime = null)
    {
        (DateTime? start, DateTime? finish) = GetTimeBoundaries(startTime, finishTime);

        return Ok(await _reportService.GetTransactionsForOrgAsync(organizationId, start, finish, status));
    }

    [HttpGet("/api/user/{userId}/transactions")]
    public async Task<IActionResult> GetForUser(Guid userId,
        [FromQuery] string status = null,
        [FromQuery] string startTime = null,
        [FromQuery] string finishTime = null)
    {
        (DateTime? start, DateTime? finish) = GetTimeBoundaries(startTime, finishTime);

        return Ok(await _reportService.GetTransactionsForUserAsync(userId, start, finish, status));
    }

    private static (DateTime? Start, DateTime? Finish) GetTimeBoundaries(string startTime, string finishTime)
    {
        DateTime? start = null, finish = null;

        if (!string.IsNullOrWhiteSpace(startTime))
        {
            if (DateTime.TryParse(startTime, out var dt))
            {
                start = dt.ToLocalTime();
            }
        }

        if (!string.IsNullOrWhiteSpace(finishTime))
        {
            if (DateTime.TryParse(finishTime, out var dt))
            {
                finish = dt.ToLocalTime();
            }
        }

        if (finish.HasValue && !start.HasValue)
        {
            start = finish.Value.AddHours(-1);
        }

        if (start.HasValue && !finish.HasValue)
        {
            finish = new DateTime(Math.Min(DateTime.Now.Ticks, start.Value.Ticks));
        }

        Debug.Assert((finish.HasValue && start.HasValue) || (!finish.HasValue && !start.HasValue));

        if (finish < start)
        {
            (start, finish) = (finish, start);
        }

        return (start, finish);
    }
}
