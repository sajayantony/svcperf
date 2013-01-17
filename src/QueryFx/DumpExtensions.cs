namespace EtlViewer.QueryFx
{
    using EtlViewerQuery;
    using System;
    using System.Collections.Generic;
    using System.Reactive.Linq;

    public static class DumpExtensions
    {
        /// <summary>
        /// Enumerble collections should have playback run already and hence we only call onNext. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        public static void Dump<T>(this IEnumerable<T> result)
        {
            QueryExecutionContext context = QueryExecutionContext.Current;
            Action<object> onNext;
            Action<Exception> onError;
            Action onComplete;
            context.OnDumpStart(typeof(T), result,
                                out onNext,
                                out onComplete,
                                out onError);
                
            try
            {                
                if (onNext != null)
                {
                    foreach (var item in result)
                    {
                        onNext(item);
                    }
                }
            }
            catch (Exception ex)
            {
                onError(ex);                
            }
            finally
            {
                onComplete();
            }
        }

        public static void Dump<T>(this IObservable<IList<T>> result)
        {
            QueryExecutionContext context = QueryExecutionContext.Current;
            Action<object> onNext;
            Action<Exception> onError;
            Action onComplete;
            context.OnDumpStart(typeof(T), result,
                                out onNext,
                                out onComplete,
                                out onError);
            try
            {
                if (onNext != null)
                {
                    result
                        .Finally(() =>
                        {
                            onComplete();
                        })
                        .Subscribe(
                        (data) =>
                        {
                            try
                            {
                                foreach (var item in data)
                                {
                                    onNext(item);
                                }
                            }
                            catch (Exception ex)
                            {
                                onError(ex);
                                throw;
                            }
                        });
                }
            }
            catch (Exception ex)
            {
                onError(ex);
            }
        }

        public static void Dump<T>(this IObservable<T> result)
        {
            QueryExecutionContext context = QueryExecutionContext.Current;
            Action<object> onNext;
            Action<Exception> onError;
            Action onComplete;
            context.OnDumpStart(typeof(T), result,
                                out onNext,
                                out onComplete,
                                out onError);
            try
            {
                if (onNext != null)
                {
                    result
                        .Finally(() =>
                        {
                            onComplete();
                        })
                        .Subscribe(
                        (data) =>
                        {
                            try
                            {
                                onNext(data);
                            }
                            catch (Exception ex)
                            {
                                onError(ex);
                                throw;
                            }
                        });
                }
            }
            catch (Exception ex)
            {
                onError(ex);
            }
        }

        public static void Dump(this SequenceDiagram result)
        {
            QueryExecutionContext context = QueryExecutionContext.Current;
            Action<object> onNext;
            Action<Exception> onError;
            Action onComplete;
            context.OnDumpStart(typeof(SequenceDiagram), result,
                                out onNext,
                                out onComplete,
                                out onError);
            try
            {
                if (onNext != null)
                {
                    onNext(result);
                }

            }
            catch (Exception ex)
            {
                onError(ex);
            }
            finally
            {
                onComplete();
            }
            
        }

        #region Dump Strings
        /// <summary>
        /// Strings are special objects that hurt the grid.
        /// </summary>
        /// <param name="str"></param>
        public static void Dump(this string str)
        {
            new string[] { str }.Dump();
        }

        public static void Dump(this object vt)
        {
            if (vt != null)
            {
                vt.ToString().Dump();
            }
            else
            {
                "NULL".Dump();
            }
        }

        public static void Dump(this IEnumerable<String> result)
        {
            QueryExecutionContext context = QueryExecutionContext.Current;
            Action<object> onNext;
            Action<Exception> onError;
            Action onComplete;
            context.OnDumpStart(typeof(StringWrapper), result,
                                out onNext,
                                out onComplete,
                                out onError);
                        
            if (onNext != null)
            {
                try
                {
                    foreach (var item in result)
                    {
                        onNext(new StringWrapper { Value = item });
                    }
                }
                catch (Exception ex)
                {
                    onError(ex);
                }
                finally
                {
                    onComplete();
                }
            }
        }

        /// <summary>
        /// Dummy wrapper class for silly strings
        /// </summary>
        class StringWrapper
        {
            public String Value { get; set; }
        }
        #endregion Dump Strings
    }
}
