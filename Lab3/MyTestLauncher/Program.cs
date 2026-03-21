using System.Diagnostics;
using System.Reflection;
using MyTestLauncher;
using SharedUtils.Utils;

var testAssembly = Assembly.LoadFrom("MyTestProject.dll");
var logger = new Logger();

Console.WriteLine("=================================================");
Console.WriteLine("   Сравнение производительности: 1 поток vs 4 потока");
Console.WriteLine("=================================================\n");

Console.WriteLine(">>> Запуск в 1 поток (Sequential)...");

var sequentialRunner = new TestLauncher(logger,1,1);
var stopwatch = Stopwatch.StartNew();
sequentialRunner.LaunchTest(testAssembly);
stopwatch.Stop();
var sequentialTime = stopwatch.Elapsed;
sequentialRunner.Dispose();

Thread.Sleep(1000);
Console.WriteLine($"\n>>> Время выполнения (1 поток): {sequentialTime.TotalSeconds:F3} сек.\n");

Console.WriteLine(new string('-', 50) + "\n");

Console.WriteLine(">>> Запуск в 4 потока (Parallel)...");

var parallelRunner = new TestLauncher(logger, 0, 4);
stopwatch.Restart();
parallelRunner.LaunchTest(testAssembly);
stopwatch.Stop();
var parallelTime = stopwatch.Elapsed;
parallelRunner.Dispose();

Console.WriteLine($"\n>>> Время выполнения (4 потока): {parallelTime.TotalSeconds:F3} сек.\n");


Console.WriteLine("=================================================");
Console.WriteLine("                 ИТОГИ");
Console.WriteLine("=================================================");
//Console.WriteLine($"Последовательно: {sequentialTime.TotalSeconds:F3} с");
Console.WriteLine($"Параллельно:     {parallelTime.TotalSeconds:F3} с");