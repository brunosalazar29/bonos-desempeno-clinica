using Microsoft.AspNetCore.Mvc;
using BonusTest.Application.Bonuses;

namespace BonusTest.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class BonusController(BonusService bonusService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] BonusFilterRequest request,
        CancellationToken cancellationToken) =>
        Ok(await bonusService.GetPagedAsync(request, cancellationToken));

    [HttpGet("employee/{departmentCode}/{employeeNumber:int}")]
    public async Task<IActionResult> GetEmployee(
        string departmentCode,
        int employeeNumber,
        CancellationToken cancellationToken) =>
        Ok(await bonusService.FindEmployeeAsync(departmentCode, employeeNumber, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateBonusRequest request,
        [FromHeader(Name = "UserCode")] string? userCode,
        CancellationToken cancellationToken)
    {
        var bonusId = await bonusService.CreateAsync(request, userCode, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { bonusId });
    }

    [HttpPut("{bonusId:long}/cancel")]
    public async Task<IActionResult> Cancel(
        long bonusId,
        [FromHeader(Name = "UserCode")] string? userCode,
        CancellationToken cancellationToken)
    {
        await bonusService.CancelAsync(bonusId, userCode, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{bonusId:long}")]
    public async Task<IActionResult> Delete(
        long bonusId,
        [FromHeader(Name = "UserCode")] string? userCode,
        CancellationToken cancellationToken)
    {
        await bonusService.DeleteAsync(bonusId, userCode, cancellationToken);
        return NoContent();
    }
}
