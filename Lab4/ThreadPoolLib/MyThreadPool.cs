namespace ThreadPoolLib;

public class MyThreadPool : IDisposable
{
    private readonly List<MyWorker> _workers = new ();
    private readonly Queue<Action> _tasks = new ();

    public int MinThreads { get; }
    public int MaxThreads { get; }
    private readonly int _threadIdleTimeout;
    private readonly int _taskMaxDuration;
    private readonly object _lock = new();
    private bool _isDisposed = false;

    public event Action<EventWorker>? OnIdleTimeout;
    public event Action<EventWorker>? OnTaskComplete;
    public event Action<EventWorker>? OnTaskStuck;
    public event Action<List<EventWorker>, int>? OnMonitorWake;

    public MyThreadPool(int minThreads, int maxThreads, int threadIdleTimeout, int taskMaxDuration)
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
                OnIdleTimeout?.Invoke(new EventWorker(worker.Id, worker.IsExecuting, worker.HasTask));
            }
        }

    }

    private void OnWorkerTaskComplete(MyWorker worker)
    {
        OnTaskComplete?.Invoke(new EventWorker(worker.Id, worker.IsExecuting, worker.HasTask));
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
            OnMonitorWake?.Invoke(
                _workers.Select(w => new EventWorker(w.Id, w.IsExecuting, w.HasTask)).ToList(),
                _tasks.Count
            );
            lock (_lock)
            {
                while (_workers.FirstOrDefault(w =>
                           w.ExecuteTime.HasValue &&
                           (DateTime.Now - w.ExecuteTime.Value).TotalMilliseconds > _taskMaxDuration) is { } stuckWorker)
                {
                    OnTaskStuck?.Invoke(new EventWorker(stuckWorker.Id, stuckWorker.IsExecuting, stuckWorker.HasTask));
                    _workers.Remove(stuckWorker);
                    var newWorker = CreateAndStartWorker();
                    _workers.Add(newWorker);

                    if (_tasks.TryDequeue(out var task))
                    {
                        newWorker.AssignTask(task);
                    }
                }
            }
            Thread.Sleep(_taskMaxDuration / 5);
        }
    }

    public void Dispose()
    {
        _isDisposed = true;
        foreach (var w in _workers) w.Stop();
    }
}