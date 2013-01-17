namespace EtlViewer
{
    using System;
    using EtlViewer.QueryFx;
    using System.Text;
    using Tx.Windows;

    class EventRecordProxy
    {
        public EventRecordProxy Previous { get; set; }

        private SystemEvent OriginalEvent;

        public EventRecordProxy(ref SystemEvent item)
        {
            this.OriginalEvent = item;
        }

        public SystemHeader Header
        {
            get { return this.OriginalEvent.Header; }
        }

        public Resolver Resolver
        {
            get
            {
                return SymbolHelper.GetResolver(this);
            }
        }

        #region Fast path properties

        public int Id
        {
            get { return this.OriginalEvent.Header.EventId; }
        }

        public Guid ProviderId
        {
            get { return this.OriginalEvent.Header.ProviderId; }
        }

        public int Level
        {
            get { return this.OriginalEvent.Header.Level; }
        }

        public ulong Keywords
        {
            get { return this.OriginalEvent.Header.Keywords; }
        }

        public Guid ActivityId
        {
            get { return OriginalEvent.Header.ActivityId; }
        }

        public Guid RelatedActivityId
        {
            get { return OriginalEvent.Header.RelatedActivityId; }
        }

        public uint ProcessId
        {
            get { return this.OriginalEvent.Header.ProcessId; }
        }

        public uint ThreadId
        {
            get { return this.OriginalEvent.Header.ThreadId; }
        }

        public DateTime TimeStamp
        {
            get { return this.OriginalEvent.Header.Timestamp; }
        }

        public string ProviderName
        {
            get { return this.Resolver.GetProviderName(this.OriginalEvent.Header.ProviderId); }
        }

        public string Symbol
        {
            get { return this.Resolver.GetSymbolName(this.OriginalEvent.Header.EventId); }
        }

        public ushort TaskId
        {
            get { return this.OriginalEvent.Header.Task; }
        }

        public string Message
        {
            get { return this.OriginalEvent.ToString(); }
        }

        public string Task
        {
            get { return Resolver.GetTaskName(this.OriginalEvent.Header.Task); }
        }

        public string Opcode
        {
            get { return this.Resolver.GetOpcodeName(this.OriginalEvent.Header.Opcode); }
        }

        public string Context
        {
            get { return this.OriginalEvent.Header.Context; }
        }

        public double TimeFromLastEvent
        {
            get { return this.Previous != null ? (this.OriginalEvent.Header.Timestamp - this.Previous.TimeStamp).TotalMilliseconds : 0; }
        }
        #endregion

        public object Details
        {
            get
            {
                try
                {
                    return this.BuildDetailsView();
                }
                catch (Exception exception)
                {
                    return exception.ToString();
                }
            }
        }

        private object BuildDetailsView()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Id").Append(" \t\t= ")
                .Append((int)this.OriginalEvent.Header.EventId).Append("\n");
            sb.Append("ProcessId").Append(" \t= ")
                .Append(this.OriginalEvent.Header.ProcessId).Append("\n");
            sb.Append("ThreadId").Append(" \t= ")
                .Append(this.OriginalEvent.Header.ThreadId).Append("\n");
            sb.Append("TimeCreated").Append(" \t= ")
                .Append(this.OriginalEvent.Header.Timestamp).Append("\n");
            sb.Append("ActivityId").Append(" \t= ")
                .Append(this.OriginalEvent.Header.ActivityId).Append("\n");
            sb.Append("RelatedActivityId").Append(" \t= ")
                .Append(this.OriginalEvent.Header.RelatedActivityId).Append("\n");
            sb.Append("Symbol").Append(" \t\t= ")
                .Append(this.Symbol).Append("\n");
            sb.Append("Task").Append(" \t\t= ")
                .Append(this.Task).Append("\n");
            sb.Append("ProviderName").Append(" \t= ")
                .Append(this.ProviderName).Append("\n");
            sb.Append("Opcode").Append(" \t\t= ")
                .Append(this.Opcode).Append("\n");
            sb.Append("Keywords").Append(" \t= ");
            this.GetFormattedKeywords(sb).Append("\n");
            sb.Append("ProviderId").Append(" \t= ")
                .Append(this.OriginalEvent.Header.ProviderId).Append("\n");
            sb.Append("Level").Append(" \t\t= ")
                .Append((int)this.OriginalEvent.Header.Level).Append("\n");
            sb.Append("Message").Append(" \t= ")
                .Append(this.Message).Append("\n");

            return sb.ToString();

        }

        private StringBuilder GetFormattedKeywords(StringBuilder builder)
        {
            ulong kval = this.OriginalEvent.Header.Keywords;
            builder.Append(kval.ToString("X"));
            if (this.Resolver != null)
            {
                builder.Append("   ");
                foreach (var kw in this.Resolver.Keywords)
                {
                    if ((kval & kw.Mask) != 0)
                    {
                        builder.Append(" | ").Append(kw.Name);
                    }
                }
            }
            return builder;
        }
    }
}
