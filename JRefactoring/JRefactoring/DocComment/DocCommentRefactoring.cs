using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JRefactoring.DocComment
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(DocCommentRefactoring)), Shared]
    internal sealed class DocCommentRefactoring : CodeRefactoringProvider
    {
        private const string TriviaPrefix = "/// ";

        private static readonly HashSet<SyntaxKind> _commentSyntax = new HashSet<SyntaxKind>()
        {
            SyntaxKind.SingleLineDocumentationCommentTrivia,
            SyntaxKind.MultiLineDocumentationCommentTrivia
        };

        private readonly List<IDocCommentStrategy> _strategies = new List<IDocCommentStrategy>();

        public DocCommentRefactoring()
        {
            _strategies.Add(new Strategies.BaseDocCommentStrategy());
            _strategies.Add(new Strategies.FilterPriorityCleanupDocCommentStrategy());
            _strategies.Add(new Strategies.OverrideCleanupDocCommentStrategy());
        }

        private XElement GenerateEmptyDocComment() => new XElement("doc");

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);
            
            var memberDecl = node as MemberDeclarationSyntax;
            if (memberDecl == null) return;

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);
            if (semanticModel == null) return;

            var symbol = semanticModel.GetDeclaredSymbol(memberDecl, context.CancellationToken);
            if (symbol == null) return;

            var doc = symbol.GetDocumentationCommentXml(expandIncludes: true, cancellationToken: context.CancellationToken);
            XElement xdoc;

            try
            {
                xdoc = string.IsNullOrEmpty(doc)
                    ? GenerateEmptyDocComment()
                    : XElement.Parse(doc);
            }
            catch { return; }

            var docContext = new DocCommentRefactoringContext(memberDecl, symbol, xdoc, semanticModel, context.CancellationToken);
            var previousCanComment = false;

            for (var i = 0; i < _strategies.Count; i++)
            {
                if (await _strategies[i].CanCommentAsync(previousCanComment, docContext))
                {
                    var ii = i;
                    var action = CodeAction.Create(Resources.Title, c => GenerateDocCommentAsync(context.Document, docContext, ii, c), nameof(DocCommentRefactoring));
                    context.RegisterRefactoring(action);
                    previousCanComment = true;
                }
            }
        }

        private async Task<Document> GenerateDocCommentAsync(Document document, DocCommentRefactoringContext docContext, int startIndex, CancellationToken cancellationToken)
        {
            docContext = new DocCommentRefactoringContext(
                docContext,
                docContext.Syntax,
                docContext.Symbol,
                docContext.DocComment,
                await document.GetSemanticModelAsync(cancellationToken),
                cancellationToken);

            // Allow all strategies to try generate, some may mutate an existing DocComment.
            var previousCanComment = false;
            for (var i = startIndex; i < _strategies.Count; i++)
            {
                if (await _strategies[i].CanCommentAsync(previousCanComment, docContext))
                {
                    await _strategies[i].CommentAsync(docContext);
                    previousCanComment = true;
                }
            }

            var root = await document.GetSyntaxRootAsync(docContext.CancellationToken);
            var trivia = docContext.Syntax.GetLeadingTrivia();

            // Remove existing DocComments
            for (var i = 0; i < trivia.Count; i++)
            {
                if (trivia[i].IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                    trivia[i].IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                {
                    trivia.RemoveAt(i--);
                }
            }

            var insertionIndex = 0;
            var newLine = NewLineTrivia(root);
            if (trivia.Count  < 2)
            {
                // Empty file, insert a linebreak after where the trivia
                // will be inserted.
                trivia = trivia.Insert(0, newLine);
                insertionIndex = 0;
            }
            else
            {
                // Probably a newline followed by a whitespace. Insert the
                // trivia before the newline.
                insertionIndex = trivia.Count - 1;
                trivia = trivia.Insert(insertionIndex, newLine);
            }

            var whitespace = string.Empty;
            if (trivia[trivia.Count - 1].IsKind(SyntaxKind.WhitespaceTrivia))
            {
                whitespace = trivia[trivia.Count - 1].ToString();
            }

            trivia = trivia.InsertRange(insertionIndex, SyntaxFactory.ParseLeadingTrivia(GetTriviaText(docContext.DocComment.Nodes(), whitespace, newLine.ToString())));
            var newMember = docContext.Syntax.WithLeadingTrivia(trivia);
            root = root.ReplaceNode(docContext.Syntax, newMember);

            return document.WithSyntaxRoot(root);
        }

        private SyntaxTrivia NewLineTrivia(SyntaxNode root)
        {
            var result = root.DescendantTrivia().FirstOrDefault(x => x.IsKind(SyntaxKind.EndOfLineTrivia));
            if (result == default(SyntaxTrivia)) result = SyntaxFactory.ElasticCarriageReturnLineFeed;
            return result;
        }

        private string GetTriviaText(IEnumerable<XNode> nodes, string prefix, string newLine)
        {
            using (var tw = new StringWriter())
            {
                tw.Write($"{prefix}{TriviaPrefix}");

                var triviaWriterSettings = new XmlWriterSettings()
                {
                    Async = false,
                    CheckCharacters = false,
                    CloseOutput = false,
                    ConformanceLevel = ConformanceLevel.Fragment,
                    Encoding = Encoding.Unicode,
                    Indent = true,
                    IndentChars = "  ",
                    NamespaceHandling = NamespaceHandling.OmitDuplicates,
                    NewLineChars = $"{newLine}{prefix}{TriviaPrefix}",
                    NewLineHandling = NewLineHandling.None,
                    NewLineOnAttributes = false,
                    OmitXmlDeclaration = true,
                    WriteEndDocumentOnClose = false
                };

                using (var writer = XmlWriter.Create(tw, triviaWriterSettings))
                {
                    foreach (var item in nodes)
                        item.WriteTo(writer);
                }
                return tw.ToString();
            }
        }
    }
}
