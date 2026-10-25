using Claims.Auditing;
using Claims.Premium;
using Claims.Repositories;
using Claims.Validation;

namespace Claims.Services;

public class CoversService : ICoversService
{
    private readonly ICoversRepository _coversRepository;
    private readonly IAuditer _auditer;
    private readonly IPremiumCalculator _premiumCalculator;
    private readonly TimeProvider _timeProvider;

    public CoversService(ICoversRepository coversRepository, IAuditer auditer, IPremiumCalculator premiumCalculator, TimeProvider timeProvider)
    {
        _coversRepository = coversRepository;
        _auditer = auditer;
        _premiumCalculator = premiumCalculator;
        _timeProvider = timeProvider;
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
        Validate(cover);
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

    private void Validate(Cover cover)
    {
        var today = _timeProvider.GetUtcNow().Date;

        if (cover.StartDate.Date < today)
        {
            throw new ValidationException("Start date cannot be in the past.");
        }

        if (cover.EndDate <= cover.StartDate)
        {
            throw new ValidationException("End date must be after start date.");
        }

        if (cover.EndDate > cover.StartDate.AddYears(1))
        {
            throw new ValidationException("Insurance period cannot exceed 1 year.");
        }
    }
}