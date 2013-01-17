namespace EtlViewer.QueryFx
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq.Dynamic;
    using System.Linq.Expressions;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Tx.Windows;

    abstract class EtlReader : IDisposable
    {
        protected readonly IEnumerable<string> files;
        public long StartTime;
        public long EndTime;
        public Guid[] EnabledProviders { get; private set; }
        protected ulong? EnabledKeywords { get; set; }
        public string WhereFilter { get; set; }
        public long EventCount { get; set; }
        public long SessionStartTime;
        public long SessionStopTime;
        internal bool SessionWindowInitialized { get; set; }
        public Func<EventRecordProxy, bool> WhereExpression { get; set; }

        public EtlReader(IEnumerable<string> files)
        {
            this.files = files;
            this.StartTime = long.MinValue;
            this.EndTime = long.MaxValue;
            this.SessionStopTime = long.MaxValue;
        }

        protected virtual void Reset()
        {
        }


        public void SetTimeFilter(long startTime, long endTime)
        {
            this.StartTime = startTime;
            this.EndTime = endTime;
            this.Reset();
        }

        //internal abstract IEnumerable<EventRecordProxy> GetEvents(Predicate<EventRecordProxy> filter = null, string predicate = null, params object[] values);
        internal abstract IObservable<EventRecordProxy> GetObservableEvents();
        internal virtual void StartPublish(IObservable<EventRecordProxy> observer)
        {
        }

        abstract public void Dispose();

        internal void SetEnabledProviders(Guid[] guids)
        {
            this.EnabledProviders = guids;
        }


        internal void SetWhereFilter(string whereFilter)
        {
            this.WhereFilter = whereFilter;
        }

        public bool ShouldFilterTimeWindow
        {
            get { return this.StartTime != long.MinValue && this.EndTime != long.MaxValue; }
        }

        public bool ShouldFilterProviders
        {
            get { return this.EnabledProviders != null && this.EnabledProviders.Length > 0; }
        }

        public bool ShouldFilterKeywords
        {
            get { return this.EnabledKeywords.HasValue && (KeywordDefinition.All.Mask ^ this.EnabledKeywords) != 0; }
        }

        public bool ShouldFilterByWhere
        {
            get { return !String.IsNullOrEmpty(this.WhereFilter) && this.WhereFilter.Trim().Length > 0; }
        }

        internal void SetKeywordFilter(ulong? keywordMask)
        {
            this.EnabledKeywords = keywordMask;
        }

        public IObservable<EventRecordProxy> GetObservable()
        {
            return this.GetEventsObservableImpl();
        }

        IObservable<EventRecordProxy> GetEventsObservableImpl()
        {
            IObservable<EventRecordProxy> results = this.GetObservableEvents();
            return results;
        }

    }

    public struct SystemEventWithMessage
    {
        public SystemEvent holder;

        public SystemEventWithMessage(ref SystemEvent e)
        {
            this.holder = e;
        }

        public SystemHeader Header { get { return holder.Header; } }

        public string Message
        {
            get { return this.holder.ToString(); }
        }
    }

    static class FilterParser
    {
        // Starts with ActivityId and look ahead should not yield a alpha/numeral/-_
        static Regex guidRegex = new Regex(@"(?<=ActivityId\s*!*\=*\s*)[A-Fa-f0-9]{8}-([A-Fa-f0-9]{4}-){3}[A-Fa-f0-9]{12}(?![A-Za-z09\-_])", RegexOptions.Compiled);

        static Regex rootactivityRegex = new Regex(@"(?<![a-zA-Z])RootActivityId[\s]*=@[0-9]+", RegexOptions.Compiled);
        internal static Regex MessageExpr = new Regex(@"(?<![a-zA-Z])(message)(?=([\s]*[=!<>.]+))", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        static Regex[] replacers;
        static string[][] fieldMap = new string[][] { 
            new string[]{"pid|process|processid", "Header.ProcessId"},
            new string[]{"tid|thread|threadid", "Header.ThreadId"},
            new string[]{"id|eventid", "Header.EventId"},
            new string[]{"level", "Header.Level"},
            new string[]{"activity|activityid", "Header.ActivityId"},
            new string[]{"relatedActivity|relatedActivityId|related", "Header.RelatedActivityId"},
            new string[]{"message", "Message"},
            new string[]{"rootactivity|rootactivityid", "RootActivityId"},
        };

        static FilterParser()
        {
            replacers = new Regex[fieldMap.Length];
            for (int i = 0; i < fieldMap.Length; i++)
            {
                // Example - new Regex(@"(?<![a-zA-Z])(id|eventid)(?=([\s]*[=!<>]+))", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                // Does not start with a character and if followed by an operator 
                string regexp = string.Format(@"(?<![a-zA-Z])({0})(?=([\s]*[=!<>.]+))", fieldMap[i][0]);
                replacers[i] = new Regex(regexp, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }
        }

        public static Func<T, bool> ParseWhere<T>(string predicate)
        {
            List<object> queryParameters = new List<object>();

            for (int i = 0; i < fieldMap.Length; i++)
            {
                predicate = replacers[i].Replace(predicate, fieldMap[i][1]);
            }
            predicate = ParameterizeGuids(predicate, queryParameters);
            predicate = ParseRootActivity<T>(predicate, queryParameters);
            Logger.Log("Filter: " + predicate);

            ParameterExpression[] parameters = new ParameterExpression[] { Expression.Parameter(typeof(T), "") };
            ExpressionParser parser = new ExpressionParser(parameters, predicate, queryParameters.ToArray());
            LambdaExpression expression = Expression.Lambda(parser.Parse(typeof(bool)), parameters);

            return (Func<T, bool>)expression.Compile();
        }



        private static string ParseRootActivity<T>(string predicate, List<object> queryParameters)
        {
            RootActivityContext context = null;
            predicate = rootactivityRegex.Replace(predicate, delegate(Match match)
            {
                string v = match.ToString();
                int paramIndex = Int32.Parse(v.Replace("RootActivityId=@", ""));
                Guid rootActivity = (Guid)queryParameters[paramIndex];
                context = new RootActivityContext(rootActivity);


                if (typeof(T) == typeof(SystemEvent))
                {
                    Expression<Func<SystemEvent, bool>> e1 = c => context.IsValid(c.Header.ActivityId, c.Header.RelatedActivityId);
                    queryParameters[paramIndex] = e1;
                }
                else
                {
                    Expression<Func<EventRecordProxy, bool>> e1 = c => context.IsValid(c.Header.ActivityId, c.Header.RelatedActivityId);
                    queryParameters[paramIndex] = e1;
                }

                v = "(@" + paramIndex + "(it)==true)";
                return v;
            });
            return predicate;
        }

        private static string ParameterizeGuids(string predicate, List<object> queryParameters)
        {
            predicate = guidRegex.Replace(predicate, delegate(Match match)
            {
                string v = match.ToString();
                queryParameters.Add(Guid.Parse(v));
                return "@" + (queryParameters.Count - 1);
            });
            return predicate;
        }

        class RootActivityContext
        {
            HashSet<Guid> guids = new HashSet<Guid>();

            public RootActivityContext(Guid root)
            {
                guids.Add(root);
            }


            public bool IsValid(Guid activity, Guid related)
            {
                if (this.guids.Contains(activity))
                {
                    if (related != Guid.Empty)
                    {
                        this.guids.Add(related);
                    }
                    return true;
                }
                else if (this.guids.Contains(related))
                {
                    if (activity != Guid.Empty)
                    {
                        this.guids.Add(activity);
                    }

                    return true;
                }

                return false;
            }
        }
    }
}
