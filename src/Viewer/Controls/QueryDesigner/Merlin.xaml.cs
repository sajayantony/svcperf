namespace EtlViewer.Viewer.Controls
{
    using EtlViewer.Viewer.Models;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    /// <summary>
    /// Interaction logic for Merlin.xaml
    /// </summary>
    partial class Merlin : UserControl
    {
        internal QueryEditorModel Model { get { return DataContext as QueryEditorModel; } }

        public Merlin()
        {
            InitializeComponent();

            this.DataContextChanged += Merlin_DataContextChanged;
        }

        void Merlin_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is QueryEditorModel)
            {
                QueryEditorModel model = e.NewValue as QueryEditorModel;
                model.BuildStartCommand.CanExecuteTargets += () => true;
                model.BuildStartCommand.ExecuteTargets += (o) =>
                {
                    // Source doesn't get updated for we need to move the 
                    // focus out for the bind to the source.
                    TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Next);
                    UIElement element = Keyboard.FocusedElement as UIElement;
                    if (element != null)
                    {
                        element.MoveFocus(request);
                        element.Focus();
                    }
                };
            }
        }

        private void ClosePanel_Click_1(object sender, RoutedEventArgs e)
        {
            this.Visibility = System.Windows.Visibility.Collapsed;
        }
    }

    class ParameterTemplateSelector : DataTemplateSelector
    {
        public DataTemplate StringTemplate { get; set; }
        public DataTemplate SelectorTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            QueryParameter qparam = item as QueryParameter;
            if (qparam != null && qparam.HasOptions)
            {
                return SelectorTemplate;
            }
            return this.StringTemplate;

            //return base.SelectTemplate(item, container);
        }
    }
}
