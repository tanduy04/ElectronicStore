using System.ComponentModel.DataAnnotations;

namespace ElectronicStore.Api.Dto
{
    public class NewProductReviewDto
    {
        [Required]
        public int ProductId { get; set; }
        [Required]

        public string FullName { get; set; }
        [Required]

        public string Phone { get; set; }
        [Required]

        public int Rating { get; set; }
        public int? ParentID { get; set; }
        [Required]

        public string? Content { get; set; }
    }
    public class ReplyReview
    {
        [Required]

        public int ParentID { get; set; }
        [Required]

        public string Content { get; set; }
    }
    public class ViewReplyReview
    {
        public int ReviewID { get; set; }
        public int ParentID { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
    }
    public class ProductReviewDto
    {
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }

        [Range(1,5)]
        public int Rating { get; set; }
        public int? ParentId { get; set; }
        public string? Content { get; set; }
        public ViewReplyReview? ReplyReview { get; set; }

        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }   
}
