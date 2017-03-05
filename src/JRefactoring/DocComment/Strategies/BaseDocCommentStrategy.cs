using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JRefactoring.DocComment.Strategies
{
    public class BaseDocCommentStrategy : IDocCommentStrategy
    {
        private static readonly XmlReaderSettings _triviaReaderSettings = new XmlReaderSettings()
        {
            Async = false,
            CheckCharacters = false,
            CloseInput = false,
            ConformanceLevel = ConformanceLevel.Fragment,
            DtdProcessing = DtdProcessing.Ignore,
            IgnoreComments = false,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true
        };

        private Task<string> GetCommentAsync(DocCommentRefactoringContext context)
        {
            ISymbol targetSymbol = null;

            if (context.Symbol is IEventSymbol evt)
                targetSymbol = evt.OverriddenEvent ?? evt.ExplicitInterfaceImplementations.FirstOrDefault();
            else if (context.Symbol is IMethodSymbol method)
                targetSymbol = method.OverriddenMethod ?? method.ExplicitInterfaceImplementations.FirstOrDefault();
            else if (context.Symbol is INamedTypeSymbol nt && nt.OriginalDefinition == nt)
                targetSymbol = nt.BaseType == null || nt.BaseType.SpecialType == SpecialType.System_Object ? null : nt.BaseType;
            else if (context.Symbol is IPropertySymbol prop)
                targetSymbol = prop.OverriddenProperty ?? prop.ExplicitInterfaceImplementations.FirstOrDefault();

            if (targetSymbol == null && context.Symbol.ContainingType != null)
            {
                foreach (var iface in context.Symbol.ContainingType.AllInterfaces)
                {
                    var members = iface.GetMembers(context.Symbol.Name).Where(x => x.Kind == context.Symbol.Kind);
                    foreach (var member in members)
                    {
                        if (context.Symbol.ContainingType.FindImplementationForInterfaceMember(member) == context.Symbol)
                        {
                            targetSymbol = member;
                            break;
                        }
                    }
                }
            }

            if (targetSymbol != null)
            {
                var comment = targetSymbol.GetDocumentationCommentXml(expandIncludes: true, cancellationToken: context.CancellationToken);
                if (string.IsNullOrWhiteSpace(comment)) return Task.FromResult(comment);

                return Task.FromResult(RemoveInsignificantWhitespace(comment));
            }

            return Task.FromResult(string.Empty);
        }

        private string RemoveInsignificantWhitespace(string value)
        {
            // HACK: remove insignificant whitespace
            value = value.Replace("\n", string.Empty).Replace("\r", string.Empty);
            using (var sr = new StringReader(value))
            using (var reader = XmlReader.Create(sr, _triviaReaderSettings))
            {
                var element = XElement.Load(reader);
                foreach (var node in element.DescendantNodes().OfType<XText>())
                {
                    if (string.IsNullOrEmpty(node.Value)) continue;

                    var wsStart = node.Value[0];
                    var wsEnd = node.Value[node.Value.Length - 1];
                    node.Value = node.Value.Trim();
                    
                    if (char.IsWhiteSpace(wsStart) && char.IsWhiteSpace(wsEnd) &&
                        node.PreviousNode != null && node.NextNode != null)
                        node.Value = $"{wsStart}{node.Value}{wsEnd}";
                    else if (char.IsWhiteSpace(wsStart) && node.PreviousNode != null)
                        node.Value = $"{wsStart}{node.Value}";
                    else if (char.IsWhiteSpace(wsEnd) && node.NextNode != null)
                        node.Value = $"{node.Value}{wsEnd}";
                }
                return element.ToString(SaveOptions.DisableFormatting);
            }
        }

        public async Task<bool> CanCommentAsync(bool previousCanComment, DocCommentRefactoringContext context)
        {
            if (previousCanComment) return false;
            if (!context.DocComment.IsEmpty) return false;
            var comment = await context.GetValue(this, () => GetCommentAsync(context));
            return !string.IsNullOrEmpty(comment);
        }

        public Task CommentAsync(DocCommentRefactoringContext context)
        {
            var comment = context.GetValue<string>(this);

            var nodes = XElement.Parse(comment).Nodes().ToList();
            foreach (var node in nodes)
            {
                node.Remove();
                context.DocComment.Add(node);
            }

            return Task.FromResult(true);
        }
    }
}
