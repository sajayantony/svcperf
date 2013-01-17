namespace EtlViewer.Viewer.Parser
{
    using System;
    using System.Collections.Generic;

    public class FilterParser
    {
        public FilterParser(string condition)
        {
            this.Parse(condition);
        }

        void Parse(string conditions)
        {
            this.Context = new FilterParserContext(conditions);
            GetConditions(conditions, this.Context);
        }

        static void GetConditions(string filter, FilterParserContext context)
        {
            List<Guid> activities = new List<Guid>();
            List<uint> processes = new List<uint>();
            List<int> threads = new List<int>();
            List<int> ids = new List<int>();
            List<int> levels = new List<int>();
            List<string> texts = new List<string>();
            string normalizedFilterString = string.Empty;

            Token current = null;
            try
            {
                foreach (Token token in GetNextToken(filter))
                {
                    current = token;
                    switch (token.Type)
                    {
                        case TokenType.Id:
                            ids.Add((int)token.Value);
                            break;
                        case TokenType.Pid:
                            processes.Add((uint)token.Value);
                            break;
                        case TokenType.Tid:
                            threads.Add((int)token.Value);
                            break;
                        case TokenType.ActivityId:
                            activities.Add((Guid)token.Value);
                            break;
                        case TokenType.RootActivity:
                            context.RootActivityFilter = (Guid)token.Value;
                            break;
                        case TokenType.Level:
                            levels.Add((int)token.Value);
                            break;
                        case TokenType.MinLevel:
                            if (context.MinLevel != null)
                            {
                                throw new InvalidOperationException("Cannot have more than one MinValue clause");
                            }
                            context.MinLevel = (int)token.Value;
                            break;
                        case TokenType.Text:
                            texts.Add((string)token.Value);
                            break;
                        case TokenType.Where:
                            if (context.WhereFilter != null)
                            {
                                throw new FilterParserException("Cannot have more than one where clause",
                                                                current.Type.ToString(),
                                                                current.Index,
                                                                null);
                            }
                            context.WhereFilter = (string)token.Value;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is FilterParserException)
                {
                    throw;
                }

                if (current != null)
                {
                    throw new FilterParserException(current.Type.ToString(), current.Index, ex);
                }
                else
                {
                    throw;
                }
            }

            if (processes.Count > 0)
                context.ProcessFilters = processes.ToArray();
            if (threads.Count > 0)
                context.ThreadsFilter = threads.ToArray();
            if (ids.Count > 0)
                context.IdFilters = ids.ToArray();
            if (levels.Count > 0)
                context.LevelFilters = levels.ToArray();
            if (activities.Count > 0)
                context.ActivityIdFilters = activities.ToArray();
            if (texts.Count > 0)
                context.TextFilters = texts.ToArray();
        }

        static IEnumerable<Token> GetNextToken(string filter)
        {
            int filterIndex = 0;
            while (filterIndex < filter.Length)
            {
                //Escape spaces.
                while (filterIndex < filter.Length && filter[filterIndex] == ' ')
                {
                    filterIndex++;
                }

                if (filter.StartsWithCaseInsensitive(filterIndex, "id="))
                {
                    filterIndex += "id=".Length;
                    yield return new Token()
                    {
                        Index = filterIndex,
                        Type = TokenType.Id,
                        Value = GetValue(filter, ref filterIndex, TokenType.Id)
                    };

                }
                else if (filter.StartsWithCaseInsensitive(filterIndex, "pid="))
                {
                    filterIndex += "pid=".Length;
                    yield return new Token()
                    {
                        Index = filterIndex,
                        Type = TokenType.Pid,
                        Value = GetValue(filter, ref filterIndex, TokenType.Pid)
                    };
                }
                else if (filter.StartsWithCaseInsensitive(filterIndex, "tid="))
                {
                    filterIndex += "tid=".Length;
                    yield return new Token()
                    {
                        Index = filterIndex,
                        Type = TokenType.Tid,
                        Value = GetValue(filter, ref filterIndex, TokenType.Tid)
                    };
                }
                else if (filter.StartsWithCaseInsensitive(filterIndex, "activityId="))
                {
                    filterIndex += "activityId=".Length;
                    yield return new Token()
                    {
                        Index = filterIndex,
                        Type = TokenType.ActivityId,
                        Value = GetValue(filter, ref filterIndex, TokenType.ActivityId)
                    };
                }
                else if (filter.StartsWithCaseInsensitive(filterIndex, "relatedActivityId="))
                {
                    filterIndex += "relatedActivityId=".Length;
                    yield return new Token()
                    {
                        Index = filterIndex,
                        Type = TokenType.ActivityId,
                        Value = GetValue(filter, ref filterIndex, TokenType.ActivityId)
                    };
                }
                else if (filter.StartsWithCaseInsensitive(filterIndex, "rootActivity="))
                {
                    filterIndex += "rootActivity=".Length;
                    yield return new Token()
                    {
                        Index = filterIndex,
                        Type = TokenType.RootActivity,
                        Value = GetValue(filter, ref filterIndex, TokenType.RootActivity)
                    };
                }
                else if (filter.StartsWithCaseInsensitive(filterIndex, "minlevel="))
                {
                    filterIndex += "minlevel=".Length;
                    yield return new Token()
                    {
                        Index = filterIndex,
                        Type = TokenType.MinLevel,
                        Value = GetValue(filter, ref filterIndex, TokenType.MinLevel)
                    };
                }
                else if (filter.StartsWithCaseInsensitive(filterIndex, "level="))
                {
                    filterIndex += "level=".Length;
                    yield return new Token()
                    {
                        Index = filterIndex,
                        Type = TokenType.Level,
                        Value = GetValue(filter, ref filterIndex, TokenType.Level)
                    };

                }
                else if (filter.StartsWithCaseInsensitive(filterIndex, "where="))
                {
                    filterIndex += "where=".Length;
                    yield return new Token()
                    {
                        Index = filterIndex,
                        Type = TokenType.Where,
                        Value = GetValue(filter, ref filterIndex, TokenType.Where)
                    };
                }
                else
                {
                    //There might be multiple text filters.
                    int indexofSpace = filter.IndexOf(' ', filterIndex);
                    if (indexofSpace >= 0)
                    {
                        string textFilter = filter.Substring(filterIndex, indexofSpace - filterIndex);
                        filterIndex += textFilter.Length;
                        yield return new Token()
                        {
                            Index = filterIndex,
                            Type = TokenType.Text,
                            Value = textFilter
                        };
                    }
                    else
                    {
                        if (filter.Length > 0)
                        {
                            yield return new Token()
                            {
                                Index = filterIndex,
                                Type = TokenType.Text,
                                Value = filter.Substring(filterIndex)
                            };
                            filterIndex = filter.Length;
                        }
                        else
                            yield break;
                    }
                }
            }
        }

        static object GetValue(string filter, ref int filterIndex, TokenType tokenType)
        {
            object typedValue = null;
            string delimiter = Token.GetDelimiter(tokenType);
            while (filterIndex < filter.Length)
            {
                int index = filter.IndexOf(delimiter, filterIndex);
                if (index == -1)
                {
                    index = filter.Length;
                }
                else
                {
                    index++; //include the delimiter;
                }

                if (index >= 0)
                {
                    GetTypedValue(filterIndex,
                                    filter.Substring(filterIndex, index - filterIndex),
                                    tokenType,
                                    out typedValue);
                    filterIndex = index;
                    while (filterIndex < filter.Length && filter[filterIndex] == ' ')
                    {
                        filterIndex++;
                    }
                    break;
                }

                filterIndex++;
            }

            return typedValue;
        }

        static bool GetTypedValue(int currentIndex, string p, TokenType tokenType, out object value)
        {
            value = null;
            p = p.Trim();
            switch (tokenType)
            {
                case TokenType.Id:
                case TokenType.Tid:
                case TokenType.Level:
                case TokenType.MinLevel:
                    int v;
                    if (Int32.TryParse(p, out v))
                    {
                        value = v;
                    }
                    break;
                case TokenType.Pid:
                    uint v1;
                    if (uint.TryParse(p, out v1))
                    {
                        value = v1;
                    }
                    break;
                case TokenType.ActivityId:
                case TokenType.RootActivity:
                    Guid v2;
                    if (Guid.TryParse(p.Trim('"'), out v2))
                    {
                        value = v2;
                    }
                    break;
                case TokenType.Text:
                    value = (string)p;
                    break;
                case TokenType.Where:
                    if (p.IndexOf('[') != 0 || p.IndexOf(']') != p.Length - 1)
                    {
                        throw new FilterParserException("Where clause should be enclosed with []", currentIndex, null);
                    }
                    value = (string)p.Substring(1, p.Length - 2); //Trim the starting '[';
                    break;
            }

            return value == null;
        }

        class Token
        {
            public int Index { get; set; }
            public TokenType Type { get; set; }
            public object Value { get; set; }

            public static string GetDelimiter(TokenType token)
            {
                switch (token)
                {
                    case TokenType.Where: return "]";
                    default: return " ";
                }
            }
        }

        enum TokenType
        {
            Id,
            Pid,
            Tid,
            TaskId,
            Level,
            MinLevel,
            Where,
            Text,
            ActivityId,
            RootActivity,
        }

        public FilterParserContext Context
        {
            get;
            private set;
        }

    }

    internal static class TokenExtensions
    {
        public static bool StartsWithCaseInsensitive(this string input, int startIndex, string value)
        {
            if (input.Length <= startIndex + value.Length)
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                if (Char.ToLower(input[startIndex + i]) != Char.ToLower(value[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }


    public class FilterParserContext
    {
        internal FilterParserContext(string filter)
        {
            this.Filter = filter;
        }

        public string Filter { get; private set; }
        public Guid[] ActivityIdFilters { get; set; }
        public uint[] ProcessFilters { get; set; }
        public int[] ThreadsFilter { get; set; }
        public ushort? TaskFilter { get; set; }
        public int[] IdFilters { get; set; }
        public int[] LevelFilters { get; set; }
        public int? MinLevel { get; set; }
        public string[] TextFilters { get; set; }
        public Guid? RootActivityFilter { get; set; }
        public string WhereFilter { get; set; }
    }

    public class FilterParserException : Exception
    {
        public int ParserIndex { get; set; }

        public FilterParserException(string type, int parseIndex, Exception inner)
            : base(String.Format("Error while parsing token {0} at character {1}", type, parseIndex), inner)
        {
            this.ParserIndex = parseIndex;
        }

        public FilterParserException(string message, string type, int parseIndex, Exception inner)
            : base(String.Format("Error while parsing token {0} at character {1}. \r\nError:\r\n{2}", type, parseIndex, message), inner)
        {
            this.ParserIndex = parseIndex;
        }
    }


}
