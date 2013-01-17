using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using EtlViewer;

namespace EtlViewer.QueryFx
{
    class EventLoglReaderHelper:EtlReader
    {
        public Action<string> OnFileLoading = null;
        public Action<string, Exception> OnFileOpenFailed = null;

        public EventLoglReaderHelper(string filename)
            :base(new string[]{filename})
        {

        }

        internal override bool StopReading
        {
            get
            {
                return UserState.StopReading;
            }
            set
            {
                UserState.StopReading = value;
            }
        }

        //internal override IEnumerable<EventRecordProxy> GetEvents(Predicate<EventRecordProxy> filter = null, string predicate = null, params object[] values)
        //{
        //    foreach (var item in this.ReadFile(string.Empty))
        //    {
        //        yield return new EventLogRecordProxy(item, this.FindResolver(item.ProviderId.Value));
        //    }
        //}

        IEnumerable<EventRecord> ReadFile(string filename)//, out IEnumerable<Guid> unknownProviderGuids)
        {
            this.StopReading = false;
            IEnumerable<Guid> unknownProviderGuids = null;
            string tempEtlFile = null;
            try
            {
                FileInfo fi = new FileInfo(filename);

                // 0 byte files will be skipped as they can occur under normal operation
                // and we don't want a dialog for them.
                if (fi.Exists && fi.Length == 0)
                {
                    yield break;
                    //return null;
                }

                if (this.OnFileLoading != null)
                {
                    this.OnFileLoading(filename);
                }

                EventLogReader logReader;
                try
                {
                    EventLogQuery eventsQuery = new EventLogQuery(filename, PathType.FilePath);
                    logReader = new EventLogReader(eventsQuery);
                }
                catch (EventLogException)
                {
                    // the file may be locked if it is still used by an running session
                    // copy it to a temp file and open from the temp file
                    try
                    {
                        tempEtlFile = Path.GetTempFileName();
                        fi.CopyTo(tempEtlFile, true);
                        EventLogQuery eventsQuery = new EventLogQuery(tempEtlFile, PathType.FilePath);
                        logReader = new EventLogReader(eventsQuery);
                    }
                    catch (Exception e)
                    {
                        if (this.OnFileOpenFailed != null)
                            this.OnFileOpenFailed(filename, e);
                        yield break;
                        //return null;
                    }
                }
                catch (Exception e)
                {
                    if (this.OnFileOpenFailed != null) this.OnFileOpenFailed(filename, e);
                    yield break;
                    //return null;
                }

                List<Guid> unknownProviders = new List<Guid>();
                // iterate first and store the EventRecord in a list, so that we know the total count.
                for (EventRecord eventInstance = logReader.ReadEvent(); null != eventInstance; eventInstance = logReader.ReadEvent())
                {
                    yield return eventInstance;
                    if (string.IsNullOrEmpty(eventInstance.ProviderName) && !unknownProviders.Contains(eventInstance.ProviderId.Value))
                    {
                        unknownProviders.Add(eventInstance.ProviderId.Value);
                    }
                    if (StopReading)
                    {
                        break;
                    }
                }

                if (unknownProviders.Count > 0)
                {
                    unknownProviderGuids = unknownProviders;
                }
                yield break;
            }
            finally
            {
                // clean up the temp file if we have created one
                if (!string.IsNullOrEmpty(tempEtlFile))
                {
                    try
                    {
                        File.Delete(tempEtlFile);
                    }
                    catch
                    {
                        // ignore exception
                    }
                }
            }
        }


        public override void Dispose()
        {
            
        }

        internal override IObservable<EventRecordProxy> GetObservableEvents()
        {
            throw new NotImplementedException();
        }
    }

    class EventLogRecordProxy : EventRecordProxy<EventRecord>
    {
        static Dictionary<int, string> TaskDictionary = new Dictionary<int, string>();
        string toString = string.Empty;

        Resolver resolver;


        public EventLogRecordProxy(EventRecord record,
            Resolver resolver)
            : base(record)
        {
            this.resolver = resolver;
        }


        protected override bool TryGetProperty<T>(EventRecordProxy<EventLogRecord>.Field field, out T propertyValue)
        {
            if (this.DataItem == null)
            {
                propertyValue = default(T);
                return false;
            }

            object val = null;
            switch (field)
            {
                case Field.Id:
                    val = this.DataItem.Id;
                    break;
                case Field.ProcessId:
                    val = this.DataItem.ProcessId;
                    break;
                case Field.ThreadId:
                    val = this.DataItem.ThreadId;
                    break;
                case Field.TimeCreated:
                    val = this.DataItem.TimeCreated;
                    break;
                case Field.ActivityId:
                    val = this.DataItem.ActivityId ?? Guid.Empty;
                    break;
                case Field.RelatedActivityId:
                    val = this.DataItem.RelatedActivityId ?? Guid.Empty;
                    break;
                case Field.Message:
                    val = this.DataItem.FormatDescription();
                    break;
                case Field.Symbol:
                    if (this.DataItem.ProviderId == TraceHelper.WcfProviderId)
                    {
                        val = this.resolver.GetSymbolName(this.DataItem.Id);
                    }
                    else
                    {
                        val = string.Empty;
                    }
                    break;
                case Field.Task:
                    if (this.DataItem.Task != null)
                    {
                        if (TaskDictionary.ContainsKey(this.DataItem.Task.Value))
                        {
                            val = TaskDictionary[this.DataItem.Task.Value];
                        }
                        else
                        {
                            string name = this.DataItem.TaskDisplayName;
                            TaskDictionary.Add(this.DataItem.Task.Value, name);
                            val = name;
                        }
                    }
                    break;
                case Field.ProviderName:
                    val = this.DataItem.ProviderName;
                    break;
                case Field.Opcode:
                    val = this.DataItem.OpcodeDisplayName ?? string.Empty;
                    break;
                case Field.Keywords:
                    val = (ulong)(this.DataItem.Keywords ?? 0);
                    break;
                case Field.Level:
                    val = (int)this.DataItem.Level;
                    break;
                case Field.ProviderId:
                    val = this.DataItem.ProviderId ?? Guid.Empty;
                    break;
            }

            propertyValue = (T)val;

            return val != null;
        }


        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(this.toString))
            {
                StringWriter writer = new StringWriter();
                try
                {
                    EventRecord e = this.DataItem;
                    writer.WriteLine(e.FormatDescription());
                    ObjectDumper.Write(e, 5, writer);
                    return writer.ToString();

                }
                catch (Exception ex)
                {
                    return ex.Message.ToString();
                }
            }
            return string.Empty;
        }

        public override object Details
        {
            get
            {
                return this.ToString();
            }
        }
    }
}
