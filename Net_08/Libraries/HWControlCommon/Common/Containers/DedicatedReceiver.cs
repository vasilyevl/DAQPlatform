namespace Grumpy.SDAQFramework.Common;

/// <summary>
/// Processes queued items sequentially on one dedicated, long-lived thread.
/// </summary>
public sealed class DedicatedReceiver<T> : IDisposable
{
    private static readonly TimeSpan DefaultStopTimeout = TimeSpan.FromSeconds(10);

    private readonly FIFOBase<T> _queue;
    private readonly Action<T, CancellationToken> _receiver;
    private readonly AutoResetEvent _workAvailable = new(false);
    private readonly ManualResetEventSlim _started = new(false);
    private readonly CancellationTokenSource _stopSource = new();
    private readonly object _lifecycleLock = new();

    private Thread? _thread;
    private bool _acceptingItems;
    private bool _startedOnce;
    private bool _disposed;
    private Exception? _lastFaultNotificationException;

    /// <summary>
    /// Initializes a receiver with a bounded queue.
    /// </summary>
    public DedicatedReceiver(
        string name,
        int capacity,
        Action<T, CancellationToken> receiver)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(receiver);

        Name = name;
        _queue = new FIFOBase<T>(capacity, $"{name}.Queue");
        _receiver = receiver;
    }

    /// <summary>
    /// Raised on the receiver thread when item processing throws.
    /// </summary>
    public event EventHandler<ReceiverFaultedEventArgs<T>>? Faulted;

    /// <summary>Gets the receiver name.</summary>
    public string Name { get; }

    /// <summary>Gets the managed ID of the dedicated thread.</summary>
    public int? ThreadId => _thread?.ManagedThreadId;

    /// <summary>Gets whether the dedicated thread is running.</summary>
    public bool IsRunning => _thread?.IsAlive == true;

    /// <summary>Gets the number of queued items.</summary>
    public int PendingCount => _queue.Count;

    /// <summary>
    /// Gets the most recent exception thrown by a fault event subscriber.
    /// </summary>
    public Exception? LastFaultNotificationException =>
        Volatile.Read(ref _lastFaultNotificationException);

    /// <summary>
    /// Starts the dedicated thread and waits until it is ready.
    /// </summary>
    public void Start(TimeSpan? timeout = null)
    {
        lock (_lifecycleLock) {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_thread is not null) {
                return;
            }

            if (_startedOnce) {
                throw new InvalidOperationException(
                    $"Receiver '{Name}' cannot be restarted after it stops.");
            }

            _startedOnce = true;
            _acceptingItems = true;
            _thread = new Thread(Run) {
                IsBackground = true,
                Name = Name
            };
            _thread.Start();
        }

        TimeSpan startTimeout = timeout ?? TimeSpan.FromSeconds(5);
        if (!_started.Wait(startTimeout)) {
            Stop(DefaultStopTimeout);
            throw new TimeoutException(
                $"Receiver '{Name}' did not start within {startTimeout}.");
        }
    }

    /// <summary>
    /// Adds an item without waiting for queue space.
    /// </summary>
    public bool TrySubmit(T item, out string error)
    {
        lock (_lifecycleLock) {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_acceptingItems || _thread is null) {
                error = $"Receiver '{Name}' is not accepting items.";
                return false;
            }

            if (!_queue.Push(item, out error)) {
                return false;
            }
        }

        _workAvailable.Set();
        return true;
    }

    /// <summary>
    /// Requests shutdown, wakes the thread, and waits for it to finish.
    /// </summary>
    public bool Stop(TimeSpan timeout)
    {
        Thread? thread;

        lock (_lifecycleLock) {
            _acceptingItems = false;
            thread = _thread;

            if (thread is null) {
                return true;
            }

            _stopSource.Cancel();
            _workAvailable.Set();
        }

        if (thread == Thread.CurrentThread) {
            return false;
        }

        bool stopped = thread.Join(timeout);
        if (stopped) {
            lock (_lifecycleLock) {
                _thread = null;
            }
        }

        return stopped;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_lifecycleLock) {
            if (_disposed) {
                return;
            }
        }

        if (!Stop(DefaultStopTimeout)) {
            throw new TimeoutException(
                $"Receiver '{Name}' did not stop within {DefaultStopTimeout}.");
        }

        lock (_lifecycleLock) {
            if (_disposed) {
                return;
            }

            _disposed = true;
            _queue.Dispose();
            _workAvailable.Dispose();
            _started.Dispose();
            _stopSource.Dispose();
        }
    }

    private void Run()
    {
        CancellationToken cancellationToken = _stopSource.Token;
        _started.Set();

        while (!cancellationToken.IsCancellationRequested) {
            _workAvailable.WaitOne();

            while (!cancellationToken.IsCancellationRequested &&
                   _queue.Pop(out T item, out _)) {
                try {
                    _receiver(item, cancellationToken);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested) {
                    return;
                }
                catch (Exception exception) {
                    RaiseFaulted(item, exception);
                }
            }
        }
    }

    private void RaiseFaulted(T item, Exception exception)
    {
        EventHandler<ReceiverFaultedEventArgs<T>>? handlers = Faulted;
        if (handlers is null) {
            return;
        }

        var eventArgs = new ReceiverFaultedEventArgs<T>(item, exception);
        foreach (EventHandler<ReceiverFaultedEventArgs<T>> handler in
                 handlers.GetInvocationList()) {
            try {
                handler(this, eventArgs);
            }
            catch (Exception notificationException) {
                Volatile.Write(
                    ref _lastFaultNotificationException,
                    notificationException);
            }
        }
    }
}

/// <summary>Provides details about a receiver callback failure.</summary>
public sealed class ReceiverFaultedEventArgs<T> : EventArgs
{
    /// <summary>Initializes the event data.</summary>
    public ReceiverFaultedEventArgs(T item, Exception exception)
    {
        Item = item;
        Exception = exception;
    }

    /// <summary>Gets the item being processed when the failure occurred.</summary>
    public T Item { get; }

    /// <summary>Gets the processing exception.</summary>
    public Exception Exception { get; }
}
