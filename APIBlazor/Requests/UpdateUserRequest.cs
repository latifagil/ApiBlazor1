using System.ComponentModel.DataAnnotations;
namespace APIBlazor.Requests
{
    public class UpdateUserRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Password { get; set; } // Добавьте это поле
    }
}
