namespace Claims.Services;

public interface ICoversService
{
    Task<IEnumerable<Cover>> GetAllAsync();
    Task<Cover?> GetByIdAsync(string id);
    Task<Cover> CreateAsync(Cover cover);
    Task<bool> DeleteAsync(string id);
    decimal ComputePremium(DateTime startDate, DateTime endDate, CoverType coverType);
}