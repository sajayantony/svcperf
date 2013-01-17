namespace EtlViewerQuery
{
    using EtlViewer.Viewer.Models;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public class SequenceDiagram
    {
        public string Title { get; set; }

        public Dictionary<string, SequenceItem> SequenceSteps { get; set; }
        public List<Tuple<SequenceItem, SequenceItem, string>> Connectors { get; set; }

        public SequenceDiagram()
        {
            this.SequenceSteps = new Dictionary<string, SequenceItem>();
            this.Connectors = new List<Tuple<SequenceItem, SequenceItem, string>>();
        }

        public void Add(SequenceItem item, string key = null)
        {
            if (String.IsNullOrEmpty(key))
            {
                key = item.Name;
            }

            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("Name", "Key/Name cannot be empty.");
            }

            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            this.SequenceSteps[key] = item;
        }

        public void Connect(SequenceItem from, SequenceItem to = null, string message = null)
        {
            if (from == null)
            {
                throw new ArgumentException("from");
            }

            if (message == null)
            {
                if (to != null)
                {
                    message = from.Name + "-" + to.Name;
                }
                else
                {
                    message = from.Name;
                }
            }

            this.Connectors.Add(new Tuple<SequenceItem, SequenceItem, string>(from, to, message));
        }

        public SequenceItem this[string name]
        {
            get
            {
                SequenceItem item;
                if (this.SequenceSteps.TryGetValue(name, out item))
                {
                    return item;
                }

                return null;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentException("SequenceItem", "Null is not a valid value");
                }
                this.SequenceSteps[name] = value;
            }
        }
    }
}