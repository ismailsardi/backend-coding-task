using Claims.Services;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Controllers;

[ApiController]
[Route("[controller]")]
public class ClaimsController : ControllerBase
{
    private readonly IClaimsService _claimsService;

    public ClaimsController(IClaimsService claimsService)
    {
        _claimsService = claimsService;
    }

    [HttpGet]
    public async Task<IEnumerable<Claim>> GetAsync()
    {
        return await _claimsService.GetAllAsync();
    }

    [HttpPost]
    public async Task<ActionResult> CreateAsync(Claim claim)
    {
        var created = await _claimsService.CreateAsync(claim);
        return Created($"/Claims/{created.Id}", created);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAsync(string id)
    {
        var deleted = await _claimsService.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Claim>> GetAsync(string id)
    {
        var claim = await _claimsService.GetByIdAsync(id);
        if (claim is null)
        {
            return NotFound();
        }

        return Ok(claim);
    }
}
