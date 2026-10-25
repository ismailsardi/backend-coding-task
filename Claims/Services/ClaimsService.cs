using Claims.Auditing;
using Claims.Repositories;
using Claims.Validation;

namespace Claims.Services;

public class ClaimsService : IClaimsService
{
    private const Decimal MaxDamageCost = 100_000m;
    private readonly IClaimsRepository _claimsRepository;
    private readonly ICoversRepository _coversRepository;
    private readonly IAuditer _auditer;

    public ClaimsService(IClaimsRepository claimsRepository, ICoversRepository coversRepository, IAuditer auditer)
    {
        _claimsRepository = claimsRepository;
        _coversRepository = coversRepository;
        _auditer = auditer;
    }

    public async Task<IEnumerable<Claim>> GetAllAsync()
    {
        return await _claimsRepository.GetAllAsync();
    }

    public async Task<Claim?> GetByIdAsync(string id)
    {
        return await _claimsRepository.GetByIdAsync(id);
    }

    public async Task<Claim> CreateAsync(Claim claim)
    {
        await ValidateAsync(claim);
        claim.Id = Guid.NewGuid().ToString();
        await _claimsRepository.AddAsync(claim);
        _auditer.AuditClaim(claim.Id, "POST");
        return claim;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var deleted = await _claimsRepository.DeleteAsync(id);
        if (deleted)
        {
            _auditer.AuditClaim(id, "DELETE");
        }
        return deleted;
    }
    
 private async Task ValidateAsync(Claim claim)
{
    if (claim.DamageCost > MaxDamageCost)
    {
        throw new ValidationException("Damage cost cannot exceed 100,000.");
    }

    var cover = await _coversRepository.GetByIdAsync(claim.CoverId);
    if (cover is null)
    {
        throw new ValidationException("Cover not found.");
    }

    if (claim.Created < cover.StartDate || claim.Created > cover.EndDate)
    {
        throw new ValidationException("Created date must be within the cover period.");
    }
}
}