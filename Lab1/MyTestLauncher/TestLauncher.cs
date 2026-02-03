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

        var classInitializeMethod = testClass.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .SingleOrDefault(m => m.GetCustomAttribute<ClassInitializeAttribute>() != null);

        var testInitializeMethod = testClass.GetMethods()
            .SingleOrDefault(m => m.GetCustomAttribute<TestInitializeAttribute>() != null);

        var testCleanupMethod = testClass.GetMethods()
            .SingleOrDefault(m => m.GetCustomAttribute<TestCleanupAttribute>() != null);

        classInitializeMethod?.Invoke(null, null);

        var methods = testClass.GetMethods()
            .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null)
            .ToList();

        methods.ForEach(m => RunTestMethod(instance, m, testInitializeMethod, testCleanupMethod));

        var classCleanupMethod = testClass.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .SingleOrDefault(m => m.GetCustomAttribute<ClassCleanupAttribute>() != null);

        classCleanupMethod?.Invoke(null, null);

        Console.WriteLine();
    }

    private void RunTestMethod(object instance, MethodInfo method, MethodInfo? init, MethodInfo? cleanup)
    {
        init?.Invoke(instance, null);
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
            cleanup?.Invoke(instance, null);
        }
    }

    private void ExecuteTestMethod(object instance, MethodInfo method)
    {
        var dataRows = method.GetCustomAttributes<DataRowAttribute>().ToList();

        if (dataRows.Count != 0)
        {
            dataRows.ForEach(dataRow => Invoke(instance, method, dataRow.Values));
        }
        else
        {
            Invoke(instance, method, null);
        }
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