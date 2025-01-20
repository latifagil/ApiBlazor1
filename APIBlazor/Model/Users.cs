using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIBlazor.Model
{
    public class Users
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        
        [Required]
        [ForeignKey("Role")] 
        public int RoleId { get; set; } 

        public Role Role { get; set; }
        public ICollection<Logins> Logins { get; set; } = new List<Logins>();
    }
}
