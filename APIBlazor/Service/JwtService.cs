using APIBlazor.Model;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static APIBlazor.Service.UserLoginService;

namespace APIBlazor.Service
{
    public class JwtService
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _lifespan;

        public JwtService(IConfiguration configuration)
        {
            _secretKey = configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT secret key is not configured.");
            _issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT issuer is not configured.");
            _audience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT audience is not configured.");

            var lifespanValue = configuration["Jwt:Lifespan"];
            if (string.IsNullOrEmpty(configuration["Jwt:Lifespan"]))
            {
                _lifespan = 60; //Значение по умолчанию
            }
            else if (!int.TryParse(lifespanValue, out _lifespan))
            {
                throw new InvalidOperationException("Некорректное значение для JWT Lifespan.");
            }
        }

        public string GenerateJwtToken(UserDto user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // Id пользователя
                new Claim(ClaimTypes.Name, user.Name ?? "Не указано"), // Имя пользователя
                new Claim("description", user.Description ?? "Не указано"), // Описание
                new Claim(ClaimTypes.Role, user.Role ?? "Не указано"), // Роль
                new Claim(ClaimTypes.Email, user.Email ?? "Не указано") // Email
            };

            if (user.Logins != null)
            {
                foreach (var login in user.Logins)
                {
                    claims.Add(new Claim("logins", login));
                }
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_lifespan),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
