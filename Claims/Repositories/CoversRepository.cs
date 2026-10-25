using Claims.Data;
using Microsoft.EntityFrameworkCore;

namespace Claims.Repositories;

public class CoversRepository : ICoversRepository
{
    private readonly ClaimsContext _claimsContext;

    public CoversRepository(ClaimsContext claimsContext)
    {
        _claimsContext = claimsContext;
    }

    public async Task<IEnumerable<Cover>> GetAllAsync()
    {
        return await _claimsContext.Covers.ToListAsync();
    }

    public async Task<Cover?> GetByIdAsync(string id)
    {
        return await _claimsContext.Covers
            .Where(cover => cover.Id == id)
            .SingleOrDefaultAsync();
    }

    public async Task AddAsync(Cover cover)
    {
        _claimsContext.Covers.Add(cover);
        await _claimsContext.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var cover = await GetByIdAsync(id);
        if (cover is null)
        {
            return false;
        }

        _claimsContext.Covers.Remove(cover);
        await _claimsContext.SaveChangesAsync();
        return true;
    }
}
