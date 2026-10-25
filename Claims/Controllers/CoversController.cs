using Claims.Auditing;
using Claims.Premium;
using Claims.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Controllers;

[ApiController]
[Route("[controller]")]
public class CoversController : ControllerBase
{
    private readonly ICoversRepository _coversRepository;
    private readonly ILogger<CoversController> _logger;
    private readonly Auditer _auditer;
    private readonly IPremiumCalculator _premiumCalculator;

    public CoversController(ICoversRepository coversRepository, IPremiumCalculator premiumCalculator, AuditContext auditContext, ILogger<CoversController> logger)
    {
        _coversRepository = coversRepository;
        _premiumCalculator = premiumCalculator;
        _logger = logger;
        _auditer = new Auditer(auditContext);
    }

    [HttpPost("compute")]
    public ActionResult ComputePremium(DateTime startDate, DateTime endDate, CoverType coverType)
    {
        return Ok(_premiumCalculator.Calculate(startDate, endDate, coverType));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Cover>>> GetAsync()
    {
        var results = await _coversRepository.GetAllAsync();
        return Ok(results);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Cover?>> GetAsync(string id)
    {
        var cover = await _coversRepository.GetByIdAsync(id);
        return Ok(cover);
    }

    [HttpPost]
    public async Task<ActionResult> CreateAsync(Cover cover)
    {
        cover.Id = Guid.NewGuid().ToString();
        cover.Premium = _premiumCalculator.Calculate(cover.StartDate, cover.EndDate, cover.Type);
        await _coversRepository.AddAsync(cover);
        _auditer.AuditCover(cover.Id, "POST");
        return Ok(cover);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAsync(string id)
    {
        _auditer.AuditCover(id, "DELETE");
        await _coversRepository.DeleteAsync(id);

        return NoContent();
    }
}
