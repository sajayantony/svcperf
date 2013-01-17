namespace EtlViewer.Viewer.Controls
{
    using EtlViewer.Viewer.Models;
    using System;
    using System.Collections.ObjectModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media.Imaging;
    using System.Windows.Threading;
    using System.Linq;
    /// <summary>
    /// Interaction logic for FilterBox.xaml
    /// </summary>
    partial class FilterBox : UserControl
    {
        DispatcherTimer _popupTimer;
        FilterModel Model;

        public FilterBox()
        {
            InitializeComponent();
            this.GotFocus += FilterBox_GotFocus;
            this.txtFilterText.LostFocus += txtFilterText_LostFocus;
            this.DataContextChanged += FilterBox_DataContextChanged;
            this.lstFields.KeyUp += (s, e) =>
                {
                    this.HandleFieldKeyDown(s, e, this.popupFields);
                };
        }

        void txtFilterText_LostFocus(object sender, RoutedEventArgs e)
        {
            this.Model.FilterText = this.txtFilterText.Text;
        }

        void FilterBox_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is FilterModel)
            {
                this.Model = e.NewValue as FilterModel;
                this.Model.FilterCommand.CanExecuteTargets += () => true;
                Model.FilterCommand.ExecuteTargets += ExecuteHandler;
                this.SetupValidationRules();
                Model.PropertyChanged += Model_PropertyChanged;
            }
        }

        void Model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == FilterModel.FilterExceptionPropertyName)
            {
                if (!this.txtFilterText.BindingGroup.ValidateWithoutUpdate())
                {
                    this.txtFilterText.Focus();
                }
            }
            else if (e.PropertyName == FilterModel.ModePropertyName)
            {
                this.txtFilterText.Text = HistoryItems.Latest(this.Model.Mode).Term;
                this.txtFilterText.SelectAll();
            }
        }

        private void SetupValidationRules()
        {
            BindingGroup group = new BindingGroup();
            group.ValidationRules.Add(new FilterValidation(this.Model));
            this.txtFilterText.BindingGroup = group;
        }

        private void ExecuteHandler(string obj)
        {
            HistoryItems.Add(this.Model.FilterText, this.Model.Mode);
        }

        void FilterBox_GotFocus(object sender, RoutedEventArgs e)
        {
            DependencyObject v = (DependencyObject)FocusManager.GetFocusedElement(this);
            if (v == null)
            {
                FocusManager.SetFocusedElement(this, txtFilterText);
            }
        }

        private void OnFilterTextKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            bool isValid = this.ValidateOnFilter();
            TextBox txtBox = sender as TextBox;
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Space)
            {
                txtBox.Undo(); // Undo the space typed.   
                this.ShowFieldsPopup();
                e.Handled = true;
            }
            else if (e.Key == Key.Down && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ShowHistoryPopup();
                e.Handled = true;
            }
            else
            {
                base.OnKeyUp(e);
            }
        }

        void InitializePopupTimer()
        {
            if (this._popupTimer == null)
            {
                this._popupTimer = new DispatcherTimer(DispatcherPriority.Normal);
                _popupTimer.Interval = TimeSpan.FromMilliseconds(1000);
                _popupTimer.Tick += (obj, e) =>
                {
                    this._popupTimer.Stop();

                    //Show the popup only if there is a valid expression or if there is a trailing space
                    string value = this.txtFilterText.Text;
                    if (this.ValidateOnFilter() && value.Length > 0 && value[value.Length - 1] == ' ')
                    {
                        ShowFieldsPopup();
                    }
                };

                this.popupFields.Closed += (s, e) =>
                {
                    this._popupTimer.Stop();
                };
            }

            //Restart the timer
            if (_popupTimer.IsEnabled)
            {
                _popupTimer.Stop();
            }

            if (popupFields.IsOpen == false)
            {
                _popupTimer.Interval = TimeSpan.FromMilliseconds(1000);
                _popupTimer.Start();
            }
        }

        private void ShowFieldsPopup()
        {
            Rect txtRect = this.txtFilterText.GetRectFromCharacterIndex(this.txtFilterText.CaretIndex, true);
            //Position it immediately after the character

            //TODO:Handle when the rect goes out of the window
            Rect r = new Rect(new Point(txtRect.TopLeft.X + lstFields.Width, txtRect.TopLeft.Y - 2),
                              new Point(txtRect.BottomLeft.X, txtRect.BottomLeft.Y - 2));
            popupFields.PlacementRectangle = r;
            popupFields.IsOpen = true;
            lstFields.SelectedIndex = 0;
            lstFields.Focus();
        }

        void ShowHistoryPopup()
        {
            if (HistoryItems.history.Count == 0)
            {
                return;
            }

            lstHistory.SelectedIndex = 0;
            popupHistory.IsOpen = true;
            lstHistory.Focus();
        }

        void lstHistorySelection_KeyDown(object sender, KeyEventArgs e)
        {
            Popup popup = this.popupHistory;
            HandleHistoryPopupKeyDown(sender, e, popup);
        }

        void HandleFieldKeyDown(object sender, KeyEventArgs e, Popup popup)
        {
            switch (e.Key)
            {
                case Key.Enter:
                case Key.Tab:
                    ListBox lb = sender as ListBox;
                    if (lb == null)
                        return;
                    SetSelectedField(lb, popup);
                    e.Handled = true;
                    break;
            }
        }

        void SetSelectedField(ListBox lb, Popup parent)
        {
            parent.IsOpen = false;
            // Get the selected item value
            string insertItem = lb.SelectedItem.ToString() + "=";

            // Save the Caret position
            int i = txtFilterText.CaretIndex;

            txtFilterText.Text = txtFilterText.Text.Insert(i, insertItem);

            // Move the caret to the end of the added text
            txtFilterText.CaretIndex = i + insertItem.Length;

            // Move focus back to the text box. 
            // This will auto-hide the PopUp due to StaysOpen="false"
            this.txtFilterText.Focus();
        }

        void HandleHistoryPopupKeyDown(object sender, KeyEventArgs e, Popup popup)
        {
            switch (e.Key)
            {
                case Key.Enter:
                case Key.Tab:
                    ListBox lb = sender as ListBox;
                    if (lb == null)
                        return;

                    this.SetSelectionFromHistory(lb, popup);
                    break;
                case System.Windows.Input.Key.Escape:
                    // Hide the Popup
                    popup.IsOpen = false;
                    Keyboard.Focus(this.txtFilterText);
                    this.txtFilterText.Focus();
                    break;
            }
        }

        void SetSelectionFromHistory(ListBox lb, Popup parent)
        {
            parent.IsOpen = false;
            // Get the selected item value
            string insertItem = lb.SelectedItem.ToString();

            var item = HistoryItems.SetLatest(insertItem);

            // Save the Caret position
            int i = txtFilterText.CaretIndex;

            // Move the caret to the end of the added text
            this.txtFilterText.Text = item.Term;
            txtFilterText.CaretIndex = i + item.Term.Length;

            // Move focus back to the text box. 
            // This will auto-hide the PopUp due to StaysOpen="false"
            this.Focus();
        }

        private void lstFields_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Keyboard.Focus(this.txtFilterText);
        }

        private void lstFields_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ListBox listbox = sender as ListBox;
            if (listbox == null)
            {
                return;
            }

            if (e.Key == Key.Tab)
            {
                this.PopupClosing();
                e.Handled = true;
            }
            else if (e.Key == Key.Up || e.Key == Key.Down)
            {
                if (e.Key == Key.Up)
                {
                    if (listbox.SelectedIndex > 0)
                    {
                        listbox.SelectedIndex--;
                    }
                }
                else
                {
                    if (listbox.SelectedIndex < listbox.Items.Count - 1)
                    {
                        listbox.SelectedIndex++;
                    }
                }

                e.Handled = true;
            }
        }

        private void PopupClosing()
        {
            if (this.popupFields.IsOpen)
            {
                this.SetSelectedField(this.lstFields, this.popupFields);
            }
            else if (this.popupHistory.IsOpen)
            {
                this.SetSelectedField(this.lstHistory, this.popupHistory);
            }
        }

        private void BtnShowDropDown_Click(object sender, RoutedEventArgs e)
        {
            this.ShowHistoryPopup();
        }

        private void lstItemClick(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem item = sender as ListBoxItem;
            if (sender != null)
            {
                item.IsSelected = true;
            }
        }

        private void lstItemClick(object sender, MouseEventArgs e)
        {
            ListBoxItem item = sender as ListBoxItem;
            if (sender != null)
            {
                item.IsSelected = true;
            }
        }

        private void lstHistory_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.SetSelectionFromHistory(this.lstHistory, this.popupHistory);
        }

        private void lstFields_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.SetSelectedField(this.lstFields, this.popupFields);
        }

        private void txtFilterText_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox txtBox = sender as TextBox;

            if (e.Key == Key.F3 ||
                (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Shift))
            {
                // The TextChangedEvent fires only after loss of focus.
                this.Model.FilterText = this.txtFilterText.Text;
                return;
            }

            if (e.Key == Key.Enter || e.Key == Key.F5)
            {
                e.Handled = true;
                this.Model.FilterText = this.txtFilterText.Text;
                InvokeFilterEvent();
            }
        }

        private void InvokeFilterEvent()
        {
            this.Model.FilterCommand.Execute(null);
        }

        private bool ValidateOnFilter()
        {

            ////TODO: Fix exeptions and warnings during parsing
            ////FilterParserException exception = null;
            ////bool isValid = this.TryParse(out exception);
            //Exception exception = null;
            //bool isValid = true;
            //BindingExpression bindingExpression =
            //        BindingOperations.GetBindingExpression(this.txtFilterText, TextBox.TextProperty);

            //BindingExpressionBase bindingExpressionBase =
            //    BindingOperations.GetBindingExpressionBase(this.txtFilterText, TextBox.TextProperty);

            //if (!isValid)
            //{
            //    ValidationError validationError =
            //        new ValidationError(new ExceptionValidationRule(), bindingExpression);
            //    validationError.ErrorContent = exception != null ? exception.Message : "Error occured during parsing.";

            //    Validation.MarkInvalid(bindingExpressionBase, validationError);
            //}
            //else
            //{
            //    Validation.ClearInvalid(bindingExpression);
            //}

            //return isValid;

            return true;
        }

        private void SplitButton_ButtonClick_1(object sender, RoutedEventArgs e)
        {
            this.InvokeFilterEvent();
        }

        private void MenuItem_Click(Object sender, RoutedEventArgs e)
        {
            MenuItem menu = e.OriginalSource as MenuItem;
            if (menu != null)
            {
                this.Model.Mode = (FilterMode)Enum.Parse(typeof(FilterMode), menu.Header.ToString());
            }
        }
    }

    class HistoryItems
    {
        public static ObservableCollection<SearchItem> history = new ObservableCollection<SearchItem>();
        const int maxItems = 30;
        const int trimSize = 10;

        public object History
        {
            get
            {
                return history;
            }
        }

        internal static void Add(string filterText, FilterMode filterMode)
        {
            if (string.IsNullOrEmpty(filterText))
            {
                return;
            }

            //Try to trim the history.
            if (history.Count >= maxItems)
            {
                for (int i = history.Count - 1; i >= trimSize; i--)
                {
                    history.RemoveAt(i);
                }
            }

            var toRemove = history.Where(h => h.Mode == filterMode && h.Term == filterText).FirstOrDefault();
            if (toRemove != null)
            {
                history.Remove(toRemove);
            }

            history.Insert(0, new SearchItem { Mode = filterMode, Term = filterText });

        }

        internal class SearchItem
        {
            public string Term { get; set; }
            public FilterMode Mode { get; set; }
            public static SearchItem Empty = new SearchItem();

            public override string ToString()
            {
                return this.Mode.ToString() + ":" + this.Term;
            }
        }

        internal static SearchItem Latest(FilterMode filterMode)
        {
            var item = history.Where(h => h.Mode == filterMode).FirstOrDefault();
            return item ?? SearchItem.Empty;
        }

        internal static SearchItem SetLatest(string insertItem)
        {
            int separatorIndex = insertItem.IndexOf(":");
            if (separatorIndex > -1)
            {
                var mode = (FilterMode)Enum.Parse(typeof(FilterMode), insertItem.Substring(0, separatorIndex));
                var term = insertItem.Substring(separatorIndex + 1);
                var item = history.Where(h => h.Mode == mode && h.Term == term).FirstOrDefault();
                if (item != null)
                {
                    history.Remove(item);
                    history.Insert(0, item);
                    return item;
                }
            }

            return SearchItem.Empty;
        }
    }

    class FilterModeIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                string uri = null;
                switch ((FilterMode)value)
                {
                    case FilterMode.Source:
                        uri = "pack://application:,,,/Assets/images/play.png";
                        break;
                    case FilterMode.View:
                        uri = "pack://application:,,,/Assets/images/filter.png";
                        break;
                    case FilterMode.Search:
                        uri = "pack://application:,,,/Assets/images/search.png";
                        break;
                    default:
                        break;
                }
                img.UriSource = new Uri(uri);
                img.EndInit();
                return img;
            }
            catch (Exception ex)
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class FilterValidation : ValidationRule
    {
        FilterModel model;

        public FilterValidation(FilterModel model)
        {
            this.model = model;
        }
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            if (this.model.FilterException != null)
            {
                if (this.model.FilterException is AggregateException)
                {
                    string message = string.Empty;
                    foreach (var item in ((AggregateException)this.model.FilterException).InnerExceptions)
                    {
                        message += item.Message + "; ";
                    }

                    return new ValidationResult(false, message);
                }
                else
                {
                    return new ValidationResult(false, this.model.FilterException.Message);
                }
            }
            return new ValidationResult(true, "");
        }
    }
}
