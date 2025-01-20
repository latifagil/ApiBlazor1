using APIBlazor.DataBaseContext;
using APIBlazor.Interfaces;
using APIBlazor.Model;
using APIBlazor.Requests;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Azure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Net.Http;
using Azure.Core;
namespace APIBlazor.Service
{

    public class UserLoginService : IUserLoginService
    {
        private readonly ContextDB _context;
        private readonly JwtService _jwtService;
        private readonly ILogger<UserLoginService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserLoginService(ContextDB context, JwtService jwtService, ILogger<UserLoginService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }



        public async Task<IActionResult> GetUsers()
        {
             try
                {
                    var users = await _context.Users
                        .Include(u => u.Role) // Включаем данные о роли
                        .Include(u => u.Logins) // Включаем данные о логинах
                        .Select(u => new
                        {
                            u.Id,
                            u.Name,
                            u.Description,
                            Role = u.Role.Name, // Название роли
                            Logins = u.Logins.Select(l => l.Email).ToList() // Список логинов (email)
                        })
                        .ToListAsync();

                    return new OkObjectResult(users);
                }
            catch
            {

                return new BadRequestObjectResult("Произошла ошибка на сервере.");
            }
        }


        public async Task<IActionResult> Login([FromBody] LoginUserRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    _logger.LogWarning("Попытка входа с пустым email или паролем.");
                    return new BadRequestObjectResult("Email и пароль обязательны.");
                }

                var login = await _context.Logins
                    .Include(l => l.Users)
                    .ThenInclude(u => u.Role)
                    .FirstOrDefaultAsync(l => l.Email == request.Email);

                if (login == null)
                {
                    _logger.LogWarning("Пользователь с email {Email} не найден.", request.Email);
                    return new UnauthorizedObjectResult("Неверный email или пароль.");
                }
                if (login.Password != request.Password)
                {
                    _logger.LogWarning("Неверный пароль для пользователя с email {Email}.", request.Email);
                    return new UnauthorizedObjectResult("Неверный email или пароль.");
                }
                if (login.Users == null)
                {
                    _logger.LogError("Данные пользователя неполные: Users не найден для email {Email}.", request.Email);
                    return new UnauthorizedObjectResult("Данные пользователя неполные: Users не найден.");
                }
                if (login.Users.Role == null)
                {
                    _logger.LogError("Данные пользователя неполные: Role не найдена для пользователя с email {Email}.", request.Email);
                    return new UnauthorizedObjectResult("Данные пользователя неполные: Role не найдена.");
                }
                var userDto = new UserDto
                {
                    Id = login.Users.Id,
                    Name = login.Users.Name,
                    Description = login.Users.Description,
                    Role = login.Users.Role.Name,
                    Email = login.Email,
                    Logins = new List<string> { login.Email }
                };
                var token = _jwtService.GenerateJwtToken(userDto);
                _logger.LogInformation("Пользователь с email {Email} успешно авторизован.", request.Email);
                return new OkObjectResult(new { Token = token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при авторизации пользователя с email {Email}.", request.Email);
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
        {
            if (_context.Logins.Any(l => l.Email == request.Email))
            {
                return new BadRequestObjectResult("Email уже используется");
            }
            var user = new Users
            {
                Name = request.Name,
                Description = request.Description,
                RoleId = 2
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            var login = new Logins
            {
                Email = request.Email,
                Password = request.Password,
                UserId = user.Id
            };
            _context.Logins.Add(login);
            _context.SaveChanges();
            return new OkObjectResult("Пользователь успешно зарегистрирован");
        }

        public async Task<IActionResult> GetUserById([FromQuery] int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.Logins)
                    .Where(u => u.Id == id)
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        Name = u.Name,
                        Description = u.Description,
                        Role = u.Role.Name,
                        Logins = u.Logins.Select(l => l.Email).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return new NotFoundObjectResult(new { Message = "Пользователь не найден" });
                }

                return new OkObjectResult(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении пользователя по ID: {Id}", id);
                return new BadRequestObjectResult(new { Message = "Произошла ошибка на сервере." });
            }
        }   

        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)//404
        {
            return new NotFoundResult();
        }
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return new OkObjectResult(new
            {
                status = true,
                MessageContent = "Пользователь удален"
            });
        }

        

        public async Task<UserDto?> GetUserIdFromTokenAsync(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var userIdClaim = jwtToken.Claims
                    .FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

                var nameClaim = jwtToken.Claims
                    .FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;

                var descriptionClaim = jwtToken.Claims
                    .FirstOrDefault(c => c.Type == "description")?.Value;

                var roleClaim = jwtToken.Claims
                    .FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;

                var emailClaim = jwtToken.Claims
                    .FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

                var loginsClaim = jwtToken.Claims
                    .Where(c => c.Type == "logins")
                    .Select(c => c.Value)
                    .ToList();

                if (int.TryParse(userIdClaim, out int userId))
                {
                    return new UserDto
                    {
                        Id = userId,
                        Name = nameClaim ?? "Не указано",
                        Description = descriptionClaim ?? "Не указано",
                        Role = roleClaim ?? "Не указано",
                        Email = emailClaim,
                        Logins = loginsClaim
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                // Логируем ошибку
                Console.WriteLine($"Ошибка при чтении токена: {ex.Message}");
                return null;
            }
        }
        public async Task<UserDto?> GetUserFromTokenAsync(string token)
        {   
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var userIdClaim = jwtToken.Claims
                    .FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                var nameClaim = jwtToken.Claims
                    .FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;
                var descriptionClaim = jwtToken.Claims
                    .FirstOrDefault(c => c.Type == "description")?.Value;
                var roleClaim = jwtToken.Claims
                    .FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
                var emailClaim = jwtToken.Claims
                    .FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
                var loginsClaim = jwtToken.Claims
                    .Where(c => c.Type == "logins")
                    .Select(c => c.Value)
                    .ToList();
                if (int.TryParse(userIdClaim, out int userId))
                {
                    return new UserDto
                    {
                        Id = userId,
                        Name = nameClaim ?? "Не указано",
                        Description = descriptionClaim ?? "Не указано",
                        Role = roleClaim ?? "Не указано",
                        Email = emailClaim,
                        Logins = loginsClaim
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при чтении токена: {ex.Message}");
                return null;
            }
        }

        public async Task<IActionResult> UpdateUser(UpdateUserRequest request)
        {
                try
                {
                    if (request == null)
                    {
                        _logger.LogWarning("Запрос на обновление пользователя пуст.");
                        return new BadRequestObjectResult("Запрос не может быть пустым.");
                    }
                    var user = await _context.Users
                        .Include(u => u.Logins) // Включаем связанные логины
                        .FirstOrDefaultAsync(u => u.Id == request.Id);

                    if (user == null)
                    {
                        _logger.LogWarning("Пользователь с id {UserId} не найден.", request.Id);
                        return new NotFoundObjectResult("Пользователь не найден.");
                    }
                    _logger.LogInformation("Получены данные для обновления: Name={Name}, Description={Description}, Password={Password}",
                        request.Name, request.Description, request.Password);

                    user.Name = request.Name;
                    user.Description = request.Description;

                    var login = user.Logins.FirstOrDefault();

                    if (login != null)
                    {
                        login.Password = request.Password;
                        _context.Logins.Update(login);
                    }
                    else
                    {
                        _logger.LogWarning("Логин для пользователя с id {UserId} не найден.", request.Id);
                        return new BadRequestObjectResult("Логин не найден.");
                    }
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Данные пользователя с id {UserId} успешно обновлены.", request.Id);
                    return new OkObjectResult("Данные пользователя успешно обновлены.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при обновлении данных пользователя с id {UserId}.", request?.Id);
                    return new BadRequestObjectResult("Произошла внутренняя ошибка сервера.");
                }
        }

        public async Task<IActionResult> ChangeUserRole(ChangeRoleAndDeleteRequest request)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == request.UserId);

                if (user == null)
                {
                    return new NotFoundObjectResult(new { Message = "Пользователь не найден" });
                }

                var adminRole = await _context.Role.FirstOrDefaultAsync(r => r.Name == "Администратор");
                if (adminRole == null)
                {
                    return new BadRequestObjectResult(new { Message = "Роль 'Администратор' не найдена" });
                }
                user.Role = adminRole;
                await _context.SaveChangesAsync();

                return new OkObjectResult(new { Message = "Роль пользователя успешно изменена" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при изменении роли пользователя");
                return new BadRequestObjectResult(new { Message = "Произошла ошибка на сервере." });
            }
        }

        public async Task<IActionResult> DeleteUser(ChangeRoleAndDeleteRequest request)
        {
            try
            {
                // Находим пользователя по ID
                var user = await _context.Users.FindAsync(request.UserId);
                if (user == null)
                {
                    return new NotFoundObjectResult(new { Message = "Пользователь не найден" });
                }

                // Удаляем пользователя
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return new OkObjectResult(new { Message = "Пользователь успешно удален" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении пользователя");
                return new BadRequestObjectResult(new { Message = "Произошла ошибка на сервере." });
            }
        }

        public class ChangeRoleAndDeleteRequest
        {
            public int UserId { get; set; }
        }
        public class UserDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Role { get; set; }
            public string? Email { get; set; }
            public List<string>? Logins { get; set; } 
        }
    }
}