using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderManagement.Data;
using OrderManagement.Exceptions;
using OrderManagement.Models;

namespace OrderManagement.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(o => o.Email == email)
            .FirstOrDefaultAsync();
    }

    public async Task SaveUserAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }
}
