using MyProject.dtos;
using MyProject.entities;

namespace MyProject.repositories.Interfaces;

public interface IUserInMemoryRepository
{
    public User? GetById(int id);
    public List<User> GetAll();
    public User Create(UserDto user);
    public User Update(User user);
    public bool Delete(int id);
}