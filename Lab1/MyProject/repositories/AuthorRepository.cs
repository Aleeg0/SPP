using MyProject.entities;

namespace MyProject.repository;

public class AuthorRepository
{
    public List<Author> GetAll()
    {
        return [new Author(1, "Aleego", "asgfasg", "Alexander", "Egorov")];
    }
}