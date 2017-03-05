using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace JRefactoring.DocComment
{
    internal static class DocFactory
    {
        public static string ToDocComment(this ISymbol symbol,
            SymbolDisplayGlobalNamespaceStyle globalNamespaceStyle = SymbolDisplayGlobalNamespaceStyle.Included, 
            SymbolDisplayTypeQualificationStyle typeQualificationStyle = SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces, 
            SymbolDisplayGenericsOptions genericsOptions = SymbolDisplayGenericsOptions.IncludeTypeParameters, 
            SymbolDisplayMemberOptions memberOptions = SymbolDisplayMemberOptions.IncludeContainingType, 
            SymbolDisplayDelegateStyle delegateStyle = SymbolDisplayDelegateStyle.NameAndSignature, 
            SymbolDisplayExtensionMethodStyle extensionMethodStyle = SymbolDisplayExtensionMethodStyle.StaticMethod, 
            SymbolDisplayParameterOptions parameterOptions = SymbolDisplayParameterOptions.IncludeType, 
            SymbolDisplayPropertyStyle propertyStyle = SymbolDisplayPropertyStyle.NameOnly, 
            SymbolDisplayLocalOptions localOptions = SymbolDisplayLocalOptions.None, 
            SymbolDisplayKindOptions kindOptions = SymbolDisplayKindOptions.None, 
            SymbolDisplayMiscellaneousOptions miscellaneousOptions = SymbolDisplayMiscellaneousOptions.ExpandNullable)
        {
            if (symbol == null) return string.Empty;
            var fmt = symbol.ToDisplayString(new SymbolDisplayFormat(globalNamespaceStyle, typeQualificationStyle, genericsOptions, memberOptions, delegateStyle, extensionMethodStyle, parameterOptions, propertyStyle, localOptions, kindOptions, miscellaneousOptions));
            var sb = new StringBuilder(fmt.Length);
            for (var i = 0; i < fmt.Length; i++)
            {
                var c = fmt[i];
                if (fmt[i] == '<') sb.Append('{');
                else if (fmt[i] == '>') sb.Append('}');
                else sb.Append(c);
            }
            return sb.ToString();
        }

        public static XElement See(string target) =>
            new XElement("see", new XAttribute("cref", target));

        public static XElement See(ISymbol symbol) => See(symbol.ToDocComment());

        public static XElement SeeShort(ISymbol symbol) => See(symbol.ToDocComment(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly
        ));

        public static XElement Param(string name) =>
            new XElement("param", new XAttribute("name", name));

        public static XElement True() => new XElement("c", "true");
        public static XElement False() => new XElement("c", "false");
        public static XElement Null() => new XElement("c", "null");
    }
}
