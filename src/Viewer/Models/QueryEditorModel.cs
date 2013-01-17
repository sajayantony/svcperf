namespace EtlViewer.Viewer.Models
{
    using EtlViewer.QueryFx;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows;
    
    enum QueryModelState
    {
        Template,
        TemplateChanged,
        New,
        Opened,
        Changed,
    }

    class QueryEditorModel : DependencyObject, INotifyPropertyChanged
    {
        public DelegateCommand<object> BuildCommand { get; set; }
        public DelegateCommand<object> BuildStartCommand { get; set; }

        public string FileName
        {
            get { return (string)GetValue(FileNameProperty); }
            set { SetValue(FileNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FileName.  
        // This enables animation, styling, binding, etc...
        public const string FileNamePropertyName = "FileName";
        public static readonly DependencyProperty FileNameProperty =
            DependencyProperty.Register("FileName",
                                        typeof(string),
                                        typeof(QueryEditorModel),
                                        new PropertyMetadata(string.Empty, OnFileNameChanged));

        static void OnFileNameChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            QueryEditorModel source = sender as QueryEditorModel;
            string filename = args.NewValue as string;

            if (source != null && source.PropertyChanged != null)
            {
                source.PropertyChanged(sender, new PropertyChangedEventArgs(FileNamePropertyName));
            }
        }

        public bool HasChanges
        {
            get { return (bool)GetValue(HasChangesProperty); }
            set { SetValue(HasChangesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HasChanges.  
        // This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HasChangesProperty =
            DependencyProperty.Register("HasChanges",
                                        typeof(bool),
                                        typeof(QueryEditorModel),
                                        new PropertyMetadata(false,
                                            new PropertyChangedCallback(OnHasChangesChanged)));

        static void OnHasChangesChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            // Get reference to self
            QueryEditorModel source = (QueryEditorModel)sender;

            // Add Handling Code
            bool newValue = (bool)args.NewValue;

            if (newValue)
            {
                if (source.State != QueryModelState.Template)
                {
                    source.State = QueryModelState.Changed;
                }
                else if (source.State == QueryModelState.Template)
                {
                    source.State = QueryModelState.TemplateChanged;
                }
            }
        }

        public string Template
        {
            get { return (string)GetValue(TemplateProperty); }
            set { SetValue(TemplateProperty, value); }
        }

        internal Dictionary<string, ParameterResolver> ParameterResolvers { get; set; }
        // Using a DependencyProperty as the backing store for Template.  
        // This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TemplateProperty =
            DependencyProperty.Register("Template",
                                        typeof(string),
                                        typeof(QueryEditorModel),
                                        new PropertyMetadata(null,
                                            new PropertyChangedCallback(OnTemplateChanged)));

        static void OnTemplateChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            // Get reference to self
            QueryEditorModel source = (QueryEditorModel)sender;

            // Add Handling Code
            string newValue = (string)args.NewValue;
            if (!String.IsNullOrEmpty(newValue))
            {
                List<string> parameteres = QueryBuilder.GetParameters(newValue);
                if (parameteres.Count > 0)
                {
                    source.Parameters.Clear();
                    foreach (var item in parameteres.Distinct())
                    {
                        if (source.ParameterResolvers != null && source.ParameterResolvers.ContainsKey(item))
                        {
                            source.Parameters.Add(ParameterResolver.GetParameter(item, source.ParameterResolvers[item]));
                        }
                        else
                        {
                            source.Parameters.Add(ParameterResolver.GetParameter(item, null));
                        }
                    }

                    source.QueryString = newValue;
                    source.State = QueryModelState.Template;
                }
                else
                {
                    var fileContent = args.NewValue as string;
                    source.QueryString = fileContent;
                    source.Template = null;
                    source.State = QueryModelState.Opened;
                    source.HasChanges = false;
                }
            }
        }

        public QueryParameterCollection Parameters { get; private set; }

        public string Id { get; set; }

        public string QueryString
        {
            get { return (string)GetValue(QueryStringProperty); }
            set { SetValue(QueryStringProperty, value); }
        }

        // Using a DependencyProperty as the backing store for QueryString.  
        // This enables animation, styling, binding, etc...
        public const string QueryStringPropertyName = "QueryString";
        public static readonly DependencyProperty QueryStringProperty =
            DependencyProperty.Register(QueryStringPropertyName,
                                        typeof(string),
                                        typeof(QueryEditorModel),
                                        new PropertyMetadata(string.Empty,
                                            new PropertyChangedCallback(OnQueryStringChanged)));

        static void OnQueryStringChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            // Get reference to self
            QueryEditorModel source = (QueryEditorModel)sender;

            // Add Handling Code
            string newValue = (string)args.NewValue;
            if (source.State != QueryModelState.Template)
            {
                source.State = QueryModelState.Changed;
            }
        }

        public QueryEditorModel()
        {
            this.ParameterResolvers = new Dictionary<string, ParameterResolver>();
            this.Parameters = new QueryParameterCollection(new StringDictionary());
            this.BuildStartCommand = new DelegateCommand<object>();
            this.BuildCommand = new DelegateCommand<object>();
            this.State = QueryModelState.New;
            this.BuildCommand.CanExecuteTargets += () =>
            {
                return !String.IsNullOrEmpty(this.Template);
            };

            this.BuildCommand.ExecuteTargets += (o) =>
                {
                    if (this.BuildStartCommand.CanExecute(null))
                    {
                        this.BuildStartCommand.Execute(null);
                    }

                    if (!String.IsNullOrEmpty(this.Template))
                    {
                        this.QueryString = QueryBuilder.Replace(this.Template, this.Parameters);
                    }
                };
        }

        class QueryBuilder
        {
            public static readonly Regex parameterRegex = new Regex(@"\{([^\s\}]+)\}", RegexOptions.Compiled);

            public static string Replace(string input, QueryParameterCollection fields)
            {
                string output = parameterRegex.Replace(input, delegate(Match match)
                {
                    return fields[match.Groups[1].Value];
                });

                return output;
            }

            public static List<string> GetParameters(string template)
            {
                List<string> parameters = new List<string>();

                if (!String.IsNullOrEmpty(template))
                {
                    parameterRegex.Replace(template, delegate(Match match)
                    {
                        parameters.Add(match.Groups[1].Value);
                        return match.Groups[1].Value;
                    });
                }

                return parameters;
            }
        }


        public void LoadFile(string filename)
        {
            if (File.Exists(filename))
            {
                this.FileName = filename;
                StringWriter errorWriter = new StringWriter();
                string query = LinqpadHelpers.ExtractQuery(filename, errorWriter);
                if (string.IsNullOrEmpty(query))
                {
                    Logger.Log(errorWriter.ToString());
                }
                this.Template = LinqpadHelpers.ExtractQuery(filename, TextWriter.Null);
            }
        }

        internal void Save()
        {
            File.WriteAllText(this.FileName, this.QueryString);
            this.State = QueryModelState.Opened;
            this.HasChanges = false;
        }

        public QueryModelState State { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    class QueryParameterCollection : ObservableCollection<QueryParameter>
    {
        public string this[string key]
        {
            get
            {
                QueryParameter val = this.Where((e) => e.Name == key).FirstOrDefault();
                if (val != null)
                {
                    return val.ParameterValue == null ? string.Empty : val.ParameterValue.ToString();
                }

                return string.Empty;
            }
        }

        public QueryParameterCollection(StringDictionary values)
        {
            foreach (string key in values.Keys)
            {
                this.Add(key, values[key]);
            }
        }

        public void Add(string key, string value)
        {
            this.Add(new QueryParameter()
            {
                Name = key,
                ParameterValue = value
            });
        }
    }

    class QueryParameter : DependencyObject
    {
        public string Name { get; set; }

        public object ParameterValue
        {
            get { return (object)GetValue(ParameterValueProperty); }
            set { SetValue(ParameterValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ParameterValue.  
        // This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ParameterValueProperty =
            DependencyProperty.Register("ParameterValue",
                                        typeof(object),
                                        typeof(QueryParameter),
                                        new PropertyMetadata(string.Empty,
                                            new PropertyChangedCallback(OnParameterValueChanged)));

        static void OnParameterValueChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            // Get reference to self
            QueryParameter source = (QueryParameter)sender;

            // Add Handling Code
            string newValue = (string)args.NewValue;
        }

        public ObservableCollection<ParameterOption> Options
        {
            get { return (ObservableCollection<ParameterOption>)GetValue(OptionsProperty); }
            set { SetValue(OptionsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Options.  
        // This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OptionsProperty =
            DependencyProperty.Register("Options",
                                        typeof(ObservableCollection<ParameterOption>),
                                        typeof(QueryParameter),
                                        new PropertyMetadata(null,
                                            new PropertyChangedCallback(OnOptionsChanged)));

        static void OnOptionsChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            // Get reference to self
            QueryParameter source = (QueryParameter)sender;

            // Add Handling Code
            ObservableCollection<ParameterOption> newValue = (ObservableCollection<ParameterOption>)args.NewValue;
        }

        public bool HasOptions
        {
            get
            {
                return Options != null && Options.Count > 0;
            }
        }
    }

    class ParameterOption
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    class ParameterResolver : DependencyObject
    {
        public string Value
        {
            get;
            set;
        }

        public ObservableCollection<ParameterOption> Options
        {
            get;
            private set;
        }

        public ParameterResolver(object value)
        {
            this.Options = new ObservableCollection<ParameterOption>();
            if (value != null)
            {
                this.Value = value.ToString();
            }
        }

        public static QueryParameter GetParameter(string name, ParameterResolver resolver)
        {
            if (resolver == null)
            {
                if (name.ToLower().Contains("join"))
                {
                    JoinParameterResolver joinResolver = new JoinParameterResolver();
                    return new QueryParameter()
                    {
                        Name = name,
                        ParameterValue = joinResolver.Value,
                        Options = joinResolver.Options
                    };
                }
                else
                {
                    return new QueryParameter()
                    {
                        Name = name,
                        ParameterValue = "{" + name + "}",
                        Options = null,
                    };
                }
            }

            return new QueryParameter()
            {
                Name = name,
                ParameterValue = resolver.Value,
                Options = resolver.Options
            };
        }

        class JoinParameterResolver : ParameterResolver
        {
            public JoinParameterResolver()
                : base(JoinFields[0])
            {
                var q = JoinFields.Select((e) => new ParameterOption { Name = e, Value = e });
                this.Options = new ObservableCollection<ParameterOption>(q.ToList());
            }

            static string[] JoinFields = new string[] { 
                "Header.ActivityId",
                "Header.RelatedActivityId",
                "Header.ProcessId",
                "Header.ThreadId",            
            };
        }

    }

    class EventParameterResolver : ParameterResolver
    {
        public EventParameterResolver(string startEvent)
            : base(startEvent)
        {
        }
    }


}
