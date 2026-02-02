namespace MyTest.attributes;

[AttributeUsage(AttributeTargets.Method)]
public class TestMethodAttribute : Attribute
{
    public string? Description { get; set; } = null;
    public int Priority { get; set; } = 1;
}