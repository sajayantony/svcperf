namespace EtlViewer.Viewer.Models
{
    using EtlViewer.QueryFx;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Input;
    
    public enum FilterMode
    {
        Source,
        View,
        Search
    }

    class FilterModel : DependencyObject, INotifyPropertyChanged
    {
        public ObservableCollection<CheckedItem<KeywordDefinition>> Keywords { get; set; }
        public ObservableCollection<CheckedItem<Provider>> Providers { get; set; }
        public DelegateCommand<object> SelectProviderCommand { get; set; }

        public FilterModel()
        {
            this.FilterCommand = new StringDelegateCommand();
            this.SelectProviderCommand = new DelegateCommand<object>();

            this.SelectProviderCommand.ExecuteTargets += SelectProviderCommand_ExecuteTargets;
            this.SelectProviderCommand.CanExecuteTargets += () => { return this.Providers.Count > 1; };

            this.Providers = new ObservableCollection<CheckedItem<Provider>>();
            this.Providers.Add(new CheckedItem<Provider>()
            {
                IsChecked = true,
                Name = "Any",
                Item = new Provider()
                {
                    Name = "Any",
                    Id = Guid.Empty
                }
            });

            this.Providers[0].PropertyChanged += (s, e) =>
            {
                bool isChecked = this.Providers[0].IsChecked;
                for (int i = 1; i < this.Providers.Count; i++)
                {
                    this.Providers[i].IsChecked = isChecked;
                }
            };

            this.Keywords = new ObservableCollection<CheckedItem<KeywordDefinition>>();
            this.Keywords.Add(new CheckedItem<KeywordDefinition>() { 
                 IsChecked = true,
                 Name = KeywordDefinition.All.Name,
                 Item = KeywordDefinition.All
            });
        }

        #region Dependency properties
        public static readonly DependencyProperty
            FilterCommandProperty = DependencyProperty.Register("FilterCommand",
            typeof(StringDelegateCommand),
            typeof(FilterModel));

        public StringDelegateCommand FilterCommand
        {
            get { return (StringDelegateCommand)GetValue(FilterCommandProperty); }
            set { SetValue(FilterCommandProperty, value); }
        }

        public const string FilterExceptionPropertyName = "FilterException";
        public static readonly DependencyProperty
                FilterExceptionProperty = DependencyProperty.Register(FilterExceptionPropertyName,
                typeof(Exception),
                typeof(FilterModel),
                new PropertyMetadata(OnPropertyChanaged));

        public const string ResolverPropertyName = "Resolver";
        public static readonly DependencyProperty ResolverProperty = DependencyProperty.Register(
            ResolverPropertyName,
            typeof(Resolver),
            typeof(FilterModel),
            new FrameworkPropertyMetadata(OnPropertyChanaged));

        public Resolver Resolver
        {
            get { return (Resolver)this.GetValue(ResolverProperty); }
            set { this.SetValue(ResolverProperty, value); }
        }

        public const string FilterTextPropertyName = "FilterText";
        public static readonly DependencyProperty FilterTextProperty = DependencyProperty.Register(
          FilterTextPropertyName,
          typeof(string),
          typeof(FilterModel),
          new FrameworkPropertyMetadata(null,
              FrameworkPropertyMetadataOptions.AffectsRender,
              new PropertyChangedCallback(OnPropertyChanaged)
          )
        );

        public string FilterText
        {
            get { return (string)GetValue(FilterTextProperty) ?? string.Empty; }
            set { SetValue(FilterTextProperty, value); }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        public const string ModePropertyName = "Mode";
        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
          ModePropertyName,
          typeof(FilterMode),
          typeof(FilterModel),
          new FrameworkPropertyMetadata(OnPropertyChanaged));

        public FilterMode Mode
        {
            get { return (FilterMode)this.GetValue(ModeProperty); }
            set { this.SetValue(ModeProperty, value); }
        }

        static void OnPropertyChanaged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            FilterModel model = sender as FilterModel;
            string propertyName = e.Property.Name;
            model.OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (propertyName == ModePropertyName)
            {
                this.FilterException = null;
            }
            else if (propertyName == ResolverPropertyName)
            {
                this.LoadKeywords(this.Resolver);
            }

            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public Exception FilterException
        {
            get { return (Exception)GetValue(FilterExceptionProperty); }
            set { SetValue(FilterExceptionProperty, value); }
        }
        #endregion Dependency Properties

        public Guid[] SeletectedProviders
        {
            get
            {
                // If any is selected then don't filter
                if (!this.Providers[0].IsChecked)
                {
                    return this.Providers.Where((s) => s.IsChecked && s.Item.Id != Guid.Empty).Select((x) => x.Item.Id).ToArray();
                }

                return null;
            }
        }
        
        public ulong SelectedKeywords
        {
            get
            {
                if (this.Keywords.Count == 0)
                {
                    return KeywordDefinition.All.Mask;
                }

                ulong mask = 0;
                foreach (var keyword in this.Keywords)
                {
                    if (keyword.IsChecked)
                    {
                        mask = mask | keyword.Item.Mask;
                    }
                }
                return mask;
            }
        }
       
        #region Grid FilterSupported Fields

        public IEnumerable<string> SupportedFields
        {
            get { return _supportedFields; }
        }

        static string[] _supportedFields = new string[]
                    {
                        "Id",
                        "Tid",
                        "ActivityId",
                        "RelatedActivityId",
                        "Pid",
                        "Level",                        
                        "RootActivityId"
                    };

        #endregion

        void SelectProviderCommand_ExecuteTargets(object obj)
        {
            CheckedItem<Provider> provider = obj as CheckedItem<Provider>;
            if (provider != null && provider.Item.Id != Guid.Empty)
            {
                this.Resolver = provider.Item.Resolver;
            }
        }

        public void AddProviders(IEnumerable<Resolver> resolvers)
        {
            if (resolvers == null)
            {
                return;
            }

            var checkboxes = (from resolver in resolvers
                              select new CheckedItem<Provider>
                              {
                                  Item = new Provider
                                  {
                                      Id = resolver.ProviderId,
                                      Name = resolver.ProviderName,
                                      Resolver = resolver
                                  },
                                  Name = resolver.ProviderName,
                                  IsChecked = true
                              });

            // Add providers that haven't already been added
            foreach (var item in checkboxes)
            {
                if (this.Providers.Count((e) => e.Item.Id == item.Item.Id) == 0)
                {
                    this.Providers.Add(item);
                }
            }

            if (this.Providers.Count > 1)
            {
                this.Resolver = this.Providers[this.Providers.Count - 1].Item.Resolver;
            }
        }

        internal string UpdateReaderFilters(TxReader reader)
        {
            string filter = this.FilterText.Trim();
            Contract.Assert(reader != null);
            reader.SetEnabledProviders(this.SeletectedProviders);
            reader.SetKeywordFilter(this.SelectedKeywords);
            reader.SetWhereFilter(filter);            
            return this.GetFilterString(reader);
        }

        string GetFilterString(TxReader reader)
        {
            StringBuilder sb = new StringBuilder();

            if (reader.ShouldFilterByWhere)
            {
                sb.Append(reader.WhereFilter);
            }

            if (reader.ShouldFilterTimeWindow)
            {
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                }
                sb.Append("From:" + new DateTime(reader.StartTime))
                  .Append(" To:" + new DateTime(reader.EndTime));
            }

            if (reader.ShouldFilterProviders)
            {
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                }
                sb.Append("Providers:")
                  .AppendLine(reader.EnabledProviders.Select((e) => e.ToString()).Aggregate((curr, next) => curr + "," + next));
            }

            //TODO: Add other filters.
            return sb.ToString();
        }    

        void LoadKeywords(Resolver resolver)
        {
            this.Keywords.Clear();
            this.Keywords.Add(new CheckedItem<KeywordDefinition>()
            {
                Name = "All",
                Item = new KeywordDefinition()
                {
                    Name = "All",
                    Mask = ulong.MaxValue
                },
                IsChecked = true,
            });

            this.Keywords[0].PropertyChanged += (s, e) =>
            {
                bool isChecked = this.Keywords[0].IsChecked;
                foreach (var item in this.Keywords)
                {
                    if (item.Item.Mask != KeywordDefinition.All.Mask)
                    {
                        item.IsChecked = isChecked;
                    }
                }
            };

            if (resolver != null)
            {
                var items = (from p in resolver.Keywords
                             orderby p.Mask
                             select new CheckedItem<KeywordDefinition>
                             {
                                 Item = p,
                                 IsChecked = true,
                             }).ToList();

                foreach (var keyword in items)
                {
                    this.Keywords.Add(keyword);
                }
            }
        }
    }

    class StringDelegateCommand : ICommand
    {
        Action<string> m_ExecuteTargets = delegate { };
        Func<bool> m_CanExecuteTargets = delegate { return false; };
        bool m_Enabled = false;

        #region ICommand Members

        public bool CanExecute(object parameter)
        {
            Delegate[] targets = m_CanExecuteTargets.GetInvocationList();
            foreach (Func<bool> target in targets)
            {
                m_Enabled = false;
                bool localenable = target.Invoke();
                if (localenable)
                {
                    m_Enabled = true;
                    break;
                }
            }
            return m_Enabled;
        }

        public void Execute(object parameter)
        {
            if (m_Enabled)
                m_ExecuteTargets(parameter != null ? parameter.ToString() : null);
        }

        public event EventHandler CanExecuteChanged = delegate { };

        #endregion

        public event Action<string> ExecuteTargets
        {
            add
            {
                m_ExecuteTargets += value;
            }
            remove
            {
                m_ExecuteTargets -= value;
            }
        }

        public event Func<bool> CanExecuteTargets
        {
            add
            {
                m_CanExecuteTargets += value;
                CanExecuteChanged(this, EventArgs.Empty);
            }
            remove
            {
                m_CanExecuteTargets -= value;
                CanExecuteChanged(this, EventArgs.Empty);
            }
        }
    }
}
