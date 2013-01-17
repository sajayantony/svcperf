namespace EtlViewer.QueryFx
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Threading;
    using System.Threading.Tasks;
    using Tx.Windows;

    /// <summary>
    /// Class used to communicate with the query execution engine
    /// </summary>
    public class QueryExecutionContext : IDisposable
    {
        int disposed;
        [ThreadStatic]
        internal static QueryExecutionContext Current;
        List<object> results;
        volatile bool cancelled;
        volatile Exception exception;
        TaskCompletionSource<object> CompletionSource { get; set; }
        List<TaskCompletionSource<object>> PendingTasks { get; set; }
        Func<Type, object, Action<object>> OnStart { get; set; }

        internal Playback Playback { get; private set; }
        public Task<object> Task
        {
            get
            {
                return this.CompletionSource.Task;
            }
        }


        public IList<object> Results
        {
            get
            {
                return this.results;
            }
        }

        /// <summary>
        /// The context will be used to setup the enumeration on the object.
        /// </summary>
        /// <param name="playback"></param>
        /// <param name="onNext">Action to be invoked to return items. Pass in NULL to avoid playback execution or item iteration.</param>
        /// <param name="onStart"></param>
        public QueryExecutionContext(Playback playback,
                                    Func<Type, object, Action<object>> onStart)
        {
            this.Playback = playback;
            this.OnStart = onStart;
            this.CompletionSource = new TaskCompletionSource<object>();
            this.results = new List<object>();
            this.PendingTasks = new List<TaskCompletionSource<object>>();
            this.cancelled = false;

            // Add the global exception handler to the playback scheduler
            // this avoids exception and from crashing the app. 
            if (playback != null)
            {
                playback.Scheduler.Catch<Exception>((ex) =>
                {
                    if (this.exception != null)
                    {
                        this.exception = ex;
                    }
                    return true;
                });
            }
        }

        internal void Run()
        {
            this.ThrowIfDisposed();

            if (this.PendingTasks.Count > 0)
            {
                Task<object>.Factory.ContinueWhenAll(
                this.PendingTasks.Select((e) => e.Task).ToArray(), (results) =>
                {
                    if (this.cancelled)
                    {
                        this.CompletionSource.TrySetCanceled();
                    }
                    else if (this.exception != null)
                    {
                        this.CompletionSource.TrySetException(this.exception);
                    }
                    else
                    {
                        this.CompletionSource.TrySetResult(null);
                    }
                    return null;
                });
            }
            else
            {
                this.CompletionSource.TrySetResult(null);
            }
        }

        internal void OnDumpStart(Type type, object result,
                                out Action<object> onNext,
                                out Action onComplete,
                                out Action<Exception> onError)
        {
            this.ThrowIfDisposed();

            this.results.Add(result);
            var userOnNext = this.OnStart(type, result);

            if (userOnNext == null)
            {
                onNext = null;
                onComplete = null;
                onError = null;
            }
            else
            {
                TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
                this.PendingTasks.Add(tcs);
                onNext = (o) =>
                {
                    if (!tcs.Task.IsCanceled || !tcs.Task.IsFaulted)
                    {
                        userOnNext(o);
                    }
                };

                onComplete = () =>
                    {
                        tcs.TrySetResult(null);
                    };
                onError = (ex) =>
                    {
                        tcs.TrySetException(ex);
                    };
            }
        }


        public void Cancel()
        {
            this.cancelled = true;

            if (this.PendingTasks.Count != 0)
            {
                foreach (var item in this.PendingTasks)
                {
                    item.TrySetCanceled();
                }
            }
            else
            {
                this.CompletionSource.TrySetCanceled();
            }
        }

        internal void SetException(Exception ex)
        {
            this.exception = ex;
            if (this.PendingTasks.Count != 0)
            {
                foreach (var item in this.PendingTasks)
                {
                    if (item.Task.IsCompleted || item.Task.IsFaulted)
                    {
                        item.TrySetCanceled();
                    }
                }
            }
            else
            {
                this.CompletionSource.TrySetException(ex);
            }
        }

        internal static QueryExecutionContext CreateFromFiles(IList<string> etlfiles, Action<Type> onStart, Action<object> onNext)
        {
            Playback playback = new Playback();
            foreach (var item in etlfiles)
            {
                playback.AddEtlFiles(item);
            }

            Func<Type, object, Action<object>> v = (t, o) =>
                {
                    onStart(t);
                    return onNext;
                };
            QueryExecutionContext context = new QueryExecutionContext(playback, v);
            playback.KnownTypes = ManifestCompiler.GetKnowntypesforPlayback();
            return context;
        }

        ~QueryExecutionContext()
        {
            this.Dispose();
        }
        

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref this.disposed, 1, 0) == 0)
            {
                Playback p = this.Playback;
                if (p != null)
                {
                    try
                    {
                        // Try to cancel any outstanding operations.
                        this.Cancel();
                    }
                    catch (Exception)
                    {                                               
                    }

                    try
                    {
                        Logger.Log("Disposing playback");
                        p.Dispose();                        
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        void ThrowIfDisposed()
        {
            if (this.disposed != 0)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }
    }
}
