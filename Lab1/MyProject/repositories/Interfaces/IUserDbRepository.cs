using MyProject.dtos;
using MyProject.entities;

namespace MyProject.repositories.Interfaces;

public interface IUserDbRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<List<User>> GetAllAsync();
    Task<User> CreateAsync(UserDto userDto);
    Task<User> UpdateAsync(User user);
    Task<bool> DeleteAsync(int id);
}