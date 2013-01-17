namespace EtlViewer.QueryFx
{    
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using Tx.Windows;

    static class Constants
    {
        public const string Provider_wcf_wf = "{c651f5f6-1c0d-492e-8ae1-b4efd7c9d503}";
        public const string Provider_wcf_wf_Alias = "WCF/WF";
        public const string Provider_Name = "Microsoft-Windows-Application Server-Applications";
    }

    class ManifestParser
    {
        XElement _root;
        XElement _instrumentation;
        XElement _events;
        IEnumerable<XElement> _providers;
        XElement _stringTable;

        //public string GetTemplate(int id)
        //{
        //    EnsureSymbols();
        //    if (symbols.ContainsKey(id))
        //    {
        //        return symbols[id].Message;
        //    }

        //    return string.Empty;
        //}

        public IEnumerable<Resolver> Parse(string manifestFile)
        {
            string manifest = File.ReadAllText(manifestFile);

            XElement localization;
            XElement resources;

            _root = XElement.Parse(manifest);
            _instrumentation = _root.Element(ElementNames.Instrumentation);
            if (_instrumentation == null)
            {
                _instrumentation = _root.Element(ElementNames.Instrumentation1);
                localization = _root.Element(ElementNames.Localization1);
                resources = localization.Element(ElementNames.Resources1);
                _stringTable = resources.Element(ElementNames.StringTable1);
            }
            else
            {
                localization = _root.Element(ElementNames.Localization);
                if (localization != null)
                {
                    resources = localization.Element(ElementNames.Resources);
                    _stringTable = resources.Element(ElementNames.StringTable);
                }
            }
            _events = _instrumentation.Element(ElementNames.Events);
            if (_events != null)
            {
                _providers = _events.Elements(ElementNames.Provider);
                if (_providers != null)
                {
                    foreach (XElement provider in _providers)
                    {
                        // Itis unusual that the source attribute is missing. I send mail to Vance
                        string source = provider.Attribute(AttributeNames.Source) == null ?
                            "Xml" : provider.Attribute(AttributeNames.Source).Value;

                        switch (source)
                        {
                            case "Xml":
                                yield return ParseManifestProvider(provider);
                                break;

                            case "Wbem":
                                yield return ParseClassicProvider(provider);
                                break;

                            default:
                                throw new Exception(
                                    String.Format("unknown source attribute {0} for provider {1}. The expexted values are Xml and Wbem",
                                        source,
                                        provider.Attribute(AttributeNames.Name).Value));
                        }
                    }
                }
            }
        }

        Resolver ParseManifestProvider(XElement provider)
        {
            Resolver resolver;
            string providerName = MakeIdentifier(provider.Attribute(AttributeNames.Name).Value);
            string providerGuid = provider.Attribute(AttributeNames.Guid).Value;

            GetEarliestVersions(provider);
            var nameFunction = FindNameFunction(provider);

            XElement events = provider.Element(ElementNames.Events);
            XElement keywords = provider.Element(ElementNames.Keywords);
            XElement tasks = provider.Element(ElementNames.Tasks);
            IEnumerable<XElement> opcodes = provider.Descendants(ElementNames.Opcodes);
            XElement templates = provider.Element(ElementNames.Templates);
            XElement messages = this._stringTable;

            resolver = CreateResolver(true,
                providerName,
                providerGuid,
                events,
                keywords,
                tasks,
                opcodes,
                messages);

            return resolver;
        }

        private Resolver CreateResolver(bool crimson,
            string providerName,
            string providerGuid,
            XElement events,
            XElement keywords,
            XElement tasks,
            IEnumerable<XElement> opcodes,
            XElement messages)
        {
            Resolver resolver;
            resolver = new CrimsonSymbolResolver();
            resolver.ProviderName = providerName;
            resolver.ProviderId = Guid.Parse(providerGuid);

            if (messages != null)
            {
                var m = from t in messages.Elements()
                        select new MessagDefinition
                        {
                            Id = (string)t.Attribute(AttributeNames.Id),
                            Message = t.Attribute(AttributeNames.Value).Value
                        };
                resolver.Messages = m.ToList();
            }

            resolver.Symbols = this.GetEventsFromCompiledManifests(resolver.ProviderId);

            Func<XElement, string> findMessage = (t) =>
                {
                    string msg = null;
                    string message = t.Attribute(AttributeNames.Message) != null ? t.Attribute(AttributeNames.Message).Value : null;
                    if (!String.IsNullOrEmpty(message) && message.Length > 9)
                    {
                        var stringId = message.Substring(9)   // skip "$(string."
                                      .TrimEnd(')');
                        msg = LookupResourceString(stringId);
                    }
                    else
                    {
                        msg = t.Attribute(AttributeNames.Name) != null ? t.Attribute(AttributeNames.Name).Value : null;
                        if (String.IsNullOrEmpty(msg) && t.Name != null)
                        {
                            msg = t.Name.LocalName + "_" + IntAttribute(t, AttributeNames.Value);
                        }
                    }

                    return msg ?? string.Empty;
                };

            // Populate tasks
            if (tasks != null)
            {
                var tq = from t in tasks.Elements()
                         select new TaskDefinition
                         {
                             Id = IntAttribute(t, AttributeNames.Value),
                             Name = findMessage(t)
                         };

                resolver.Tasks = tq.ToList();
            }

            // Populate keywords
            if (keywords != null)
            {
                Func<XElement, string> LookupKeywordName = (t) =>
                    {
                        if (t.Attribute(AttributeNames.Symbol) != null && t.Attribute(AttributeNames.Symbol).Value != null)
                        {
                            return t.Attribute(AttributeNames.Symbol).Value.ToString();
                        }

                        return findMessage(t);
                    };

                var kv = from t in keywords.Elements()
                         let mask = ulong.Parse(t.Attribute("mask").Value.Substring(2), System.Globalization.NumberStyles.AllowHexSpecifier)
                         select new KeywordDefinition
                         {
                             Name = LookupKeywordName(t),
                             Mask = mask
                         };
                resolver.Keywords = kv.ToList();
            }

            // Populate opcodes
            if (opcodes != null)
            {
                var op = from o in opcodes.Elements()
                         select new OpcodeDefinition
                         {
                             Id = IntAttribute(o, AttributeNames.Value),
                             Name = findMessage(o)
                         };
                resolver.Opcodes = op.ToList();
            }



            if (resolver.Symbols == null)
                resolver.Symbols = new List<EventDefinition>();
            if (resolver.Keywords == null)
                resolver.Keywords = new List<KeywordDefinition>();
            if (resolver.Tasks == null)
                resolver.Tasks = new List<TaskDefinition>();
            if (resolver.Opcodes == null)
                resolver.Opcodes = new List<OpcodeDefinition>();
            if (resolver.Messages == null)
                resolver.Messages = new List<MessagDefinition>();
            return resolver;
        }

        private IEnumerable<EventDefinition> GetEventsFromCompiledManifests(Guid provider)
        {
            var q = from s in ManifestCompiler.GetKnowntypesforPlayback()
                    let attr = (ManifestEventAttribute)s.GetCustomAttributes(true).Where((e) => e is ManifestEventAttribute).FirstOrDefault()
                    where attr != null && attr.ProviderGuid == provider
                    select new EventDefinition
                    {
                        Id = attr.EventId,
                        Name = s.Name
                    };

            return q.ToList();
        }


        private Resolver ParseClassicProvider(XElement provider)
        {
            string providerName = MakeIdentifier(provider.Attribute(AttributeNames.Name).Value);
            string providerGuid = provider.Attribute(AttributeNames.Guid).Value;
            XElement templates = provider.Element(ElementNames.Templates);

            GetEarliestVersions(provider);
            var nameFunction = FindNameFunction(provider);

            XElement events = provider.Element(ElementNames.Events);
            XElement tasks = provider.Element(ElementNames.Tasks);
            IEnumerable<XElement> opcodes = provider.Descendants(ElementNames.Opcodes);
            XElement messages = this._stringTable;

            Resolver resolver = new CrimsonSymbolResolver();
            resolver.ProviderName = providerName;
            resolver.ProviderId = Guid.Parse(providerGuid);

            resolver = this.CreateResolver(
                                false,
                                providerName,
                                providerGuid,
                                events,
                                null, //MOF doesn't have keywords. 
                                tasks,
                                opcodes,
                                messages);

            return resolver;
        }

        string MakeIdentifier(string name)
        {
            // I stumbled on case of using field name like "load/unload"...
            char[] chars = name.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]))
                {
                    chars[i] = '_';
                }
            }
            return new string(chars);
        }

        string VersionSuffix(XElement evt)
        {
            if (evt.Attribute(AttributeNames.Version) == null)
                return "";

            int id = IntAttribute(evt, AttributeNames.Value);
            int version = IntAttribute(evt, AttributeNames.Version);
            int earliestVersion = IntAttribute(_earliestVersions[id], AttributeNames.Version);

            if (version == earliestVersion)
                return "";

            return "_V" + version;
        }

        Dictionary<int, XElement> _earliestVersions;

        void GetEarliestVersions(XElement provider)
        {
            _earliestVersions = new Dictionary<int, XElement>();
            XElement events = provider.Element(ElementNames.Events);

            foreach (XElement evt in events.Elements())
            {
                int id = IntAttribute(evt, AttributeNames.Value);

                XElement other;
                if (!_earliestVersions.TryGetValue(id, out other))
                {
                    _earliestVersions.Add(id, evt);
                    continue;
                }

                int version = IntAttribute(evt, AttributeNames.Version);
                int earliestVersion = IntAttribute(other, AttributeNames.Version);
                if (version < earliestVersion)
                {
                    _earliestVersions[id] = evt;
                }
            }
        }

        int IntAttribute(XElement element, XName attributeName)
        {
            XAttribute attribute = element.Attribute(attributeName);
            if (attribute == null)
                return 0;

            string s = attribute.Value;
            if (s.StartsWith("0x"))
            {
                string v = s.Substring(2);
                return int.Parse(v, NumberStyles.AllowHexSpecifier);
            }
            else
            {
                return int.Parse(s);
            }
        }

        Func<XElement, string> FindNameFunction(XElement provider)
        {
            Func<XElement, string> function = e =>
                e.Attribute(AttributeNames.Symbol) != null ?
                    e.Attribute(AttributeNames.Symbol).Value
                    : null;

            var names = from e in _earliestVersions.Values select function(e);

            if (AreNamesUseful(names.ToArray()))
            {
                return function;
            }

            XElement opcodes = provider.Element(ElementNames.Opcodes);

            function = e => LookupOpcodeName(e, opcodes);

            names = from e in _earliestVersions.Values select function(e);
            if (AreNamesUseful(names.ToArray()))
            {
                return function;
            }

            XElement tasks = provider.Element(ElementNames.Tasks);

            function = e => LookupTaskName(e, tasks);

            names = from e in _earliestVersions.Values select function(e);
            if (AreNamesUseful(names.ToArray()))
            {
                return function;
            }

            function = e => e.Attribute(AttributeNames.Task) == null ?
                            null : e.Attribute(AttributeNames.Task).Value;

            names = from e in _earliestVersions.Values select function(e);
            if (AreNamesUseful(names.ToArray()))
            {
                return function;
            }

            function = e => LookupTaskName(e, tasks) + "_" +
                (e.Attribute(AttributeNames.Opcode) == null ?
                "" : e.Attribute(AttributeNames.Opcode).Value.Replace("win:", ""));
            names = from e in _earliestVersions.Values select function(e);
            if (AreNamesUseful(names.ToArray()))
            {
                return function;
            }

            function = e => LookupTaskName(e, tasks) + "_"
                + (e.Attribute(AttributeNames.Opcode) == null ?
                    "" : e.Attribute(AttributeNames.Opcode).Value.Replace("win:", ""))
                + "_" + e.Attribute(AttributeNames.Value).Value;

            names = from e in _earliestVersions.Values select function(e);
            if (AreNamesUseful(names.ToArray()))
            {
                return function;
            }

            // could not find useful heuristics
            // so, generate default names
            function = e => "Event_"
                + e.Attribute(AttributeNames.Value).Value
                + "_V" + e.Attribute(AttributeNames.Version).Value;

            return function;
        }

        bool AreNamesUseful(string[] names)
        {
            for (int index = 0; index < names.Length; index++)
            {
                string name = names[index];

                if (String.IsNullOrEmpty(name))
                {
                    return false;
                }

                // names must be valid identifiers
                if (!Regex.IsMatch(name, "^[A-Z_a-z][A-Z_a-z0-9]+"))
                {
                    return false;
                }

                // there should be no duplicate names
                for (int other = index + 1; other < names.Length; other++)
                {
                    if (name == names[other])
                    {
                        return false;
                    }
                }
            }

            return true;
        }


        private Guid GetProviderId(XDocument docman, string ManifestEventNs)
        {
            string guidPart = docman.Descendants(XName.Get("provider", ManifestEventNs)).FirstOrDefault().Attribute("guid").Value;
            if (String.IsNullOrEmpty(guidPart))
            {
                return Guid.Empty;
            }

            guidPart = guidPart.Substring(1, guidPart.Length - 2);
            return Guid.Parse(guidPart);
        }

        private string GetProviderName(XAttribute ProviderId)
        {
            if (ProviderId == null)
            {
                return string.Empty;
            }
            switch (ProviderId.Value)
            {
                case Constants.Provider_wcf_wf:
                    return Constants.Provider_wcf_wf_Alias;
            }

            return string.Empty;
        }

        string LookupOpcodeName(XElement evt, XElement opcodes)
        {
            if (opcodes == null)
                return null;

            var message = (from o in opcodes.Elements()
                           where
                              evt.Attribute(AttributeNames.Opcode) != null &&
                              evt.Attribute(AttributeNames.Opcode).Value == o.Attribute(AttributeNames.Name).Value
                           select o.Attribute(AttributeNames.Message).Value).FirstOrDefault();

            if (String.IsNullOrEmpty(message))
                return null;

            var stringId = message.Substring(9)   // skip "$(string."
                                  .TrimEnd(')');

            return LookupResourceString(stringId);
        }

        string LookupTaskName(XElement evt, XElement tasks)
        {
            if (tasks == null)
                return null;

            var message = (from t in tasks.Elements()
                           where
                               t.Attribute(AttributeNames.Message) != null &&
                               evt.Attribute(AttributeNames.Task) != null &&
                               evt.Attribute(AttributeNames.Task).Value == t.Attribute(AttributeNames.Name).Value
                           select t.Attribute(AttributeNames.Message).Value).FirstOrDefault();

            if (String.IsNullOrEmpty(message))
                return null;

            var stringId = message.Substring(9)   // skip "$(string."
                                  .TrimEnd(')');

            return LookupResourceString(stringId);
        }


        string LookupResourceString(string stringId)
        {
            if (this._stringTable == null)
            {
                return stringId;
            }

            return (from s in this._stringTable.Elements()
                    where s.Attribute(AttributeNames.Id).Value == stringId
                    select s.Attribute(AttributeNames.Value).Value)
                    .FirstOrDefault();
        }

        class CrimsonSymbolResolver : Resolver
        {
        }

        class ClassicSymbolResolver : Resolver
        {
        }

        class ElementNames
        {
            static readonly XNamespace ns1 = "urn:schemas-microsoft-com:asm.v3";
            public static readonly XName Instrumentation1 = ns1 + "instrumentation";
            public static readonly XName Localization1 = ns1 + "localization";
            public static readonly XName Resources1 = ns1 + "resources";
            public static readonly XName StringTable1 = ns1 + "stringTable";

            static readonly XNamespace ns = "http://schemas.microsoft.com/win/2004/08/events";
            public static readonly XName Instrumentation = ns + "instrumentation";
            public static readonly XName Provider = ns + "provider";
            public static readonly XName Events = ns + "events";
            public static readonly XName Keywords = ns + "keywords";
            public static readonly XName Tasks = ns + "tasks";
            public static readonly XName Templates = ns + "templates";
            public static readonly XName Opcodes = ns + "opcodes";
            public static readonly XName Localization = ns + "localization";
            public static readonly XName Resources = ns + "resources";
            public static readonly XName StringTable = ns + "stringTable";
            public static readonly XName Data = ns + "data";
        }

        class AttributeNames
        {
            public const string Source = "source";
            public const string Name = "name";
            public const string Guid = "guid";
            public const string Value = "value";
            public const string Symbol = "symbol";
            public const string Task = "task";
            public const string Template = "template";
            public const string Tid = "tid";
            public const string InType = "inType";
            public const string Version = "version";
            public const string Opcode = "opcode";
            public const string Id = "id";
            public const string Message = "message";
            public const string EventGuid = "eventGUID";
            public const string MofValue = "mofValue";
        }
    }
}
