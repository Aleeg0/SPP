using System.Reflection;
using MyTest.attributes;
using MyTest.exceptions;
using SharedUtils.Utils;
using ThreadPoolLib;

namespace MyTestLauncher;

public class TestLauncher : IDisposable
{
    private readonly ILogger _logger;
    private readonly MyThreadPool _threadPool;

    public TestLauncher(ILogger logger, int minThreads = 2, int maxThreads = 4)
    {
        _logger = logger;
        _threadPool = new MyThreadPool(minThreads, maxThreads, 5000, 10000)
        {
            Logger = logger
        };
    }

    public void LaunchTest(Assembly assembly)
    {
        var assemblyName = assembly.GetName().Name;
        _logger.Print($"=== Launch tests from assembly: {assemblyName} ===\n");
        var testClasses = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
            .ToList();

        foreach (var testClass in testClasses)
        {
            RunTestClass(testClass);
        }
    }

    private void RunTestClass(Type testClass)
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
                RunTestMethod(instance, method, testInitializeMethod, testCleanupMethod);
            }
        }
        else
        {
            using var countdown = new CountdownEvent(methods.Count);

            foreach (var method in methods)
            {
                _threadPool.EnqueueTask(() =>
                {
                    try
                    {
                        var instance = CreateInstance(testClass);
                        RunTestMethod(instance, method, testInitializeMethod, testCleanupMethod);
                    }
                    finally
                    {
                        countdown.Signal();
                    }
                });
            }

            countdown.Wait();
        }
    }

    private void RunTestMethod(object instance, MethodInfo method, MethodInfo? init, MethodInfo? cleanup)
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
            ExecuteTestMethod(instance, method);
        }
        catch (Exception ex)
        {
            _logger.PrintCrashed(method.Name, ex);
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

    private void ExecuteTestMethod(object instance, MethodInfo method)
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
                    InvokeTest(instance, method, dataRow.Values);
                }
            }
        }
        else
        {
            InvokeTest(instance, method, null);
        }
    }

    private void InvokeTest(object instance, MethodInfo method, object[]? args)
    {
        var dataRows = method.GetCustomAttributes<TestMethodAttribute>().FirstOrDefault()!;
        var descriptionString = dataRows.Description != null ? $"({dataRows.Description})" : "";
        string testInfo = $"{method.Name}{descriptionString}";

        var timeoutAttr = method.GetCustomAttribute<TimeoutAttribute>();

        try
        {
            if (timeoutAttr != null)
            {
                var testTask = Task.Run(() => Invoke(instance, method, args));

                if (!testTask.Wait(timeoutAttr.Milliseconds))
                {
                    throw new TestTimeoutException(timeoutAttr.Milliseconds);
                }

                testTask.GetAwaiter().GetResult();
            }
            else
            {
                Invoke(instance, method, args);
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
            else if (testException != null)
            {
                throw testException;
            }
        }
        catch (Exception ex)
        {
            _logger.PrintCrashed(testInfo, ex);
        }
    }

    private void Invoke(object instance, MethodInfo method, object[]? args)
    {
        object? result = method.Invoke(instance, args);

        if (result is Task task)
        {
            task.GetAwaiter().GetResult();
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

    public void Dispose()
    {
        _threadPool.Dispose();
    }
}