namespace EtlViewer
{
    using System.ComponentModel;

    class CheckedItem<T> : INotifyPropertyChanged
    {
        public T Item { get; set; }
        public string Name { get; set; }

        private bool isChecked;
        public bool IsChecked
        {
            get { return isChecked; }
            set
            {
                if (isChecked != value)
                {
                    isChecked = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("IsChecked"));
                    }
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
