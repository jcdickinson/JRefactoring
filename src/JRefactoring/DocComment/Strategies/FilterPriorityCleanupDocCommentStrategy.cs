using System.Threading.Tasks;

namespace JRefactoring.DocComment.Strategies
{
    public class FilterPriorityCleanupDocCommentStrategy : IDocCommentStrategy
    {
        public Task<bool> CanCommentAsync(bool previousCanComment, DocCommentRefactoringContext context)
        {
            if (!previousCanComment) return Task.FromResult(false);
            return Task.FromResult(context.DocComment.Element("filterpriority") != null);
        }

        public Task CommentAsync(DocCommentRefactoringContext context)
        {
            var element = context.DocComment.Element("filterpriority");
            if (element != null) element.Remove();
            return Task.FromResult(true);
        }
    }
}
