using System.ComponentModel.DataAnnotations;

namespace CrucibleBlogMVC.Models
{
    public class BlogLike
    {
        public int Id { get; set; }
        public int BlogPostId { get; set; }
        public bool IsLiked { get; set; }
        [Required]
        public string? BlogUserId { get; set; }

        // Navigation Properties
        public virtual BlogPost? BlogPost { get; set; }
        public virtual BlogUser? BlogUser { get; set; }
    }
}
