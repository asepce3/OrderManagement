using OrderManagement.Models;

namespace OrderManagement.Repositories;

public interface IUserRepository
{
    Task<User?> GetUserByEmailAsync(string email);
    Task SaveUserAsync(User user);
}
