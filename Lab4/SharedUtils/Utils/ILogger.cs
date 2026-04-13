namespace SharedUtils.Utils;

public interface ILogger
{
    void Print(string message, ConsoleColor? color = null);
    void PrintSuccess(string testInfo);
    void PrintSkipped(string methodName, string message);
    void PrintFailed(string methodName, string message);
    void PrintCrashed(string methodName, Exception ex);
}