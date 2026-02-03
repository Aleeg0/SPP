using MyProject.entities;
using MyTest;
using MyTest.attributes;

namespace MyTestProject.repositories;

[TestClass]
public class AuthorRepositoryTest
{
    [TestMethod(Description = "Get Author")]
    public void TestGetAll()
    {
        User user = new User(1, "Aleego", "123");
        User author2 = new User(1, "Aleego", "123");
        Assert.AreEqual(user,author2);
    }

    [TestMethod(Description = "Get Author")]
    [DataRow(1,"Aleego", "123")]
    public void TestGetById(User user, User user2)
    {
        Assert.AreEqual(user, user2);
    }
}