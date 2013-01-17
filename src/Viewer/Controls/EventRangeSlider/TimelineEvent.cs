using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EtlViewer
{
    public class TimelineEvent :IComparable<TimelineEvent>
    {
        public long Ticks { get; set; }
        public short Level { get; set; }

        public int CompareTo(TimelineEvent other)
        {
            return this.Ticks.CompareTo(other.Ticks);
        }
    }
}
