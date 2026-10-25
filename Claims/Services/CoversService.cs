using Claims.Auditing;
using Claims.Premium;
using Claims.Repositories;

namespace Claims.Services;

public class CoversService : ICoversService
{
    private readonly ICoversRepository _coversRepository;
    private readonly IAuditer _auditer;
    private readonly IPremiumCalculator _premiumCalculator;

    public CoversService(ICoversRepository coversRepository, IAuditer auditer, IPremiumCalculator premiumCalculator)
    {
        _coversRepository = coversRepository;
        _auditer = auditer;
        _premiumCalculator = premiumCalculator;
    }

    public async Task<IEnumerable<Cover>> GetAllAsync()
    {
        return await _coversRepository.GetAllAsync();
    }

    public async Task<Cover?> GetByIdAsync(string id)
    {
        return await _coversRepository.GetByIdAsync(id);
    }

    public async Task<Cover> CreateAsync(Cover cover)
    {
        cover.Id = Guid.NewGuid().ToString();
        cover.Premium = _premiumCalculator.Calculate(cover.StartDate, cover.EndDate, cover.Type);
        await _coversRepository.AddAsync(cover);
        _auditer.AuditCover(cover.Id, "POST");
        return cover;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var deleted = await _coversRepository.DeleteAsync(id);
        if (deleted)
        {
            _auditer.AuditCover(id, "DELETE");
        }
        return deleted;
    }

    public decimal ComputePremium(DateTime startDate, DateTime endDate, CoverType coverType)
    {
        return _premiumCalculator.Calculate(startDate, endDate, coverType);
    }
}