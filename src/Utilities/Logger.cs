namespace EtlViewer
{
    using EtlViewer.Viewer;
    using System;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.IO;
    using System.Text;

    public class Logger
    {
        static Logger Instance = new Logger();
        public static TextWriter Console = TextWriter.Null;

        public static string Log(string format, params object[] args)
        {
            return Log(string.Format(format, args));
        }

        public static string Log(string message, bool isError = false)
        {
            Debug.WriteLine(message);
            if (CollectionChanged != null)
            {
                CollectionChanged(Instance,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new LogEntry()
                    {
                        DateTime = DateTime.Now.ToString(),
                        LogMessage = message,
                        IsError = isError
                    }));
            }

            if (Console != null && Console != TextWriter.Null)
            {
                Console.Write("#");
                Console.WriteLine(message);
                return message;
            }

            return message;
        }

        public static string Log(Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            while (ex != null)
            {
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                }

                if (ex is AggregateException)
                {
                    foreach (var item in ((AggregateException)ex).InnerExceptions)
                    {
                        sb.Append(item.Message);
                        if (!String.IsNullOrEmpty(item.StackTrace))
                        {
                            sb.AppendLine();
                            sb.Append(item.StackTrace);
                        }
                    }
                }
                else
                {
                    sb.Append(ex.Message);
                    if (!String.IsNullOrEmpty(ex.StackTrace))
                    {
                        sb.AppendLine();
                        sb.Append(ex.StackTrace);
                    }
                }

                ex = ex.InnerException;
            }
            string message = sb.ToString();
            Log(message, true);
            return message;
        }

        public static event NotifyCollectionChangedEventHandler CollectionChanged;
    }
}
