namespace EtlViewer.Viewer.UIUtils
{
    using EtlViewer.QueryFx;
    using EtlViewer.Viewer.Controls;
    using EtlViewer.Viewer.Models;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Reactive;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;

    /// <summary>
    /// Class to execute and subscribe to the playback and handle execution
    /// </summary>
    class QueryWindowHelpers
    {
        internal static Task<object> ExecuteQueryAsync(QueryModel model,
                                                        string query,
                                                        Playback playback,
                                                        CancellationTokenSource cancelationToken,
                                                        Func<Type, IEnumerable, Action> onStart)
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            bool disposed = false;
            Thread compileTaskThread = null;
            model.IsBusy = true;
            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            model.LogActivity("Loading query");
            StringWriter outputWriter = new StringWriter();
            StringWriter writer = new StringWriter();
            List<Action> completions = new List<Action>();

            Func<Type, object, Action<object>> onDumpStart = (type, result) =>
            {
                IList items = null;
                Post(() =>
                {
                    var oCollectionType = typeof(ObservableCollection<>);
                    var list = oCollectionType.MakeGenericType(type);
                    items = (IList)Activator.CreateInstance(list);
                    model.ItemsSource = items;
                    var completion = onStart(type, items);
                    if (completion != null)
                    {
                        completions.Add(completion);
                    }
                });

                Action<object> onNext = (o) =>
                {
                    if (cancelationToken.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }

                    Post(() => items.Add(o));
                };

                return onNext;

            };

            cancelationToken.Token.Register(() =>
                {
                    try
                    {
                        if (playback != null && !disposed)
                        {
                            disposed = true;
                            Logger.Log("Execution cancelled.");
                            playback.Dispose();
                            Thread t = Interlocked.CompareExchange<Thread>(ref compileTaskThread, null, compileTaskThread);
                            if (t != null && t.IsAlive)
                            {
                                //TODO: Terminate the thread that does the query compile and execution. 
                                t.Abort();
                            }
                        }
                    }
                    catch { }

                    model.IsBusy = false;
                    model.LogActivity("Execution Cancelled.");
                });


            Action onComplete = () =>
            {
                Post(() =>
                {
                    model.LogActivity("Execution complete");
                    foreach (var complete in completions)
                    {
                        complete();
                    }
                });
            };

            QueryExecutionContext queryContext = new QueryExecutionContext(playback, onDumpStart);
            Task<bool> compileTask = new Task<bool>(() =>
            {
                compileTaskThread = Thread.CurrentThread;
                bool success = QueryCompiler.CompileAndRun(queryContext,
                                                            query,
                                                            writer,
                                                            outputWriter);

                // We block and run the playback for the query.
                // By now the Dump() outputs should have already started rendering.
                if (playback != null && success && !disposed)
                {
                    playback.Start();
                }

                //There is nothing to abort at this point.
                compileTaskThread = null;
                Logger.Log(outputWriter.ToString());
                return success;
            });

            compileTask.ContinueWith((t) =>
            {
                if (t.IsCompleted && t.Result == false)
                {
                    string message = writer.ToString();
                    if (queryContext.Task.Exception != null &&
                        queryContext.Task.Exception.InnerException is PlaybackUninitializedException)
                    {
                        MessageBox.Show("Please initialize a playback by loading an ETL or a session");
                    }
                    else
                    {
                        ExceptionHelper.ShowException(new Exception(message, t.Exception));
                    }
                    model.LogActivity("Compilation Exception");
                    model.IsBusy = false;
                }
                else
                {
                    queryContext.Task.ContinueWith((t3) =>
                    {
                        model.IsBusy = false;
                        onComplete();
                        if (queryContext.Task.IsFaulted)
                        {
                            tcs.SetException(queryContext.Task.Exception);
                        }
                        else
                        {
                            tcs.SetResult(null);
                        }
                    }, scheduler);
                }

            }, scheduler);

            compileTask.Start();
            return tcs.Task;
        }

        public static void PopulateChart(DurationHistogram chart,
                                        bool hasX,
                                        Type itemType,
                                        IEnumerable items)
        {
            MethodInfo method = typeof(QueryWindowHelpers).GetMethod("GetPointsDictionary", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo generic = method.MakeGenericMethod(itemType);
            var values = (double[][])generic.Invoke(null, new object[] { items, hasX });
            chart.LoadData(values, hasX);
        }

        public static bool CanShowChart(Type t, out bool hasX, out bool hasY)
        {
            PropertyInfo[] properties = t.GetProperties();
            hasX = false;
            hasY = false;
            if (properties.Count((e) => e.Name == "ValueX") == 1 || properties.Count((e) => e.Name == "ValueY") == 1)
            {
                // If there are XAxis values then plot a line graph
                if (properties.Count((e) => e.Name == "ValueX") == 1)
                {
                    hasX = true;
                }

                if (properties.Count((e) => e.Name == "ValueY") == 1)
                {
                    hasY = true;
                }

                // We plot only if there is x&y or just y values for distribution
                if (hasY)
                {
                    return true;
                }
            }

            return false;
        }

        private static double[][] GetPointsDictionary<T>(IEnumerable<T> o, bool hasXY)
        {
            List<double[]> points = new List<double[]>();
            if (hasXY)
            {
                foreach (T item in o)
                {
                    dynamic d = item;
                    points.Add(new double[] { (double)d.ValueX, (double)d.ValueY });
                }
            }
            else
            {
                foreach (T item in o)
                {
                    dynamic d = item;
                    points.Add(new double[] { 0, (double)d.ValueY });
                }
            }

            return points.ToArray();
        }

        private static void Post(Action o)
        {
            Application.Current.Dispatcher.Invoke(o);
        }
    }
}
