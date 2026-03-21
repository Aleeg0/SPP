namespace MyTest.attributes;

[AttributeUsage(AttributeTargets.Method)]
public class TimeoutAttribute(int milliseconds) : Attribute
{
    public int Milliseconds { get; } = milliseconds;
}