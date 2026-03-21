using System.Reflection;
using MyTest.attributes;
using MyTest.exceptions;
using MyTestLauncher.Utils;

namespace MyTestLauncher;

public class TestLauncher
{
    private readonly Logger _logger = new ();
    private readonly SemaphoreSlim _semaphore;

    public TestLauncher(int maxDegreeOfParallelism = 4)
    {
        _semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
    }

    public async Task LaunchTestAsync(Assembly assembly)
    {
        var assemblyName = assembly.GetName().Name;
        _logger.Print($"=== Launch tests from assembly: {assemblyName} ===\n");
        var testClasses = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
            .ToList();

        foreach (var testClass in testClasses)
        {
            await RunTestClassAsync(testClass);
        }
    }

    private async Task RunTestClassAsync(Type testClass)
    {
        string className = testClass.Name;
        _logger.Print($"--- Class testing: {className} ---");

        var classInitializeMethod = testClass.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .SingleOrDefault(m => m.GetCustomAttribute<ClassInitializeAttribute>() != null);

        var testInitializeMethod = testClass.GetMethods()
            .SingleOrDefault(m => m.GetCustomAttribute<TestInitializeAttribute>() != null);

        var testCleanupMethod = testClass.GetMethods()
            .SingleOrDefault(m => m.GetCustomAttribute<TestCleanupAttribute>() != null);

        try
        {
            classInitializeMethod?.Invoke(null, null);
        }
        catch (Exception ex)
        {
            _logger.Print($"[CRITICAL] Failed to create test class {className}: {ex.Message}");
            return;
        }

        var methods = testClass.GetMethods()
            .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null)
            .ToList();

        bool isNonParallelizable = testClass.GetCustomAttribute<NonParallelizableAttribute>() != null;

        if (isNonParallelizable)
        {
            var instance = CreateInstance(testClass);

            if (instance == null)
            {
                _logger.Print($"[CRITICAL] Failed to find test class {className}");
                return;
            }

            foreach (var method in methods)
            {
                await RunTestMethodAsync(instance, method, testInitializeMethod, testCleanupMethod);
            }
        }
        else
        {
            var tasks = methods.Select(m =>
                RunMethodWithSemaphoreAsync(testClass, m, testInitializeMethod, testCleanupMethod)
            ).ToList();

            await Task.WhenAll(tasks);
        }

        var classCleanupMethod = testClass.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .SingleOrDefault(m => m.GetCustomAttribute<ClassCleanupAttribute>() != null);

        classCleanupMethod?.Invoke(null, null);

        _logger.Print("");
    }

    private async Task RunMethodWithSemaphoreAsync(Type testClass, MethodInfo method, MethodInfo? init, MethodInfo? cleanup)
    {
        await _semaphore.WaitAsync();
        try
        {
            var instance = CreateInstance(testClass);

            if (instance == null)
            {
                _logger.Print($"[CRITICAL] Failed to find test class {testClass}");
                return;
            }

            await Task.Run(() => RunTestMethodAsync(instance, method, init, cleanup));
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task RunTestMethodAsync(object instance, MethodInfo method, MethodInfo? init, MethodInfo? cleanup)
    {
        try
        {
            init?.Invoke(instance, null);
        }
        catch (Exception ex)
        {
            _logger.Print($"[ERROR] Init failed for {method.Name}: {ex.Message}", ConsoleColor.DarkRed);
            return;
        }

        try
        {
            await ExecuteTestMethod(instance, method);
        }
        catch (Exception ex)
        {
            _logger.Print($"[ERROR]: {method.Name}: {ex.Message}");
        }
        finally
        {
            try
            {
                cleanup?.Invoke(instance, null);
            }
            catch (Exception ex)
            {
                _logger.Print($"[ERROR] Cleanup failed for {method.Name}: {ex.Message}", ConsoleColor.DarkRed);
            }
        }
    }

    private async Task ExecuteTestMethod(object instance, MethodInfo method)
    {
        var dataRows = method.GetCustomAttributes<DataRowAttribute>().ToList();

        if (dataRows.Count != 0)
        {
            foreach (var dataRow in dataRows)
            {
                if (dataRow.IgnoreMessage != null)
                {
                    _logger.PrintSkipped(method.Name, dataRow.IgnoreMessage);
                }
                else
                {
                    await InvokeTestAsync(instance, method, dataRow.Values);
                }
            }
        }
        else
        {
            await InvokeTestAsync(instance, method, null);
        }
    }

    private async Task InvokeTestAsync(object instance, MethodInfo method, object[]? args)
    {
        var dataRows = method.GetCustomAttributes<TestMethodAttribute>().FirstOrDefault()!;
        var descriptionString = dataRows.Description != null ? $"({dataRows.Description})" : "";
        string testInfo = $"{method.Name}{descriptionString}";

        var timeoutAttr = method.GetCustomAttribute<TimeoutAttribute>();

        try
        {
            if (timeoutAttr != null)
            {
                var testTask = InvokeAsync(instance, method, args);
                var delayTask = Task.Delay(timeoutAttr.Milliseconds);

                var completedTask = await Task.WhenAny(testTask, delayTask);

                if (completedTask == delayTask)
                {
                    throw new TestTimeoutException(timeoutAttr.Milliseconds);
                }

                await testTask;
            }
            else
            {
                await InvokeAsync(instance, method, args);
            }

            _logger.PrintSuccess(testInfo);
        }
        catch (TestFailedException ex)
        {
            _logger.PrintFailed(testInfo, ex.Message);
        }
        catch (TestTimeoutException ex)
        {
            _logger.PrintFailed(testInfo, ex.Message);
        }
        catch (TargetInvocationException ex)
        {
            var testException = ex.InnerException;
            if (testException is TestFailedException)
            {
                _logger.PrintFailed(testInfo, testException.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.PrintCrashed(testInfo, ex);
        }
    }

    private async Task InvokeAsync(object instance, MethodInfo method, object[]? args)
    {
        object? result = method.Invoke(instance, args);
        if (result is Task task)
        {
            await task;
        }
    }

    private object? CreateInstance(Type testClass)
    {
        object? instance;

        try
        {
            instance = Activator.CreateInstance(testClass);
        }
        catch (Exception ex)
        {
            _logger.Print($"[CRITICAL] Failed to create test class {testClass}: {ex.Message}");
            return null;
        }

        return instance;
    }
}