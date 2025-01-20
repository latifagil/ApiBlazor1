using APIBlazor.Interfaces;
using APIBlazor.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using static APIBlazor.Service.UserLoginService;

namespace APIBlazor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserLoginController
    {
        private readonly IUserLoginService _userLoginService;
        public UserLoginController(IUserLoginService userLoginService)
        {
            _userLoginService = userLoginService;
        }

        [HttpPost("Regist")]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
        {
            return await _userLoginService.Register(request);
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginUserRequest request)
        {
            return await _userLoginService.Login(request);
        }

        
        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetUsers()
        {
            return await _userLoginService.GetUsers();
        }

        [Authorize(Roles = "Администратор")]
        [HttpPut("ChangeUserRole")]
        public async Task<IActionResult> ChangeUserRole([FromBody] ChangeRoleAndDeleteRequest request)
        {
            return await _userLoginService.ChangeUserRole(request);
        }

        [HttpGet("GetUserById")]
        public async Task<IActionResult> GetUserById(int id)
        {
            return await _userLoginService.GetUserById(id);
        }

        [HttpDelete("DeleteUser")]
        public async Task<IActionResult> DeleteUser([FromBody] ChangeRoleAndDeleteRequest request)
        {
            return await _userLoginService.DeleteUser(request);
        }

        [HttpGet("GetUserIdFromToken")]
        public async Task<UserDto?> GetUserIdFromTokenAsync(string token)
        {
            return await _userLoginService.GetUserIdFromTokenAsync(token);
        }

        [HttpPut("UpdateUserInfo")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request)
        {
            return await _userLoginService.UpdateUser(request);
        }
    }
}
