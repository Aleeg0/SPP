using System.Diagnostics;
using System.Reflection;
using MyTest.attributes;
using MyTestLauncher;
using SharedUtils.Utils;
using ThreadPoolLib;

var testAssembly = Assembly.LoadFrom("MyTestProject.dll");
var logger = new Logger();
ConsoleColor loggerColor = ConsoleColor.DarkGray;

Console.WriteLine("=================================================");
Console.WriteLine("   Сравнение производительности: 1 поток vs 4 потока");
Console.WriteLine("=================================================\n");

Console.WriteLine(">>> Запуск в 1 поток (Sequential)...");

Predicate<MethodInfo> myFilter = method =>
{
    var category = method.GetCustomAttribute<CategoryAttribute>();
    return category != null && category.Name == "Main";
};

var sequentialRunner = new TestLauncher(logger,1,1);
var stopwatch = Stopwatch.StartNew();
sequentialRunner.LaunchTest(testAssembly, myFilter);
stopwatch.Stop();
var sequentialTime = stopwatch.Elapsed;
sequentialRunner.Dispose();

Thread.Sleep(1000);
Console.WriteLine($"\n>>> Время выполнения (1 поток): {sequentialTime.TotalSeconds:F3} сек.\n");

Console.WriteLine(new string('-', 50) + "\n");

Console.WriteLine(">>> Запуск в 4 потока (Parallel)...");

var parallelRunner = new TestLauncher(logger, 0, 4);
stopwatch.Restart();
parallelRunner.LaunchTest(testAssembly, myFilter);
stopwatch.Stop();
var parallelTime = stopwatch.Elapsed;
parallelRunner.Dispose();

Console.WriteLine($"\n>>> Время выполнения (4 потока): {parallelTime.TotalSeconds:F3} сек.\n");


Console.WriteLine("=================================================");
Console.WriteLine("                 ИТОГИ");
Console.WriteLine("=================================================");
Console.WriteLine($"Последовательно: {sequentialTime.TotalSeconds:F3} с");
Console.WriteLine($"Параллельно:     {parallelTime.TotalSeconds:F3} с");




//
// void SimulateTest(int testId)
// {
//     logger.Print($"[TEST] Запуск теста #{testId} в потоке {Thread.CurrentThread.ManagedThreadId}...", ConsoleColor.Cyan);
//     int workTime = new Random().Next(1000, 2000);
//     Thread.Sleep(workTime);
//     logger.Print($"[TEST] Тест #{testId} успешно завершен за {workTime} мс.", ConsoleColor.Green);
// }
//
// void SimulateStuckTest(int testId, int workTimeMs)
// {
//     logger.Print($"[TEST] Запуск ДОЛГОГО теста #{testId} в потоке {Thread.CurrentThread.ManagedThreadId} (ожидание {workTimeMs}мс)...", ConsoleColor.DarkYellow);
//     Thread.Sleep(workTimeMs);
// }
//
// void SimulateExceptionTest(int testId)
// {
//     logger.Print($"[TEST] Запуск теста с ОШИБКОЙ #{testId} в потоке {Thread.CurrentThread.ManagedThreadId}...", ConsoleColor.Magenta);
//     Thread.Sleep(500);
//     throw new InvalidOperationException($"Специальная ошибка в тесте #{testId}");
// }
//
//
// var pool = new MyThreadPool(0, 5, 3000, 5000);
// pool.OnIdleTimeout += worker => logger.Print($"[POOL] Worker #{worker.Id} removed due to idle.", loggerColor);
// pool.OnTaskComplete += worker => logger.Print($"[POOL] Worker #{worker.Id} complete task.", loggerColor);
// pool.OnTaskStuck += worker => logger.Print($"[POOL] Worker #{worker.Id} is STUCK. Replacing...", ConsoleColor.Red);
// pool.OnMonitorWake += (workers, taskCount) => logger.Print($"[POOL] Workers: {workers.Count} | ActiveWorkers: {workers.Count(w => w.IsExecuting)} | Tasks: {taskCount}", loggerColor);
//
// Console.WriteLine("\n--- 1. Единичные подачи (Пул работает на MinThreads) ---");
// pool.EnqueueTask(() => SimulateTest(1));
// Thread.Sleep(1000);
// pool.EnqueueTask(() => SimulateTest(2));
//
// Console.WriteLine("\n--- 2. Интервал бездействия (Ждем адаптивного сжатия) ---");
// Thread.Sleep(5000);
//
// Console.WriteLine("\n--- 3. Пиковая нагрузка (Пул должен расшириться до MaxThreads) ---");
// for (int i = 0; i < 50; i++)
// {
//     int taskId = i;
//     pool.EnqueueTask(() => SimulateTest(taskId + 10));
// }
// Thread.Sleep(10000);
//
// Console.WriteLine("\n--- 4. Зависшие задачи (Пул должен выявить STUCK и заменить воркеры) ---");
// for (int i = 0; i < 3; i++)
// {
//     int taskId = i + 100;
//     pool.EnqueueTask(() => SimulateStuckTest(taskId, 8000));
// }
//
// Thread.Sleep(10000);
//
//
// Console.WriteLine("\n--- 5. Задачи с исключениями (Пул не должен упасть) ---");
// for (int i = 0; i < 3; i++)
// {
//     int taskId = i + 200;
//     pool.EnqueueTask(() => SimulateExceptionTest(taskId));
// }
//
// pool.EnqueueTask(() => SimulateTest(999));
//
// Thread.Sleep(5000);
//
// pool.Dispose();
// Console.WriteLine("\n--- Тестирование завершено ---");