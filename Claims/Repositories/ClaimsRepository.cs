using Claims.Data;
using Microsoft.EntityFrameworkCore;

namespace Claims.Repositories;

public class ClaimsRepository : IClaimsRepository
{
    private readonly ClaimsContext _claimsContext;

    public ClaimsRepository(ClaimsContext claimsContext)
    {
        _claimsContext = claimsContext;
    }

    public async Task<IEnumerable<Claim>> GetAllAsync()
    {
        return await _claimsContext.Claims.ToListAsync();
    }

    public async Task<Claim?> GetByIdAsync(string id)
    {
        return await _claimsContext.Claims
            .Where(claim => claim.Id == id)
            .SingleOrDefaultAsync();
    }

    public async Task AddAsync(Claim claim)
    {
        _claimsContext.Claims.Add(claim);
        await _claimsContext.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var claim = await GetByIdAsync(id);
        if (claim is null)
        {
            return false;
        }

        _claimsContext.Claims.Remove(claim);
        await _claimsContext.SaveChangesAsync();
        return true;
    }
}
