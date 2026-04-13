using Microsoft.EntityFrameworkCore;
using MyProject.configs;
using MyProject.dtos;
using MyProject.entities;
using MyProject.repositories.Interfaces;

namespace MyProject.repositories.Implementations;

public class UserDbRepository : IUserDbRepository
{
    private readonly ApplicationDbContext _context;

    public UserDbRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<List<User>> GetAllAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task<User> CreateAsync(UserDto userDto)
    {
        var newUser = new User(0, userDto.Login, userDto.Password);

        await _context.Users.AddAsync(newUser);
        await _context.SaveChangesAsync();

        return newUser;
    }

    public async Task<User> UpdateAsync(User user)
    {
        var exists = await _context.Users.AnyAsync(u => u.Id == user.Id);
        if (!exists)
        {
            throw new KeyNotFoundException($"User with ID {user.Id} was not found.");
        }

        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }
}