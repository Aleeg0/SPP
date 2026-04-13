namespace MyTest.attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CategoryAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}