namespace EtlViewerQuery
{
    using System.ComponentModel;

    /// <summary>
    /// Sequence Object data container
    /// </summary>
    public class SequenceItem : INotifyPropertyChanged
    {
        #region Events

        /// <summary>
        /// Event identifying property change
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        /// <summary>
        /// Sequence Object name
        /// </summary>
        private string name;

        /// <summary>
        /// Define Sequence object name
        /// </summary>
        public string Name
        {
            get { return name; }
            set
            {
                if (string.Compare(name, value) != 0)
                {
                    name = value;
                    InvokePropertyChanged("Name");
                }
            }
        }

        /// <summary>
        /// Create a new SequenceItem container
        /// </summary>
        /// <param name="name">string Sequence Object Name</param>
        public SequenceItem(string name)
        {
            Name = name;
        }

        #region Event Handlers

        /// <summary>
        /// Manager for property change events
        /// </summary>
        /// <param name="property">string property to notify</param>
        protected void InvokePropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(property));
            }
        }

        #endregion
    }

}
