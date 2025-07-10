using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiCalendarAssistant.Data.Models
{
    public class UserTask
    {
        [Key]
        public int Id { get; set; } 

        public string Title { get; set; } = null!; 

        public string? Description { get; set; } 

        public string? Color { get; set; } 
            
        [ForeignKey(nameof(User))]
        public string? UserId { get; set; } 
        public ApplicationUser? User { get; set; } 
		public Importance Importance { get; set; } = Importance.Medium; 

		public bool IsCompleted { get; set; } = false;
    }
}

