using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EstateEase.Models.Entities
{
    public class PropertyImage
    {
        [Key]
        public string Id { get; set; }
        public string ImagePath { get; set; }
        public bool IsMain { get; set; }
        public string PropertyId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string ImageType { get; set; }

        [ForeignKey("PropertyId")]
        public virtual Property Property { get; set; }
    }
} 