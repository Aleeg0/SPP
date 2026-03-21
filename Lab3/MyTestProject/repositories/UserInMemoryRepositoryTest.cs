using MyProject.dtos;
using MyProject.entities;
using MyProject.repositories.Implementations;
using MyTest;
using MyTest.attributes;

namespace MyTestProject.repositories;

[TestClass]
public class UserInMemoryRepositoryTests
{
    private UserInMemoryInMemoryRepository _repository = null!;
    private UserDto _defaultDto = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _repository = new UserInMemoryInMemoryRepository();
        _defaultDto = new UserDto("testUser", "12345678");
    }

    [TestMethod(Description = "Simple Description")]
    public void Create_ValidDto_ReturnsUserWithNewId()
    {
        var createdUser = _repository.Create(_defaultDto);

        Assert.IsNotNull(createdUser);
        Assert.AreEqual(_defaultDto.Login, createdUser.Login);

        var allUsers = _repository.GetAll();
        Assert.AreEqual(1, allUsers.Count);
    }

    [TestMethod]
    [DataRow("testUser1", "secretPassword")]
    [DataRow("admin", "verySecretPassword", IgnoreMessage = "Skipping this DataRow")]
    [DataRow("admin2", "verySecretPassword2")]
    public void Create_VariousInputs_PopulatesFieldsCorrectly(string login, string password)
    {
        var dto = new UserDto(login, password);
        var user = _repository.Create(dto);

        Assert.AreEqual(login, user.Login);
        Assert.AreEqual(password, user.Password);
    }

    [TestMethod]
    public async Task GetById_ExistingId_ReturnsUser()
    {
        var user = _repository.Create(_defaultDto);
        var retrievedUser = _repository.GetById(user.Id);

        Assert.AreEqual(user, retrievedUser);
        await Task.Delay(5000);
    }

    [TestMethod]
    public void GetById_NonExistingId_ReturnsNull()
    {
        var result = _repository.GetById(999);
        Assert.IsNull(result);
        throw new Exception("This shouldn't happen");
    }

    [TestMethod]
    public void Update_ExistingUser_UpdatesLogin()
    {
        var user = _repository.Create(_defaultDto);
        user.Login = "new_login";

        var updatedUser = _repository.Update(user);

        Assert.AreEqual("new_login", updatedUser.Login);
        Assert.AreEqual("new_login", _repository.GetById(user.Id).Login);
    }

    [TestMethod]
    [Timeout(1000)]
    public async Task Update_NonExistingUser_ThrowsKeyNotFoundException()
    {
        await Task.Delay(5000);
        var fakeUser = new User(999, "fake", "fake");
        Assert.Throws<KeyNotFoundException>(() => _repository.Update(fakeUser));
    }

    [TestMethod]
    public void Delete_ExistingId_ReturnsTrueAndRemovesUser()
    {
        var user = _repository.Create(_defaultDto);

        var result = _repository.Delete(user.Id);

        Assert.IsTrue(result);
        Assert.DoesNotContains(user, _repository.GetAll());
    }

    [TestMethod]
    public void Delete_NonExistingId_ReturnsFalse()
    {
        var user = _repository.Create(_defaultDto);
        var notExistedUserId = user.Id + 1;
        var result = _repository.Delete(notExistedUserId);

        Assert.IsFalse(result);
        Assert.Contains(user, _repository.GetAll());
    }

    [TestMethod]
    [DataRow("user1", "password1", "user2", "password2")]
    public async Task GetAll_UsersExist_ReturnsAllUsers(string firstLogin, string firstPassword, string secondLogin, string secondPassword)
    {
        _repository.Create(new UserDto(firstLogin, firstPassword));
        _repository.Create(new UserDto(secondLogin, secondPassword));

        var all = _repository.GetAll();

        Assert.AreEqual(2, all.Count);
        Assert.Contains(all, u => u.Login == firstLogin);
        Assert.Contains(all, u => u.Password == secondPassword);

        await Task.Delay(5000);
    }
}