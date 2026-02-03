using MyProject.dtos;
using MyProject.entities;
using MyProject.repositories.Interfaces;

namespace MyProject.repositories.Implementations;

public class UserInMemoryRepository : IUserRepository
{
    private readonly Dictionary<int, User> _users = new();

    private int _nextId = 0;

    public User? GetById(int id)
    {
        return _users.GetValueOrDefault(id);
    }

    public List<User> GetAll()
    {
        return _users.Values.ToList();
    }

    public User Create(UserDto userDto)
    {
        var newUser = new User(_nextId++, userDto.Login, userDto.Password);
        _users[newUser.Id] = newUser;
        return newUser;
    }

    public User Update(User user)
    {
        if (!_users.ContainsKey(user.Id))
        {
            throw new KeyNotFoundException($"User with ID {user.Id} was not found.");
        }
        _users[user.Id] = user;
        return user;
    }

    public bool Delete(int id)
    {
        return _users.Remove(id);
    }
}