using System.ComponentModel.DataAnnotations;

namespace CrucibleBlogMVC.Models
{
    public class Comment
    {
        // Primary Key
        public int Id { get; set; }

        [Required]
        [Display(Name = "Comment")]
        [StringLength(5000, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.", MinimumLength = 2)]
        public string? Body { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime CreatedDate { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? UpdatedDate { get; set; }

        [StringLength(200, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.", MinimumLength = 2)]
        public string? UpdateReason { get; set; }

        // Foreign Keys
        public int BlogPostId { get; set; }

        [Required]
        public string? AuthorId { get; set; }

        // Navigation Properties 
        public virtual BlogPost? BlogPost { get; set; }

        public virtual BlogUser? Author { get; set; }
    }
}
