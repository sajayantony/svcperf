namespace EtlViewer.Viewer.Controls
{
    using ICSharpCode.AvalonEdit;
    using ICSharpCode.AvalonEdit.CodeCompletion;
    using Roslyn.Compilers;
    using Roslyn.Compilers.CSharp;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;

    class NonVsCSharpTextBox : TextEditor
    {
        CompletionWindow completionWindow;
        readonly MetadataReference[] baseAssemblies;

        public NonVsCSharpTextBox()
        {
            this.TextArea.TextEntering += TextArea_TextEntering;
            this.TextArea.TextEntered += TextArea_TextEntered;
            this.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("C#");
            this.FontFamily = new System.Windows.Media.FontFamily("Consolas");
            this.FontSize = 12;

            List<MetadataReference> references = new List<MetadataReference>
                    {
                        new AssemblyFileReference(typeof(object).Assembly.Location),                 // mscorlib
                        new AssemblyFileReference(typeof(System.Guid).Assembly.Location),            // System
                        new AssemblyFileReference(typeof(System.Linq.IQueryable).Assembly.Location), // System.Core
                    };

            foreach (string location in EtlViewer.QueryFx.QueryCompiler.GetAssemblyLocations())
            {
                references.Add(new AssemblyFileReference(location));
            }

            baseAssemblies = references.ToArray();
        }

        void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == ".")
            {
                try
                {
                    string startString = "using System; namespace SomeNamespace { public class NotAProgram { private void SomeMethod() { ";
                    //string endString = " } } }";

                    var tree = Roslyn.Compilers.CSharp.SyntaxTree.ParseCompilationUnit(startString + this.Text.Substring(0, this.CaretOffset + 1));
                    var compilation = Roslyn.Compilers.CSharp.Compilation.Create(
                        "MyCompilation",
                        syntaxTrees: new[] { tree },
                        references: baseAssemblies);
                    var semanticModel = compilation.GetSemanticModel(tree);

                    // Ask for symbols at the caret position.
                    //var symbols = semanticModel.LookupSymbols(this.txtDurationQuery.CaretOffset + startString.Length);
                    var position = this.CaretOffset + startString.Length;
                    var token = tree.GetRoot().FindToken(position);
                    var identifier = token.Parent;// is IdentifierNameSyntax ? token.Parent : token.GetPreviousToken().Parent;
                    IList<Symbol> symbols = null;
                    if (identifier is QualifiedNameSyntax)
                    {
                        var semanticInfo = semanticModel.GetTypeInfo((identifier as QualifiedNameSyntax).Left);
                        var type = semanticInfo.Type;
                        symbols = semanticModel.LookupSymbols(position, container: type, options: LookupOptions.IncludeExtensionMethods);
                    }
                    else if (identifier is MemberAccessExpressionSyntax)
                    {
                        var semanticInfo = semanticModel.GetTypeInfo((identifier as MemberAccessExpressionSyntax).Expression);
                        var type = semanticInfo.Type;
                        symbols = semanticModel.LookupSymbols(position, container: type, options: LookupOptions.IncludeExtensionMethods);
                    }
                    else if (identifier is IdentifierNameSyntax)
                    {
                        var semanticInfo = semanticModel.GetTypeInfo(identifier as IdentifierNameSyntax);
                        var type = semanticInfo.Type;
                        symbols = semanticModel.LookupSymbols(position, container: type, options: LookupOptions.IncludeExtensionMethods);
                    }

                    if (symbols != null && symbols.Count > 0)
                    {
                        completionWindow = new CompletionWindow(this.TextArea);
                        IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
                        var distinctSymbols = (from s in symbols select s.Name).Distinct();
                        foreach (var symbol in distinctSymbols)
                        {
                            data.Add(new QueryCompletionData(symbol));
                        }

                        completionWindow.Show();
                        completionWindow.Closed += delegate
                        {
                            completionWindow = null;
                        };
                    }
                }
                catch { }
            }

        }

        void TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && completionWindow != null)
            {
                if (!char.IsLetterOrDigit(e.Text[0]))
                {
                    // Whenever a non-letter is typed while the completion window is open,
                    // insert the currently selected element.
                    completionWindow.CompletionList.RequestInsertion(e);
                }
            }
            // Do not set e.Handled=true.
            // We still want to insert the character that was typed.

        }
    }
}
