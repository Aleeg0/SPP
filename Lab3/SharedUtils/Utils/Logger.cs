namespace SharedUtils.Utils;

public class Logger : ILogger
{
    private readonly Lock _consoleLock = new();

    public ConsoleColor SuccessColor { get; init; } = ConsoleColor.Green;
    public ConsoleColor TestErrorColor { get; init; } = ConsoleColor.Red;
    public ConsoleColor CriticalErrorColor { get; init; } = ConsoleColor.DarkRed;
    public ConsoleColor SkippedColor { get; init; } = ConsoleColor.Cyan;

    public void Print(string message, ConsoleColor? color = null)
    {
        lock (_consoleLock)
        {
            if (color.HasValue)
            {
                var prev = Console.ForegroundColor;
                Console.ForegroundColor = color.Value;
                Console.WriteLine(message);
                Console.ForegroundColor = prev;
            }
            else
            {
                Console.WriteLine(message);
            }
        }
    }

    public void PrintSuccess(string testInfo)
    {
        lock (_consoleLock)
        {
            var prevColor = Console.ForegroundColor;
            Console.Write($"{testInfo}: ");
            Console.ForegroundColor = SuccessColor;
            Console.WriteLine("PASSED");
            Console.ForegroundColor = prevColor;
        }
    }

    public void PrintSkipped(string methodName, string message)
    {
        lock (_consoleLock)
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = SkippedColor;
            Console.Write($"{methodName}: SKIPPED. ");
            Console.ForegroundColor = prevColor;
            Console.WriteLine(message);
        }
    }

    public void PrintFailed(string testInfo, string message)
    {
        lock (_consoleLock)
        {
            var prevColor = Console.ForegroundColor;
            Console.Write($"{testInfo}: ");
            Console.ForegroundColor = TestErrorColor;
            Console.WriteLine($"FAILED. {message}");
            Console.ForegroundColor = prevColor;
        }
    }

    public void PrintCrashed(string testInfo, Exception ex)
    {
        lock (_consoleLock)
        {
            var prevColor = Console.ForegroundColor;
            Console.Write($"{testInfo}: ");
            Console.ForegroundColor = CriticalErrorColor;
            Console.WriteLine($"CRASHED. Unexpected error: {ex.GetType().Name} - {ex.Message}");
            Console.ForegroundColor = prevColor;
        }
    }
}