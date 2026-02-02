using System.Reflection;
using MyTest.attributes;
using MyTest.exceptions;

namespace MyTestLauncher;

public class TestLauncher
{
    private const ConsoleColor SuccessColor = ConsoleColor.Green;
    private const ConsoleColor TestErrorColor = ConsoleColor.Red;
    private const ConsoleColor CriticalErrorColor = ConsoleColor.DarkRed;

    public void LaunchTest(Assembly assembly)
    {
        var assemblyName = assembly.GetName().Name;
        Console.WriteLine($"=== Launch tests from assembly: {assemblyName} ===\n");
        var testClasses = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
            .ToList();

        testClasses.ForEach(RunTestClass);
    }

    private void RunTestClass(Type testClass)
    {
        string className = testClass.Name;
        Console.WriteLine($"--- Class testing: {className} ---");

        object? instance;

        try
        {
            instance = Activator.CreateInstance(testClass);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CRITICAL] Failed to create test class {className}: {ex.Message}");
            return;
        }

        if (instance == null)
        {
            Console.WriteLine($"[CRITICAL] Failed to find test class {className}");
            return;
        }

        // [TODO]: Здесь в будущем вызовем методы с [ClassInitialize]

        var methods = testClass.GetMethods()
            .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null)
            .ToList();

        methods.ForEach(m => RunTestMethod(instance, m));

        // [TODO]: Здесь в будущем вызовем методы с [ClassCleanup]

        // [TODO]: я бы ещё реализовал IDisposable

        Console.WriteLine();
    }

    private void RunTestMethod(object instance, MethodInfo method)
    {
        // [TODO]: Поиск и запуск метода [TestInitialize]
        // Например: FindMethodWithAttribute<TestInitializeAttribute>(instance)?.Invoke(...)

        try
        {
            ExecuteTestMethod(instance, method);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR]: {method.Name}: {ex.Message}");
        }
        finally
        {
            // [TODO]: Поиск и запуск метода [TestCleanup]
            // Например: FindMethodWithAttribute<TestCleanupAttribute>(instance)?.Invoke(...)
        }
    }

    private void ExecuteTestMethod(object instance, MethodInfo method)
    {
        // [TODO]: Проверка на наличие атрибутов [DataRow]
        // var dataRows = method.GetCustomAttributes<DataRowAttribute>();
        // ЕСЛИ DataRow есть:
        //   Цикл foreach по строкам данных -> вызов InvokeCore(instance, method, rowArgs)

        Invoke(instance, method, null);
    }

    private void Invoke(object instance, MethodInfo method, object[]? args)
    {
        Console.Write($"{method.Name}: ");

        try
        {
            object? result = method.Invoke(instance, args);
            if (result is Task task)
            {
                task.GetAwaiter().GetResult();
            }

            PrintSuccess();
        }
        catch (TargetInvocationException ex)
        {
            HandleTestException(ex.InnerException ?? ex);
        }
        catch (Exception ex)
        {
            HandleTestException(ex);
        }
    }

    private void PrintSuccess()
    {
        var prevColor = Console.ForegroundColor;
        Console.ForegroundColor = SuccessColor;
        Console.WriteLine("PASSED");
        Console.ForegroundColor = prevColor;
    }

    private void HandleTestException(Exception ex)
    {
        var prevColor = Console.ForegroundColor;

        if (ex is TestFailedException)
        {
            Console.ForegroundColor = TestErrorColor;
            Console.Write($"FAILED. {ex.Message}");
        }
        else
        {
            Console.ForegroundColor = CriticalErrorColor;
            Console.WriteLine($"CRASHED. Unexpected error: {ex.GetType().Name} - {ex.Message}");
        }

        Console.ForegroundColor = prevColor;
    }
}