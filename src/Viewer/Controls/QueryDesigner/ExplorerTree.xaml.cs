namespace EtlViewer.Viewer.Controls
{
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;

    class StringEventArgs : EventArgs
    {
        public StringEventArgs(string value)
        {
            this.Value = value;
        }
        public string Value { get; set; }
    }

    /// <summary>
    /// Interaction logic for ExplorerTree.xaml
    /// </summary>
    partial class ExplorerTree : UserControl
    {
        private readonly object _dummyNode = null;

        public EventHandler<StringEventArgs> FileSelected;

        public string DirectoryRoot
        {
            get { return (string)GetValue(DirectoryRootProperty); }
            set { SetValue(DirectoryRootProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DirectoryRoot.  
        // This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DirectoryRootProperty =
            DependencyProperty.Register("DirectoryRoot",
                                        typeof(string),
                                        typeof(ExplorerTree),
                                        new PropertyMetadata(string.Empty,
                                            new PropertyChangedCallback(OnDirectoryRootChanged)));

        static void OnDirectoryRootChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            // Get reference to self
            ExplorerTree source = (ExplorerTree)sender;

            // Add Handling Code
            string newValue = (string)args.NewValue;

            if (Directory.Exists(newValue) || File.Exists(newValue))
            {
                source.txtRoot.Text = Path.GetDirectoryName(newValue);
            }
        }


        public ExplorerTree()
        {
            InitializeComponent();
            this.Loaded += ExplorerTree_Loaded;
            this.txtRoot.TextChanged += txtRoot_TextChanged;
        }

        void txtRoot_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (Directory.Exists(this.txtRoot.Text))
            {
                foldersTree.Items.Clear();
                TreeViewItem item = new TreeViewItem();
                item.Header = new DirectoryInfo(this.txtRoot.Text).Name;
                item.Tag = Path.GetFullPath(this.txtRoot.Text);
                item.Items.Add(_dummyNode);
                item.Expanded += folder_Expanded;

                // Apply the attached property so that 
                // the triggers know that this is root item.
                TreeViewItemProps.SetIsRootLevel(item, true);
                foldersTree.Items.Add(item);
            }
        }

        void ExplorerTree_Loaded(object sender, RoutedEventArgs e)
        {
            if (foldersTree.Items.Count > 0)
            {
                return;
            }
            string mydoc = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string root = "SvcPerf";
            this.txtRoot.Text = Path.Combine(mydoc, root);
        }

        void folder_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)sender;
            if (item.Items.Count == 1 && item.Items[0] == _dummyNode)
            {
                item.Items.Clear();
                try
                {
                    foreach (string dir in Directory.GetDirectories(item.Tag as string))
                    {
                        AddDirectory(item, dir);
                    }

                    foreach (var filename in Directory.GetFiles(item.Tag as string, "*.linq"))
                    {
                        AddFile(item, filename);
                    }
                }
                catch (Exception) { }
            }
        }

        private void AddFile(TreeViewItem item, string filename)
        {
            var fileItem = new TreeViewItem();
            fileItem.Header = new FileInfo(filename).Name;
            fileItem.Tag = filename;
            TreeViewItemProps.SetIsFile(fileItem, true);
            fileItem.MouseDoubleClick += (s, args) =>
            {
                if (this.FileSelected != null)
                {
                    this.FileSelected(fileItem, new StringEventArgs(filename));
                }
            };
            fileItem.PreviewKeyDown += (s, args) =>
            {
                if (args.Key == System.Windows.Input.Key.Enter)
                {
                    if (this.FileSelected != null)
                    {
                        this.FileSelected(fileItem, new StringEventArgs(filename));
                    }
                    args.Handled = true;
                }
            };
            item.Items.Add(fileItem);
        }

        private void AddDirectory(TreeViewItem node, string dir)
        {
            TreeViewItem subitem = new TreeViewItem();
            subitem.Header = new DirectoryInfo(dir).Name;
            subitem.Tag = dir;
            subitem.Items.Add(_dummyNode);
            subitem.Expanded += folder_Expanded;
            node.Items.Add(subitem);
        }

        private void btnDirectory_Click_1(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = this.txtRoot.Text;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.txtRoot.Text = dialog.SelectedPath;
            }

        }

        private void ClosePanel_Click_1(object sender, RoutedEventArgs e)
        {
            this.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void Refresh_Click_1(object sender, RoutedEventArgs e)
        {
            var refreshRoot = this.txtRoot.Text;
            this.txtRoot.Text = null;
            this.txtRoot.Text = refreshRoot;
        }
    }

    public static class TreeViewItemProps
    {
        public static bool GetIsRootLevel(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsRootLevelProperty);
        }

        public static void SetIsRootLevel(
            DependencyObject obj, bool value)
        {
            obj.SetValue(IsRootLevelProperty, value);
        }

        public static readonly DependencyProperty IsRootLevelProperty =
            DependencyProperty.RegisterAttached(
            "IsRootLevel",
            typeof(bool),
            typeof(TreeViewItemProps),
            new UIPropertyMetadata(false));

        public static bool GetIsFile(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsFileProperty);
        }

        public static void SetIsFile(
            DependencyObject obj, bool value)
        {
            obj.SetValue(IsFileProperty, value);
        }

        public static readonly DependencyProperty IsFileProperty =
            DependencyProperty.RegisterAttached(
            "IsFile",
            typeof(bool),
            typeof(TreeViewItemProps),
            new UIPropertyMetadata(false));
    }

}
