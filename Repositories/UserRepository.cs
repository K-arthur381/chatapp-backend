using ChatApp.Api.Interfaces;
using ChatApp.Api.Models;
using ChatApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Api.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted);
    }

    public async Task<IReadOnlyList<User>> GetAllUsersAsync(Guid excludeUserId)
    {
        return await _dbSet
            .Where(u => u.UserId != excludeUserId && !u.IsDeleted)
            .AsNoTracking()
            .ToListAsync();
    }
}