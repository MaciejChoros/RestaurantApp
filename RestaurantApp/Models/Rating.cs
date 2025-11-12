using System.ComponentModel.DataAnnotations;

namespace RestaurantApp.Models
{
    public class Rating
    {
        [Key]
        public int Id { get; set; }
        public int DishId { get; set; }
        public Dish Dish { get; set; }
        [Range(1, 5)]
        public int Stars { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}