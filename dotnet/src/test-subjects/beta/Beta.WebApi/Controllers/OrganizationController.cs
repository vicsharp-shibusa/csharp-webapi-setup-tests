using Beta.Common;
using Microsoft.AspNetCore.Mvc;
using TestControl.AppServices;
using TestControl.Infrastructure.SubjectApiPublic;

namespace Test.Beta.Controllers;

[ApiController]
public class OrganizationController : ControllerBase
{
    private readonly IOrganizationService _organizationService;
    private readonly IReportService _reportService;
    private readonly ILogger<OrganizationController> _logger;
    private readonly TestMetricsService _testMetricsService;

    public OrganizationController(
        TestMetricsService testMetricsService,
        IOrganizationService organizationService,
        IReportService reportService,
        ILogger<OrganizationController> logger)
    {
        _testMetricsService = testMetricsService;
        _organizationService = organizationService;
        _reportService = reportService;
        _logger = logger;
    }

    [HttpGet("api/org/{organizationId}")]
    public async Task<ActionResult<Organization>> GetById(Guid organizationId)
    {
        var organization = await _organizationService.GetByIdAsync(organizationId);
        return organization is not null ? Ok(organization) : NotFound();
    }

    [HttpPost("api/org")]
    public async Task<IActionResult> Upsert(Organization organization)
    {
        await _organizationService.UpsertAsync(organization);
        return NoContent();
    }

    [HttpGet("api/orgs")]
    public Task<IEnumerable<Organization>> GetAllOrgs()
    {
        return _organizationService.GetAllAsync();
    }
}
