namespace EtlViewer.QueryFx
{
    using EtlViewerQuery;
    using System;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Text.RegularExpressions;
    using Tx.Windows;

    class Summary
    {
        public const string GetDurationTemplate = "Tx.Summary.GetDuration<{0},{1}>().Dump();";
        public const string GetSlowRequestTemplate = "Tx.Summary.GetSlowRequest<{0},{1}>({2}).Dump();";
        internal static object GetDuration<T, U>()
            where T : SystemEvent
            where U : SystemEvent
        {
            //The Tx queries are composable.
            Playback scope = TxHelper.GetCurrentEtlScope(null, false);
            double bucketSize = 100; // Groups events in 100 milliseconds
            DateTime startTime = DateTime.Parse("1/1/0001 12:00:00 AM");
            DateTime stopTime = DateTime.Parse("12/31/9999 11:59:59 PM");

            Func<double, double> bucket = (s) => Math.Ceiling((s / bucketSize) * bucketSize);
            Func<SystemEvent, bool> withinWindow = (e) => e.Header.Timestamp >= startTime && e.Header.Timestamp <= stopTime;


            var startEvents = scope.GetObservable<T>().Where(withinWindow);
            var endEvents = scope.GetObservable<U>().Where(withinWindow);

            var requests = from b in startEvents
                           from e in endEvents.Where(e => e.Header.ActivityId == b.Header.ActivityId).Take(1)
                           select new
                           {
                               Timestamp = b.Header.Timestamp.ToString(),
                               ActivityId = b.Header.ActivityId,
                               Duration = e.Header.Timestamp - b.Header.Timestamp,
                           };


            var summary = from r in requests
                          group r by new
                          {
                              Milliseconds = bucket(r.Duration.TotalMilliseconds),
                          }
                              into groups
                              from c in groups.Count()
                              select new
                              {
                                  Milliseconds = groups.Key.Milliseconds,
                                  Count = c
                              };

            // Composition is also possible with LINQ-To-Objects.
            // Here we want to wait for the streaming computation to end, and then sort the result

            var sorted = from o in scope.BufferOutput(summary) // use this when there is only one output stream of the query graph                         
                        // orderby o.Milliseconds
                         select new DurationItem
                       {
                           Count = o.Count,
                           Duration = o.Milliseconds
                       };
            sorted.Dump();
            return sorted;
        }


        //internal static void TestRawDump<T>()
        //{
        //    Playback scope = TxHelper.GetCurrentEtlScope();
        //    var startEvents = scope.GetObservable<T>();
        //    startEvents.ToEnumerable<T>().Dump();
        //}


        static readonly Regex re = new Regex(@"\{([^\s\}]+)\}", RegexOptions.Compiled);

        #region Duration Query
        internal static string GetDurationQuery(StringDictionary fields)
        {
            string input = durationQueryTemplate;


            string output = re.Replace(input, delegate(Match match)
            {
                return fields[match.Groups[1].Value];
            });

            return output;
        }


        const string durationQueryTemplate = @"
// PRESS F5 to execute your query

double bucketSize = 100; // Groups events in 100 milliseconds
DateTime startTime = DateTime.Parse(""{startTime}"");
DateTime stopTime = DateTime.Parse(""{stopTime}"");

Func<double, double> bucket = (s) => Math.Ceiling(s/bucketSize)*bucketSize;
Func<SystemEvent, bool> withinWindow = (e) => e.Header.Timestamp >= startTime && e.Header.Timestamp <= stopTime;

var startEvents = playback.GetObservable<{startEvent}>().Where(withinWindow);
var endEvents = playback.GetObservable<{stopEvent}>().Where(withinWindow);

var requests = from start in startEvents
                from end in endEvents.Where(e => start.Header.ActivityId == e.Header.ActivityId).Take(1)
                select new
                {                    
                    ActivityId = start.Header.ActivityId,
                    Duration = end.Header.Timestamp - start.Header.Timestamp
                };

var stats = from request in requests
            group request by new
            {
                Milliseconds = bucket(request.Duration.TotalMilliseconds),                
            }
            into g
            from Count in g.Count()
            select new
            {
                Milliseconds = g.Key.Milliseconds,
                Count = Count
            };

// Composition is also possible with LINQ-To-Objects.
// Here we want to wait for the streaming computation to end, and then sort the result

var durationItems = from s in playback.BufferOutput(stats)
                    orderby s.Milliseconds
                    select new DurationItem
                    {
                        Count = s.Count,
                        Duration = s.Milliseconds
                    };
playback.Run();
durationItems.Dump();
";
        #endregion

        #region SlowRequestQuery
        public static string GetSlowRequestQuery(long startTime, 
                                                long stopTime,
                                                string startEvent, 
                                                string stopEvent, 
                                                string minduration)
        {
            string input = SlowRequestTempate;
            StringDictionary fields = new StringDictionary();
            fields.Add("startTime", new DateTime(startTime).ToString());
            fields.Add("stopTime", new DateTime(stopTime).ToString());
            fields.Add("startEvent", startEvent);
            fields.Add("stopEvent", stopEvent);
            fields.Add("minDuration", minduration);

            string output = re.Replace(input, delegate(Match match)
            {
                return fields[match.Groups[1].Value];
            });

            return output;
        }

        public const string SlowRequestTempate = @"

    // QueryGenerated From Drill down. 
    // To modify, copy this over and exectue in a new query window.
double bucketSize = 100; // Groups events in 100 milliseconds
DateTime startTime = DateTime.Parse(""{startTime}"");
DateTime stopTime = DateTime.Parse(""{stopTime}"");

Func<double, double> bucket = (s) => Math.Ceiling(s/bucketSize)*bucketSize;
Func<SystemEvent, bool> withinWindow = (e) => e.Header.Timestamp >= startTime && e.Header.Timestamp <= stopTime;

var startEvents = playback.GetObservable<{startEvent}>().Where(withinWindow);
var endEvents = playback.GetObservable<{stopEvent}>().Where(withinWindow);

var requests = from start in startEvents
                from end in endEvents.Where(e => start.Header.ActivityId == e.Header.ActivityId).Take(1)
                select new
                {                    
                    ActivityId = start.Header.ActivityId,
                    Duration = end.Header.Timestamp - start.Header.Timestamp
                };

var stats = from request in requests
                where bucket(request.Duration.TotalMilliseconds) == {minDuration}               
                select new { Activity = request.ActivityId };

var idQuery = from e in playback.BufferOutput(stats)
                select new { Id = e.Activity };
playback.Run(); // this does the sequence-compute, and fills up the above collection
idQuery.Dump();
";
        #endregion

    }
}
