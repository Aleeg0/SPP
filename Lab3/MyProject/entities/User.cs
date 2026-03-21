namespace MyProject.entities;

public class User(int id, string login, string password)
{
    public int Id {get; set;} = id;
    public string Login {get; set;} = login;
    public string Password {get; set;} = password;

    public override bool Equals(object? obj)
    {
        if (obj is User other)
            return Id == other.Id && Login == other.Login && Password == other.Password;
        return false;
    }
}