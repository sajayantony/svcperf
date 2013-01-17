namespace EtlViewer.Viewer.Controls
{

    using EtlViewerQuery;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Media;

    /// <summary>
    /// Basic container for Sequence Diagram elements
    /// </summary>
    partial class SequenceDiagram : UserControl
    {
        #region Dependency Properties

        /// <summary>
        /// Diagram title dependency property
        /// </summary>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            "Title",
            typeof(string),
            typeof(SequenceDiagram), new
            PropertyMetadata("No Title"));

        /// <summary>
        /// Diagram sequence object collection dependency property
        /// </summary>
        public static readonly DependencyProperty SequenceObjectsProperty = DependencyProperty.Register(
            "SequenceObjects",
            typeof(ObservableCollection<SequenceItem>), typeof(SequenceDiagram), new
            PropertyMetadata(new
            ObservableCollection<SequenceItem>()), ValidateSequenceObjects);

        /// <summary>
        /// Validate valid collection applied
        /// </summary>
        /// <param name="value">Valid collection instance</param>
        /// <returns>bool true if collection is not null</returns>
        public static bool ValidateSequenceObjects(object value)
        {
            return value != null;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Diagram title
        /// </summary>
        [Category("Data")]
        [Description("Set Sequence Diagram Title")]
        public string Title
        {
            get { return GetValue(SequenceDiagram.TitleProperty) as string; }
            set { SetValue(SequenceDiagram.TitleProperty, value); }
        }

        /// <summary>
        /// Diagram sequence object collection for display
        /// </summary>
        [Category("Data")]
        [Description("Set Object collection of sequence diagram objects")]
        public ObservableCollection<SequenceItem> SequenceObjects
        {
            get { return GetValue(SequenceDiagram.SequenceObjectsProperty) as ObservableCollection<SequenceItem>; }
            set { SetValue(SequenceDiagram.SequenceObjectsProperty, value); }
        }

        #endregion

        ObservableCollection<BaseActivityAdorner> connectors = new ObservableCollection<BaseActivityAdorner>();

        /// <summary>
        /// Basic constructor
        /// </summary>
        public SequenceDiagram()
        {
            InitializeComponent();

            // Sample data to use when in design mode
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                ObservableCollection<SequenceItem> seqObjects = SequenceObjects;
                seqObjects.Add(new SequenceItem("Item 1"));
                seqObjects.Add(new SequenceItem("Item 2"));
                seqObjects.Add(new SequenceItem("Item 3"));
            }
        }

        public void Reset()
        {
            AdornerLayer layer = AdornerLayer.GetAdornerLayer(this.SequenceItems);
            foreach (var item in this.connectors)
            {
                layer.Remove(item);
            }
            this.connectors.Clear();
        }

        public void AddConnector(object from, object to, string message)
        {
            var seq1 = GetChild<SequenceObject>(this.SequenceItems.ItemContainerGenerator.ContainerFromItem(from));
            var seq2 = to != null ? GetChild<SequenceObject>(this.SequenceItems.ItemContainerGenerator.ContainerFromItem(to)) : null;
            AddConnector(seq1, seq2, message);
        }

        void AddConnector(SequenceObject source, SequenceObject target, string message)
        {
            var layer = AdornerLayer.GetAdornerLayer(this.SequenceItems);
            if (target != null)
            {
                var connector = new ConnectorAdorner(this.SequenceItems);
                connector.Source = source;
                connector.To = target;
                connector.Message = message;
                this.connectors.Add(connector);
                layer.Add(connector);
            }
            
            var point = new PointActivity(this.SequenceItems);
            point.Source = source;
            point.Message = message;
            this.connectors.Add(point);
            layer.Add(point);

            this.ResetConnectors();
        }

        private void ResetConnectors()
        {
            double ratio = 1.0 / this.connectors.Count((e) => e is PointActivity);
            int index = 0;
            foreach (var item in this.connectors)
            {
                item.Ratio = ratio;
                item.Index = index;
                item.TopOffset = 30;
                item.ParentHeight = this.LastRenderSize.Height;
                item.ParentWidth = this.LastRenderSize.Width;
                if (item is PointActivity)
                {
                    index++;
                }
            }

            foreach (var item in connectors)
            {
                item.InvalidateVisual();
            }
        }

        public Size LastRenderSize
        {
            get;
            set;
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            this.LastRenderSize = base.ArrangeOverride(arrangeBounds);
            this.ResetConnectors();
            return this.LastRenderSize;
        }


        static T GetChild<T>(DependencyObject obj) where T : class
        {
            while (obj != null)
            {
                obj = VisualTreeHelper.GetChild(obj, 0);
                if (obj is T)
                {
                    return obj as T;
                }
            }

            return null;
        }
    }

}
