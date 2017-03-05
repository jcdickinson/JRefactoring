using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JRefactoring.DocComment
{
    public interface IDocCommentStrategy
    {
        Task<bool> CanCommentAsync(bool previousCanComment, DocCommentRefactoringContext context);

        Task CommentAsync(DocCommentRefactoringContext context);
    }
}
