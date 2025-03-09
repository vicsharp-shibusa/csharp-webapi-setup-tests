using Alpha.Common;
using Microsoft.AspNetCore.Mvc;
using TestControl.AppServices;
using TestControl.Infrastructure.SubjectApiPublic;

namespace Test.Alpha.Controllers;

[ApiController]
public class UserController : ControllerBase
{
    public static HashSet<Guid> AdminIds = new();
    private readonly IUserService _userService;
    private readonly TestMetricsService _testMetricsService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService,
        TestMetricsService testMetricsService,
        ILogger<UserController> logger)
    {
        _userService = userService;
        _testMetricsService = testMetricsService;
        _logger = logger;
    }

    [HttpGet("api/user/{userId}")]
    public async Task<ActionResult<User>> GetById(Guid userId)
    {
        var user = await _userService.GetByIdAsync(userId);
        return user is not null ? Ok(user) : NotFound();
    }

    [HttpGet("api/user/by-email/{email}")]
    public async Task<ActionResult<User>> GetByEmail([FromRoute] string email)
    {
        var user = await _userService.GetByEmailAsync(email);
        return user is not null ? Ok(user) : NotFound();
    }

    [HttpGet("api/users/by-org/{customerOrgId}")]
    public async Task<ActionResult<IEnumerable<User>>> GetForOrganization(Guid customerOrgId)
    {
        var users = await _userService.GetForOrganizationAsync(customerOrgId);
        return Ok(users);
    }

    [HttpPost("api/user")]
    [HttpPut("api/user")]
    public async Task<IActionResult> Upsert(User user)
    {
        string httpMethod = HttpContext.Request.Method;
        if (httpMethod == "POST" && user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) &&
            !AdminIds.Contains(user.UserId))
        {
            _testMetricsService.IncrementAdmins();
            AdminIds.Add(user.UserId);
        }
        if (httpMethod == "POST" && user.Role.Equals("Worker", StringComparison.OrdinalIgnoreCase))
        {
            _testMetricsService.IncrementWorkers();
        }
        await _userService.UpsertAsync(user);
        return NoContent();
    }
}
