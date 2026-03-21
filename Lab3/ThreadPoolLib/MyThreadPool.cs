using SharedUtils.Utils;

namespace ThreadPoolLib;

public class MyThreadPool : IDisposable
{
    private const ConsoleColor LoggerColor = ConsoleColor.DarkGray;

    private readonly List<MyWorker> _workers = new ();
    private readonly Queue<Action> _tasks = new ();

    public int MinThreads { get; }
    public int MaxThreads { get; }
    private readonly int _threadIdleTimeout;
    private readonly int _taskMaxDuration;
    private readonly object _lock = new();
    private bool _isDisposed = false;
    public ILogger? Logger { get; init; }

    public MyThreadPool(int minThreads = 1, int maxThreads = 1, int threadIdleTimeout = 5000, int taskMaxDuration = 5000)
    {
        MinThreads = minThreads;
        MaxThreads = maxThreads;
        _threadIdleTimeout = threadIdleTimeout;
        _taskMaxDuration = taskMaxDuration;

        for (int i = 0; i < MinThreads; i++) _workers.Add(CreateAndStartWorker());

        new Thread(MonitorSystem) { IsBackground = true }.Start();
    }

    public void EnqueueTask(Action task)
    {
        lock (_lock)
        {
            var idleWorker = _workers.FirstOrDefault(w => !w.IsExecuting);

            if (idleWorker != null)
            {
                idleWorker.AssignTask(task);
                return;
            }

            if (_workers.Count == MaxThreads)
            {
                _tasks.Enqueue(task);
                return;
            }

            var newWorker = CreateAndStartWorker();
            _workers.Add(newWorker);
            newWorker.AssignTask(task);

            Monitor.Pulse(_lock);
        }
    }

    private MyWorker CreateAndStartWorker()
    {
        var worker = new MyWorker(_threadIdleTimeout);
        worker.OnIdleTimeout += OnWorkerIdleTimeout;
        worker.OnTaskComplete += OnWorkerTaskComplete;
        worker.Start();
        return worker;
    }

    private void OnWorkerIdleTimeout(MyWorker worker)
    {
        lock (_lock)
        {
            if (_workers.Count > MinThreads && _tasks.Count == 0 && !worker.HasTask)
            {
                worker.Stop();
                _workers.Remove(worker);
                Logger?.Print($"[POOL] Worker #{worker.Id} removed due to idle.", LoggerColor);
            }
        }

    }

    private void OnWorkerTaskComplete(MyWorker worker)
    {
        Logger?.Print($"[POOL] Worker #{worker.Id} complete task.", LoggerColor);
        Logger?.Print($"[POOL] Workers: {_workers.Count} | ActiveWorkers: {_workers.Count(w => w.IsExecuting)} | Tasks: {_tasks.Count}", LoggerColor);
        lock (_lock)
        {
            if (_tasks.TryDequeue(out var task))
            {
                worker.AssignTask(task);
            }
            else
            {
                worker.ClearTask();
            }
        }
    }

    private void MonitorSystem()
    {
        while (!_isDisposed)
        {
            Logger?.Print($"[POOL] Workers: {_workers.Count} | ActiveWorkers: {_workers.Count(w => w.IsExecuting)} | Tasks: {_tasks.Count}", LoggerColor);
            lock (_lock)
            {
                MyWorker? stuckWorker;

                while ((stuckWorker = _workers.FirstOrDefault(w =>
                           w.ExecuteTime.HasValue &&
                           (DateTime.Now - w.ExecuteTime.Value).TotalSeconds > _taskMaxDuration)) != null)
                {
                    Logger?.Print($"[POOL] Worker #{stuckWorker.Id} is STUCK. Replacing...", ConsoleColor.Red);
                    _workers.Remove(stuckWorker);
                    var newWorker = CreateAndStartWorker();
                    _workers.Add(newWorker);

                    if (_tasks.TryDequeue(out var task))
                    {
                        newWorker.AssignTask(task);
                    }
                }
            }
            Thread.Sleep(_threadIdleTimeout / 5);
        }
    }

    public void Dispose()
    {
        _isDisposed = true;
        foreach (var w in _workers) w.Stop();
    }
}