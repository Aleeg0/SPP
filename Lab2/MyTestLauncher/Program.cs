using System.Diagnostics;
using System.Reflection;
using MyTestLauncher;

var testAssembly = Assembly.LoadFrom("MyTestProject.dll");

Console.WriteLine("=================================================");
Console.WriteLine("   Сравнение производительности: 1 поток vs 4 потока");
Console.WriteLine("=================================================\n");

Console.WriteLine(">>> Запуск в 1 поток (Sequential)...");

var sequentialRunner = new TestLauncher(maxDegreeOfParallelism: 1);
var stopwatch = Stopwatch.StartNew();
await sequentialRunner.LaunchTestAsync(testAssembly);
stopwatch.Stop();
var sequentialTime = stopwatch.Elapsed;

Console.WriteLine($"\n>>> Время выполнения (1 поток): {sequentialTime.TotalSeconds:F3} сек.\n");

Console.WriteLine(new string('-', 50) + "\n");

Console.WriteLine(">>> Запуск в 4 потока (Parallel)...");

var parallelRunner = new TestLauncher(maxDegreeOfParallelism: 4);
stopwatch.Restart();
await parallelRunner.LaunchTestAsync(testAssembly);
stopwatch.Stop();
var parallelTime = stopwatch.Elapsed;

Console.WriteLine($"\n>>> Время выполнения (4 потока): {parallelTime.TotalSeconds:F3} сек.\n");


Console.WriteLine("=================================================");
Console.WriteLine("                 ИТОГИ");
Console.WriteLine("=================================================");
Console.WriteLine($"Последовательно: {sequentialTime.TotalSeconds:F3} с");
Console.WriteLine($"Параллельно:     {parallelTime.TotalSeconds:F3} с");