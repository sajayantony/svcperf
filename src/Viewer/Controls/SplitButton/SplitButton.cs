namespace EtlViewer.Viewer.Controls
{
    using EtlViewer.Internal;
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;

    /// <summary>
    /// Represents a combination of a standard button on the left and a drop-down button on the right.
    /// </summary>
    [TemplatePartAttribute(Name = "PART_Popup", Type = typeof(Popup))]
    [TemplatePartAttribute(Name = "PART_Button", Type = typeof(Button))]
    class SplitButton : MenuItem
    {
        private Button splitButtonHeaderSite;

        /// <summary>
        /// Identifies the CornerRadius dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty;

        private static readonly RoutedEvent ButtonClickEvent;

        static SplitButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SplitButton), new FrameworkPropertyMetadata(typeof(SplitButton)));

            CornerRadiusProperty = Border.CornerRadiusProperty.AddOwner(typeof(SplitButton));

            IsSubmenuOpenProperty.OverrideMetadata(typeof(SplitButton),
                new FrameworkPropertyMetadata(
                    BooleanBoxes.FalseBox, 
                    new PropertyChangedCallback(OnIsSubmenuOpenChanged), 
                    new CoerceValueCallback(CoerceIsSubmenuOpen)));

            ButtonClickEvent = EventManager.RegisterRoutedEvent("ButtonClick", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(SplitButton));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(SplitButton), new FrameworkPropertyMetadata(KeyboardNavigationMode.Local));
            KeyboardNavigation.ControlTabNavigationProperty.OverrideMetadata(typeof(SplitButton), new FrameworkPropertyMetadata(KeyboardNavigationMode.None));
            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(typeof(SplitButton), new FrameworkPropertyMetadata(KeyboardNavigationMode.None));

            EventManager.RegisterClassHandler(typeof(SplitButton), MenuItem.ClickEvent, new RoutedEventHandler(OnMenuItemClick));
            EventManager.RegisterClassHandler(typeof(SplitButton), Mouse.MouseDownEvent, new MouseButtonEventHandler(OnMouseButtonDown), true);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            bool isPressed = this.IsPressed;
            if (e.Key == Key.Space || e.Key == Key.Enter)
            {
                this.OnButtonClick();
            }
            else if(e.Key == Key.Down)
            {
                this.IsSubmenuOpen = true;
            }
            base.OnKeyDown(e);
        }

        /// <summary>
        /// Gets or sets a value that represents the degree to which the corners of a <see cref="SplitButton"/> are rounded.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        /// <summary>
        /// Occurs when the button portion of a <see cref="SplitButton"/> is clicked.
        /// </summary>
        public event RoutedEventHandler ButtonClick
        {
            add { base.AddHandler(ButtonClickEvent, value); }
            remove { base.RemoveHandler(ButtonClickEvent, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            splitButtonHeaderSite = this.GetTemplateChild("PART_Button") as Button;
            if (splitButtonHeaderSite != null)
            {
                splitButtonHeaderSite.Click += OnHeaderButtonClick;
            }
            this.GotKeyboardFocus += SplitButton_GotKeyboardFocus;
        }

        void SplitButton_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            
        }

        private void OnHeaderButtonClick(Object sender, RoutedEventArgs e)
        {
            this.IsSubmenuOpen = false;
            OnButtonClick(); 
        }

        protected virtual void OnButtonClick()
        {
            base.RaiseEvent(new RoutedEventArgs(ButtonClickEvent, this));
            if (Command != null)
            {
                this.Command.Execute(null);
            }
        }

        private static void OnIsSubmenuOpenChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            SplitButton splitButton = sender as SplitButton;
            if ((Boolean)e.NewValue)
            {
                if (Mouse.Captured != splitButton)
                {
                    Mouse.Capture(splitButton, CaptureMode.SubTree);          
                }
            }
            else
            {
                if (Mouse.Captured == splitButton)
                {
                    Mouse.Capture(null);
                }

                if (splitButton.IsKeyboardFocused)
                {
                    splitButton.Focus();
                }
            }
        }

        /// <summary>
        /// Set the IsSubmenuOpen property value at the right time.
        /// </summary>
        private static Object CoerceIsSubmenuOpen(DependencyObject element, Object value)
        {
            SplitButton splitButton = element as SplitButton;
            if ((Boolean)value)
            {
                if (!splitButton.IsLoaded)
                {
                    splitButton.Loaded += delegate(Object sender, RoutedEventArgs e)
                    {
                        splitButton.CoerceValue(IsSubmenuOpenProperty);
                    };

                    return BooleanBoxes.FalseBox;
                }
            }

            return (Boolean)value && splitButton.HasItems;
        }

        private static void OnMenuItemClick(Object sender, RoutedEventArgs e)
        {
            SplitButton splitButton = sender as SplitButton;
            MenuItem menuItem = e.OriginalSource as MenuItem;

            // To make the ButtonClickEvent get fired as we expected, you should mark the ClickEvent 
            // as handled to prevent the event from poping up to the button portion of the SplitButton.
            if (menuItem != null && !typeof(MenuItem).IsAssignableFrom(menuItem.Parent.GetType()))
            {
                e.Handled = true;
            }

            if (menuItem != null && splitButton != null)
            {
                if (splitButton.IsSubmenuOpen)
                {
                    splitButton.CloseSubmenu();
                }
            }

        }

        private static void OnMouseButtonDown(Object sender, MouseButtonEventArgs e)
        {
            SplitButton splitButton = sender as SplitButton;
            if (!splitButton.IsKeyboardFocusWithin)
            {
                splitButton.Focus();
                return;
            }

            if (Mouse.Captured == splitButton && e.OriginalSource == splitButton)
            {
                splitButton.CloseSubmenu();
                return;
            }

            if (e.Source is MenuItem)
            {
                MenuItem menuItem = e.Source as MenuItem;
                if (menuItem != null)
                {
                    if (!menuItem.HasItems)
                    {
                        splitButton.CloseSubmenu();
                        menuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, menuItem));
                    }
                }
            }
        }

        private void CloseSubmenu()
        {
            if (this.IsSubmenuOpen)
            {
                ClearValue(SplitButton.IsSubmenuOpenProperty);
                if (this.IsSubmenuOpen)
                {
                    this.IsSubmenuOpen = false;
                }
            }
        }
    }
}
