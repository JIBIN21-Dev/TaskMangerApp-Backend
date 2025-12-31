using System.ComponentModel.DataAnnotations;

namespace TaskManger.Models
{
    public class Users
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Username { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
