namespace MyTest.attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class DataRowAttribute(params object[] values) : Attribute
{
    public object[] Values { get; init; } = values;
}