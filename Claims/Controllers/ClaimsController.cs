using Claims.Auditing;
using Claims.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Controllers;

[ApiController]
[Route("[controller]")]
public class ClaimsController : ControllerBase
{
    private readonly ILogger<ClaimsController> _logger;
    private readonly IClaimsRepository _claimsRepository;
    private readonly IAuditer _auditer;

    public ClaimsController(ILogger<ClaimsController> logger, IClaimsRepository claimsRepository, IAuditer auditer)
    {
        _logger = logger;
        _auditer = auditer;
        _claimsRepository = claimsRepository;
    }

    [HttpGet]
    public async Task<IEnumerable<Claim>> GetAsync()
    {
        return await _claimsRepository.GetAllAsync();
    }

    [HttpPost]
    public async Task<ActionResult> CreateAsync(Claim claim)
    {
        claim.Id = Guid.NewGuid().ToString();
        await _claimsRepository.AddAsync(claim);
        _auditer.AuditClaim(claim.Id, "POST");
        return Ok(claim);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAsync(string id)
    {
        _auditer.AuditClaim(id, "DELETE");
        await _claimsRepository.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<Claim?> GetAsync(string id)
    {
        return await _claimsRepository.GetByIdAsync(id);
    }
}
