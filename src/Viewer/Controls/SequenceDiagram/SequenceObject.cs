namespace EtlViewer.Viewer.Controls
{
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for SequenceDiagram.xaml
    /// </summary>
    /// <summary>
    /// Container for Sequence Object data
    /// </summary>
    partial class SequenceObject : Control
    {
        #region DependencyProperties and default styling

        /// <summary>
        /// Static initalization placeholder to avoid performance issues with static constructors
        /// </summary>
        private static readonly bool ControlInitialized = InitializeSequenceObject();

        /// <summary>
        /// Set <see cref="DefaultStyleKeyProperty.OverrideMetadata"/> for our type.
        /// This method should be called once by a static field on SequenceObject.
        /// </summary>
        /// <returns>boolean true, always...</returns>
        private static bool InitializeSequenceObject()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SequenceObject), new FrameworkPropertyMetadata(typeof(SequenceObject)));
            return true;
        }

        /// <summary>
        /// Dependency property for Sequence object title
        /// </summary>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(SequenceObject));

        /// <summary>
        /// Dependency property for corner radius on title box
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(SequenceObject));

        /// <summary>
        /// Dependency property for setting lead width
        /// </summary>
        public static readonly DependencyProperty LeadWidthProperty = DependencyProperty.Register("LeadWidth", typeof(double), typeof(SequenceObject));

        #endregion

        #region Local DependencyProperty wireups

        /// <summary>
        /// Sequence object title
        /// </summary>
        [Category("Data")]
        [Description("Set Object Title")]
        public string Title
        {
            get { return GetValue(TitleProperty) as string; }
            set { SetValue(TitleProperty, value); }
        }

        [Category("Appearance")]
        [Description("Set Corner Radius")]
        [TypeConverter(typeof(CornerRadiusConverter))]
        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        [Category("Appearance")]
        [Description("Set Lead Width")]
        [TypeConverter(typeof(LengthConverter))]
        public double LeadWidth
        {
            get { return (double)GetValue(LeadWidthProperty); }
            set { SetValue(LeadWidthProperty, value); }
        }

        #endregion        
    }
}
