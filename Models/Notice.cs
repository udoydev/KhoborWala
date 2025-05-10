using System;

namespace MediumClone.Models
{
    public class Notice
    {
        public int Id { get; set; }
        public string? UserId { get; set; } // The user who receives the notice, or null for global
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
