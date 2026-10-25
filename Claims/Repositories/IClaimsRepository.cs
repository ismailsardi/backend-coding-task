namespace Claims.Repositories;

public interface IClaimsRepository
{
    Task<IEnumerable<Claim>> GetAllAsync();
    Task<Claim?> GetByIdAsync(string id);
    Task AddAsync(Claim claim);
    Task<bool> DeleteAsync(string id);
}