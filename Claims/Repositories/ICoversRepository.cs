namespace Claims.Repositories;

public interface ICoversRepository
{
    Task<IEnumerable<Cover>> GetAllAsync();
    Task<Cover?> GetByIdAsync(string id);
    Task AddAsync(Cover cover);
    Task<bool> DeleteAsync(string id);
}