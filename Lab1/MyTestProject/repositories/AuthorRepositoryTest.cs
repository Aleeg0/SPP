using MyTest;
using MyTest.attributes;

namespace MyTestProject.repositories;

[TestClass]
public class AuthorRepositoryTest
{
    [TestMethod(Description = "Get Author")]
    public void TestGetAll()
    {
        Assert.AreEqual(1,1);
    }

    [TestMethod(Description = "Get Author")]
    public void TestGetById()
    {
        Assert.AreEqual(1,2);
    }
}