using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace JRefactoring.DocComment.Strategies
{
    public class OverrideCleanupDocCommentStrategy : IDocCommentStrategy
    {
        private const string Message = "When overridden in a derived class, ";

        public Task<bool> CanCommentAsync(bool previousCanComment, DocCommentRefactoringContext context)
        {
            if (!previousCanComment || !context.Symbol.IsOverride) return Task.FromResult(false);

            var summary = context.DocComment.Element("summary");
            var canComment = summary != null && ((string)summary).StartsWith(Message, StringComparison.OrdinalIgnoreCase);
            return Task.FromResult(canComment);
        }

        public Task CommentAsync(DocCommentRefactoringContext context)
        {
            var summary = context.DocComment
                .Elements("summary")
                .Nodes().OfType<XText>().FirstOrDefault(x => x.Value.StartsWith(Message, StringComparison.OrdinalIgnoreCase));
            if (summary == null || summary.Value.Length < Message.Length + 1) return Task.FromResult(false);

            var sb = new StringBuilder();
            sb.Append(char.ToUpperInvariant(summary.Value[Message.Length]));
            sb.Append(summary.Value, Message.Length + 1, summary.Value.Length - Message.Length - 1);
            summary.Value = sb.ToString();

            return Task.FromResult(true);
        }
    }
}
