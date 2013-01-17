namespace EtlViewer.QueryFx
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    abstract class Resolver
    {
        object thisLock = new object();
        internal IEnumerable<TaskDefinition> Tasks { get; set; }
        internal IEnumerable<EventDefinition> Symbols { get; set; }
        internal IEnumerable<KeywordDefinition> Keywords { get; set; }
        internal IEnumerable<OpcodeDefinition> Opcodes { get; set; }
        internal IEnumerable<MessagDefinition> Messages { get; set; }
        public Guid ProviderId { get; internal set; }
        public string ProviderName { get; internal set; }

        // Lookup tables
        Dictionary<uint, EventDefinition> symbols;
        Dictionary<int, TaskDefinition> tasks;
        Dictionary<int, string> opcodes;
        Dictionary<string, string> strings;
        bool initialized;

        internal string GetTaskName(int p)
        {
            EnsureInitialized();

            if (tasks.ContainsKey(p))
            {
                return tasks[p].Name;
            }

            return "Task_" + p;
        }

        public string GetSymbolName(uint id)
        {
            EnsureInitialized();

            if (symbols.ContainsKey(id))
            {
                return symbols[id].Name;
            }

            return "Symbol_" + id;
        }


        internal string GetOpcodeName(byte p)
        {
            EnsureInitialized();

            string opcode;
            if (this.opcodes.TryGetValue((int)p, out opcode))
            {
                return opcode;
            }
            else
            {
                return "Opcode_" + p;
            }
        }

        internal string GetProviderName(Guid guid)
        {
            EnsureInitialized();

            return this.ProviderName;
        }

        protected void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            lock (thisLock)
            {
                Dictionary<uint, EventDefinition> s = new Dictionary<uint, EventDefinition>();
                Dictionary<int, TaskDefinition> t = new Dictionary<int, TaskDefinition>();
                Dictionary<int, string> o = new Dictionary<int, string>();
                Dictionary<string, string> m = new Dictionary<string, string>();

                this.symbols = s;
                this.tasks = t;
                this.opcodes = o;
                this.strings = m;

                if (String.IsNullOrEmpty(this.ProviderName))
                {
                    this.ProviderName = this.ProviderId.ToString();
                }

                if (this.Symbols != null)
                {
                    foreach (var item in this.Symbols)
                    {
                        s.Add(item.Id, item);
                    }
                }

                if (this.Tasks != null)
                {
                    foreach (var item in this.Tasks)
                    {
                        t.Add(item.Id, item);
                    }
                }

                foreach (var item in winOpcodes.Values)
                {
                    o.Add(item.Id, item.Name);
                }

                if (this.Opcodes != null)
                {
                    foreach (var item in this.Opcodes)
                    {
                        if (!o.ContainsKey(item.Id))
                        {
                            o.Add(item.Id, item.Name);
                        }
                        else
                        {
                            Logger.Log(string.Format("WARNING: Manifest {0} contains duplicate opcodes {1}-{2} ", this.ProviderName, item.Id, item.Name));
                        }
                    }
                }

                if (this.Messages != null)
                {
                    foreach (var item in this.Messages)
                    {
                        m.Add(item.Id, item.Message);
                    }
                }

                initialized = true;
            }
        }


        static Dictionary<int, OpcodeDefinition> winOpcodes = new Dictionary<int, OpcodeDefinition>()
                    {
                          {  0	    ,   new OpcodeDefinition(0	,	"win:Info"      )},	//	WINEVENT_OPCODE_INFO	An informational event.
                          {  1	    ,   new OpcodeDefinition(1	,	"win:Start"     )},	//	WINEVENT_OPCODE_START	An event that represents starting an activity.
                          {  2	    ,   new OpcodeDefinition(2	,	"win:Stop"      )},	//	WINEVENT_OPCODE_STOP	An event that represents stopping an activity. The event corresponds to the last unpaired start event.
                          {  3	    ,   new OpcodeDefinition(3	,	"win:DC_Start"  )},	//	WINEVENT_OPCODE_DC_START	An event that represents data collection starting. These are rundown event types.
                          {  4	    ,   new OpcodeDefinition(4	,	"win:DC_Stop"   )},	//	WINEVENT_OPCODE_DC_STOP	An event that represents data collection stopping. These are rundown event types.
                          {  5	    ,   new OpcodeDefinition(5	,	"win:Extension" )},	//	WINEVENT_OPCODE_EXTENSION	An extension event.
                          {  6	    ,   new OpcodeDefinition(6	,	"win:Reply"     )},	//	WINEVENT_OPCODE_REPLY	A reply event.
                          {  7	    ,   new OpcodeDefinition(7	,	"win:Resume"    )},	//	WINEVENT_OPCODE_RESUME	An event that represents an activity resuming after being suspended.
                          {  8	    ,   new OpcodeDefinition(8	,	"win:Suspend"   )},	//	WINEVENT_OPCODE_SUSPEND	An event that represents the activity being suspended pending another activity's completion.
                          {  9	    ,   new OpcodeDefinition(9	,	"win:Send"      )},	//	WINEVENT_OPCODE_SEND	An event that represents transferring activity to another component.
                          {  240	,   new OpcodeDefinition(240	,	"win:Receive"   )}	//	WINEVENT_OPCODE_RECEIVE	An event that represents receiving an activity transfer from another component.
                    };


        //public string FormatEvent(int id, object[] payloads)
        //{
        //    this.EnsureSymbols();
        //    var msg = this.strings[this.symbols[id].Message];

        //    StringBuilder sb = new StringBuilder();
        //    for (int i = 0; i < msg.Length; )
        //    {
        //        char c = msg[i++];
        //        if (c == '%')
        //        {
        //            int index = (int)msg[i++] - 49;
        //            sb.Append(payloads[index].ToString());
        //        }
        //        else
        //        {
        //            sb.Append(c);
        //        }
        //    }

        //    return sb.ToString();
        //}
    }

    class EmptyResolver : Resolver
    {
        public static EmptyResolver Instance = new EmptyResolver();

        public EmptyResolver()
        {
            this.Messages = new List<MessagDefinition>();
            this.Symbols = new List<EventDefinition>();
            this.Tasks = new List<TaskDefinition>();
            this.Opcodes = new List<OpcodeDefinition>();
            this.Keywords = new List<KeywordDefinition>();
            this.ProviderId = Guid.Empty;
            this.ProviderName = "Unknown Provider";
            base.EnsureInitialized();
        }
    }


    struct Provider
    {
        public string Name { get; set; }
        public Guid Id { get; set; }
        public Resolver Resolver { get; set; }
    }

    class TaskDefinition
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }


    internal struct MessagDefinition
    {
        public string Id;
        public string Message;

    }

    struct KeywordDefinition
    {
        public static KeywordDefinition All = new KeywordDefinition()
        {
            Mask = ulong.MaxValue,
            Name = "All"
        };

        public ulong Mask { get; set; }
        public string Name { get; set; } //Need property for databinding        
    }

    internal struct OpcodeDefinition
    {
        public int Id;
        public string Name;

        public OpcodeDefinition(int id, string name)
        {

            this.Id = id;
            this.Name = name;
        }
    }

    class EventDefinition
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        //public string Message { get; set; }
        public override string ToString()
        {
            return this.Name + " - " + Id;
        }
    }
}
