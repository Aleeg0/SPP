namespace MyProject.entities;

public class Author(int id, string login, string password, string firstname, string lastname)
{
    public int Id {get; set;} = id;
    public string Login {get; set;} = login;
    public string Password {get; set;} = password;
    public string Firstname {get; set;} = firstname;
    public string Lastname {get; set;} = lastname;
}