namespace EtlViewer.Viewer.Controls
{
    using Roslyn.Compilers;
    using Roslyn.Compilers.CSharp;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Media;
    
    class CSharpTextBox : TextBox
    {
        static CSharpTextBox()
        {
            //override the metadata of Foreground and Background properties so that 
            //the control can set them back to the correct value if they are changed.
            Control.ForegroundProperty.OverrideMetadata(typeof(CSharpTextBox),
                new FrameworkPropertyMetadata(Brushes.Transparent, OnForegroundChanged));
            Control.BackgroundProperty.OverrideMetadata(typeof(CSharpTextBox),
                new FrameworkPropertyMetadata(OnBackgroundChanged));

            //Since this is a text box for code, the default settings for a few of the 
            //properties should be different (fixed width font, accepts return and tab)
            Control.FontFamilyProperty.OverrideMetadata(typeof(CSharpTextBox),
                new FrameworkPropertyMetadata(new FontFamily("Consolas")));
            TextBoxBase.AcceptsReturnProperty.OverrideMetadata(typeof(CSharpTextBox),
                new FrameworkPropertyMetadata(true));
            TextBoxBase.AcceptsTabProperty.OverrideMetadata(typeof(CSharpTextBox),
                new FrameworkPropertyMetadata(true));
        }

        public CSharpTextBox()
        {
            this.Loaded += (s, e) =>
            {
                var c = VisualTreeHelper.GetChild(this, 0);
                var sv = VisualTreeHelper.GetChild(c, 0) as ScrollViewer;
                sv.ScrollChanged += (s2, e2) => { this.InvalidateVisual(); };
            };
        }


        //Using a DependencyProperty to manage the default foreground color for the text in the text box
        public static readonly DependencyProperty DefaultForegroundColorProperty =
            DependencyProperty.Register("ForegroundColor", typeof(Color), typeof(CSharpTextBox),
                new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        public Color DefaultForegroundColor
        {
            get { return (Color)GetValue(DefaultForegroundColorProperty); }
            set { SetValue(DefaultForegroundColorProperty, value); }
        }

        //using a DependencyProperty to manage the background color for the text box
        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor", typeof(Color), typeof(CSharpTextBox),
                new FrameworkPropertyMetadata(Colors.White, FrameworkPropertyMetadataOptions.AffectsRender, OnBackgroundColorChanged));

        public Color BackgroundColor
        {
            get { return (Color)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }


        //Foreground must be the transparent brush for CSharpTextBox to work. If someone tries to set it
        //To something other than Transparent, this function changes it back
        private static void OnForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != Brushes.Transparent)
            {
                ((CSharpTextBox)d).Foreground = Brushes.Transparent;
            }
        }

        //If the background color changes, we need to reset the Background property to a new transparent 
        //brush in order to keep the caret and text selection adornments
        static void OnBackgroundColorChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var codebox = (CSharpTextBox)obj;
            codebox.Background = new SolidColorBrush(codebox.GetTransparentBackgroundColor());
        }

        //Like Foreground, Background must also be transparent. if someone tries to change it, this function 
        //changes it back. However, decorations like the caret and text selection box base their color on the 
        //Background property. Thus, set the background to a transparent version of the background color, 
        //provided by the TransparentBackgroundColor property on CSharpTextBox
        private static void OnBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var codebox = (CSharpTextBox)d;
            var transparent_bg = codebox.GetTransparentBackgroundColor();
            var bgbrush = e.NewValue as SolidColorBrush;
            if (bgbrush == null || bgbrush.Color != transparent_bg)
            {
                codebox.Background = new SolidColorBrush(transparent_bg);
            }
        }


        //helper property to provide a transparent version of the current Background color
        private Color GetTransparentBackgroundColor()
        {
            return Color.FromArgb(0, BackgroundColor.R, BackgroundColor.G, BackgroundColor.B);
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);
            this.InvalidateVisual();
        }

        //Render the text in the text box, using the specified background color and syntax
        //color scheme. The original rendering done by the text box is invisible as both 
        //the foreground and background brushes are transparent
        protected override void OnRender(DrawingContext dc)
        {
            var ft = new FormattedText(
                this.Text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(this.FontFamily.Source),
                this.FontSize,
                new SolidColorBrush(this.DefaultForegroundColor));

            //We specify the left and top margins to match the original rendering exactly. 
            //that way, the original caret and text selection adornments line up correctly.
            var left_margin = 2.0 + this.BorderThickness.Left;
            var top_margin = 2.0 + this.BorderThickness.Top;

            dc.PushClip(new RectangleGeometry(new Rect(0, 0, this.ActualWidth, this.ActualHeight)));
            dc.DrawRectangle(this.Background, new Pen(), new Rect(0, 0, this.ActualWidth, this.ActualHeight));

            CsharpSyntaxWalker.Start(this.Text, dc, ft);

            dc.DrawText(ft, new Point(left_margin - this.HorizontalOffset, top_margin - this.VerticalOffset));
        }

        static SolidColorBrush keywordBrush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255));
    }


    class CsharpSyntaxWalker : SyntaxWalker
    {
        DrawingContext dc;
        FormattedText formatter;
        SolidColorBrush keywordBrush = new SolidColorBrush(Color.FromRgb(0, 0, 255));
        SolidColorBrush stringBrush = new SolidColorBrush(Color.FromRgb(163, 21, 21));
        SolidColorBrush triviaBrush = new SolidColorBrush(Color.FromRgb(0, 128, 46));
        SolidColorBrush typeBrush = new SolidColorBrush(Color.FromRgb(43, 145, 175));

        SemanticModel Model { get; set; }
        static object ThisLock = new object();
        public static NamespaceSymbol[] NamespaceCollection { get; set; }
        public static MetadataReference[] References { get; set; }
        public static bool Initialized = false;

        public static void EnsureInitialized()
        {
            if (!Initialized)
            {
                lock (ThisLock)
                {
                    References = new MetadataReference[]{
                    MetadataReference.CreateAssemblyReference(typeof(object).Assembly.GetName().Name),
                    new MetadataFileReference(typeof(SequenceDiagram).Assembly.Location)};

                    SyntaxTree tree = SyntaxTree.ParseText(JitPrimeTemplate);
                    var compilation = Roslyn.Compilers.CSharp.Compilation.Create(
                        "MyCompilation",
                        syntaxTrees: new[] { tree },
                        references: References);
                    var model = compilation.GetSemanticModel(tree);
                    var namespaces = model.Compilation.GlobalNamespace.GetMembers()
                                        .Where((e) => e is NamespaceSymbol);
                    List<NamespaceSymbol> items = new List<NamespaceSymbol>();
                    foreach (var item in namespaces)
                    {
                        items.Add(item as NamespaceSymbol);
                    }
                    CsharpSyntaxWalker.NamespaceCollection = items.ToArray();
                    Initialized = true;
                    Trace.Assert(NamespaceContainsType("SequenceDiagram") == true);
                }
            }
        }

        public CsharpSyntaxWalker(SemanticModel model, DrawingContext dc, FormattedText formattedText)
            : base(Roslyn.Compilers.Common.SyntaxWalkerDepth.Node)
        {
            EnsureInitialized();
            this.Model = model;
            this.dc = dc;
            this.formatter = formattedText;
        }

        public static void Start(string text, DrawingContext dc, FormattedText ft)
        {
            SyntaxTree tree = SyntaxTree.ParseText(text);
            var compilation = Roslyn.Compilers.CSharp.Compilation.Create(
                "MyCompilation",
                syntaxTrees: new[] { tree },
                references: References);
            var walker = new CsharpSyntaxWalker(compilation.GetSemanticModel(tree), dc, ft);
            walker.Visit(tree.GetRoot());

            foreach (var item in tree.GetRoot().DescendantTrivia())
            {
                walker.VisitTrivia(item);
            }
        }

        public override void VisitToken(SyntaxToken token)
        {
            if (token.IsKeyword() ||
                (token.Kind == SyntaxKind.IdentifierToken && token.ToString() == "var"))
            {
                formatter.SetForegroundBrush(keywordBrush, token.Span.Start, token.Span.Length);
                return;
            }

            switch (token.Kind)
            {
                case SyntaxKind.StringLiteralToken:
                case SyntaxKind.StringLiteralExpression:
                    formatter.SetForegroundBrush(stringBrush, token.Span.Start, token.Span.Length);
                    break;
                case SyntaxKind.IdentifierToken:
                    if (token.Parent is SimpleNameSyntax)
                    {
                        //if (NamespaceContainsType(((SimpleNameSyntax)token.Parent).PlainName))
                        {
                            formatter.SetForegroundBrush(typeBrush, token.Span.Start, token.Span.Length);
                        }
                    }
                    break;
                case SyntaxKind.SingleLineCommentTrivia:
                case SyntaxKind.MultiLineCommentTrivia:
                    formatter.SetForegroundBrush(triviaBrush, token.Span.Start, token.Span.Length);
                    break;
            }
        }

        private static bool NamespaceContainsType(string typeName)
        {
            foreach (var item in NamespaceCollection)
            {
                if (item.GetTypeMembers(typeName).Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public override void VisitTrivia(SyntaxTrivia trivia)
        {
            if (trivia.Kind == SyntaxKind.SingleLineCommentTrivia || trivia.Kind == SyntaxKind.MultiLineCommentTrivia)
            {
                formatter.SetForegroundBrush(triviaBrush, trivia.Span.Start, trivia.Span.Length);
            }
            base.VisitTrivia(trivia);
        }

        const string JitPrimeTemplate = @"//--------------------------------------------------------------------------------
// SequenceItem SequenceDiagram[string name]   - Indexer to sequence steps         
// public void Connect(SequenceItem from, SequenceItem to, string message = null)
//--------------------------------------------------------------------------------            

Guid g = Guid.Parse(""80000146-0000-fe00-b63f-84710c7967bb"");
var q = from e in playback.GetObservable<SystemEvent>()
        where e.Header.ActivityId == g
        select new
        {
            Id = e.Header.EventId
        };


var buffer = (from e in playback.BufferOutput(q) select e);
playback.Run();
var events = buffer.ToList();

SequenceDiagram diagram = new SequenceDiagram();
diagram.Title = ""Http Activity"";
var eventNames = (from s in playback.KnownTypes
                    let attr = (ManifestEventAttribute)s.GetCustomAttributes(false)
                                    .Where((e) => e is ManifestEventAttribute).FirstOrDefault()
                    where attr != null
                    select new
                    {
                        Id = attr.EventId,
                        //Opcode = attr.Opcode,
                        Name = s.Name
                    }).ToDictionary((e) => e.Id, (e) => e.Name);

SequenceItem from = null;
foreach (var item in events)
{
    string eventName = eventNames[item.Id];
    SequenceItem to = diagram[eventName] ?? (diagram[eventName] = new SequenceItem(eventName));
    if (from != null)
    {
        diagram.Connect(from, to);
    }
    from = to;
}
diagram.Dump();";
    }
}
