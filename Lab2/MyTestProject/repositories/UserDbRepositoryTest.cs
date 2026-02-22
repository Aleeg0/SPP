using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyProject.configs;
using MyProject.dtos;
using MyProject.repositories.Implementations;
using MyTest;
using MyTest.attributes;

namespace MyTestProject.repositories;

[TestClass]
public class UserDbRepositoryTests
{
    private static ApplicationDbContext _context = null!;
    private static UserDbRepository _repository = null!;

    private int _sharedUserId;
    private string _userLogin = "user1";

    [ClassInitialize]
    public static void ClassInitialize()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var connectionString = configuration.GetConnectionString("TestDatabase");

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString);

        _context = new ApplicationDbContext(optionsBuilder.Options);
        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();
        _repository = new UserDbRepository(_context);

        Console.WriteLine("[Setup]: Database created.");
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        // Для результата
        // _context.Database.EnsureDeleted();
        _context.Dispose();
        Console.WriteLine("[Cleanup]: Database deleted.");
    }

    [TestMethod]
    public async Task Step1_CreateUser_ShouldAddToDatabase()
    {
        var dto = new UserDto(_userLogin, "12345");

        var user = await _repository.CreateAsync(dto);

        Assert.IsTrue(user.Id > 0);
        _sharedUserId = user.Id;

        var count = await _context.Users.CountAsync();
        Assert.AreEqual(1, count);
    }

    [TestMethod]
    public async Task Step2_ReadUser_ShouldSeeUserFromStep1()
    {
        var user = await _repository.GetByIdAsync(_sharedUserId);

        Assert.IsNotNull(user);
        Assert.AreEqual(_userLogin, user!.Login);
    }

    [TestMethod]
    public async Task Step3_UpdateUser_ShouldModifyDataFromStep1()
    {
        var user = await _repository.GetByIdAsync(_sharedUserId);
        Assert.IsNotNull(user);

        _userLogin = "updatedLogin";
        user.Login = _userLogin;
        await _repository.UpdateAsync(user);

        var updatedUser = await _repository.GetByIdAsync(_sharedUserId);
        Assert.AreEqual("updatedLogin", updatedUser!.Login);
    }

    [TestMethod]
    [DataRow("user1", "pass1")]
    public async Task Step4_CreateAnother_ShouldIncrementCount(string login, string password)
    {
        var newUser = await _repository.CreateAsync(new UserDto(login, password));

        _sharedUserId = newUser.Id;
        var all = await _repository.GetAllAsync();

        Assert.AreEqual(2, all.Count);
    }

    [TestMethod]
    public async Task Step5_DeleteUser_ShouldRemoveSecondUser()
    {
        var result = await _repository.DeleteAsync(_sharedUserId);
        Assert.IsTrue(result);

        var all = await _repository.GetAllAsync();
        Assert.AreEqual(1, all.Count);
        Assert.AreEqual(_userLogin, all[0].Login);
    }
}