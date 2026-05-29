using ChatApp.Api.Models;

namespace ChatApp.Api.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUsernameAsync(string username);

        Task<IReadOnlyList<User>> GetAllUsersAsync(Guid excludeUserId);
    }
}
