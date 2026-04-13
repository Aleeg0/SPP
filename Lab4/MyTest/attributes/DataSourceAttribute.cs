namespace MyTest.attributes;

[AttributeUsage(AttributeTargets.Method)]
public class DataSourceAttribute(string methodName) : Attribute
{
    public string MethodName { get; } = methodName;
}