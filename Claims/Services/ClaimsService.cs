using Claims.Auditing;
using Claims.Repositories;

namespace Claims.Services;

public class ClaimsService : IClaimsService
{
    private readonly IClaimsRepository _claimsRepository;
    private readonly IAuditer _auditer;

    public ClaimsService(IClaimsRepository claimsRepository, IAuditer auditer)
    {
        _claimsRepository = claimsRepository;
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
}