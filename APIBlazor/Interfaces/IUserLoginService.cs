using APIBlazor.Requests;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using static APIBlazor.Service.UserLoginService;


namespace APIBlazor.Interfaces
{
    public interface IUserLoginService
    {
        Task<IActionResult> Register([FromBody] RegisterUserRequest request);
        Task<IActionResult> Login([FromBody] LoginUserRequest request);
        Task<IActionResult> GetUsers();
        Task<UserDto?> GetUserIdFromTokenAsync(string token);
        Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request);
        Task<IActionResult> DeleteUser([FromBody] ChangeRoleAndDeleteRequest request);
        Task<IActionResult> GetUserById(int id);
        Task<IActionResult> ChangeUserRole([FromBody] ChangeRoleAndDeleteRequest request);

    }
}
