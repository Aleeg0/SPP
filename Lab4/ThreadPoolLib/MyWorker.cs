namespace ThreadPoolLib;

public class MyWorker
{
    private readonly Thread _thread;
    private readonly object _taskLock = new();

    private Action? _task;
    private DateTime? _executeTime;
    private bool _isStopped;
    private readonly int _threadIdleTimeout;

    public event Action<MyWorker>? OnTaskComplete;
    public event Action<MyWorker>? OnIdleTimeout;

    public MyWorker(int threadIdleTimeout)
    {
        _threadIdleTimeout = threadIdleTimeout;
        _thread = new Thread(WorkLoop) { IsBackground = true };
    }

    public DateTime? ExecuteTime { get { lock (_taskLock) return _executeTime; } }
    public bool IsExecuting { get { lock (_taskLock) return _executeTime.HasValue; } }
    public bool HasTask { get { lock (_taskLock) return _task != null; } }
    public int Id => _thread.ManagedThreadId;

    public void Start() => _thread.Start();

    public void AssignTask(Action task)
    {
        lock (_taskLock)
        {
            _task = task;
            _executeTime = DateTime.Now;
            Monitor.Pulse(_taskLock);
        }
    }

    internal void ClearTask()
    {
        lock (_taskLock)
        {
            _task = null;
            _executeTime = null;
        }
    }

    public void Stop()
    {
        lock (_taskLock)
        {
            _isStopped = true;
            Monitor.Pulse(_taskLock);
        }
    }


    private void WorkLoop()
    {
        while (!_isStopped)
        {
            Action? taskToRun = null;
            bool isIdle = false;

            lock (_taskLock)
            {
                while (_task == null)
                {
                    if (!Monitor.Wait(_taskLock, _threadIdleTimeout))
                    {
                        isIdle = true;
                        break; // Выходим из цикла, чтобы отпустить лок перед вызовом события!
                    }
                    if (_isStopped) return;
                }

                if (!isIdle) taskToRun = _task;
            }

            if (isIdle)
            {
                OnIdleTimeout?.Invoke(this);
                continue;
            }

            try
            {
                taskToRun?.Invoke();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[WORKER ERROR] Worker #{Id} crashed during task: {ex.Message}");
                Console.ResetColor();
            }
            finally
            {
                OnTaskComplete?.Invoke(this);
            }
        }
    }
}