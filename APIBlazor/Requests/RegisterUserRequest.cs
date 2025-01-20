using System.ComponentModel.DataAnnotations;

namespace APIBlazor.Requests
{
    public class RegisterUserRequest
    {
        [Required]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string Description { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
