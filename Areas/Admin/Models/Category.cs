using System.ComponentModel.DataAnnotations;

namespace MediumClone.Areas.Admin.Models
{
    public class Category
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
    }
}
