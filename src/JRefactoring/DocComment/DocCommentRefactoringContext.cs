using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JRefactoring.DocComment
{
    public struct DocCommentRefactoringContext
    {
        private readonly Dictionary<RuntimeTypeHandle, object> _cache;

        public MemberDeclarationSyntax Syntax { get; }
        public ISymbol Symbol { get; }
        public XElement DocComment { get; }
        public SemanticModel SemanticModel { get; }
        public CancellationToken CancellationToken { get; }

        public DocCommentRefactoringContext(
            MemberDeclarationSyntax syntax, 
            ISymbol symbol, 
            XElement docComment, 
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            Syntax = syntax;
            Symbol = symbol;
            DocComment = docComment;
            SemanticModel = semanticModel;
            CancellationToken = cancellationToken;
            _cache = new Dictionary<RuntimeTypeHandle, object>();
        }

        public DocCommentRefactoringContext(
            DocCommentRefactoringContext context,
            MemberDeclarationSyntax syntax,
            ISymbol symbol,
            XElement docComment,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            Syntax = syntax;
            Symbol = symbol;
            DocComment = docComment;
            SemanticModel = semanticModel;
            CancellationToken = cancellationToken;
            _cache = context._cache;
        }

        public void SetValue<TValue>(IDocCommentStrategy strategy, TValue value) =>
            _cache[strategy.GetType().TypeHandle] = value;

        public TValue GetValue<TValue>(IDocCommentStrategy strategy)
        {
            if (_cache.TryGetValue(strategy.GetType().TypeHandle, out var value))
                return (TValue)value;
            return default(TValue);
        }

        public TValue GetValue<TValue>(IDocCommentStrategy strategy, Func<TValue> defaultValue)
        {
            if (_cache.TryGetValue(strategy.GetType().TypeHandle, out var value))
                return (TValue)value;
            return defaultValue();
        }

        public async Task<TValue> GetValue<TValue>(IDocCommentStrategy strategy, Func<Task<TValue>> defaultValue)
        {
            var th = strategy.GetType().TypeHandle;
            if (_cache.TryGetValue(th, out var value))
                return (TValue)value;
            var newValue = await defaultValue();
            _cache.Add(th, newValue);
            return newValue;
        }
    }
}
