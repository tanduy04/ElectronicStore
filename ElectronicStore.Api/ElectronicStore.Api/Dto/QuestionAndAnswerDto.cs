using System.ComponentModel.DataAnnotations;

namespace ElectronicStore.Api.Dto
{
    public class QuestionAndAnswerDto
    {
        [Required]
        public string Question { get; set; }

        [Required]
        public string Answer { get; set; }
    }
}
