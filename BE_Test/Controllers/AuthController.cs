using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApplication1.Helpers;

namespace BE_Test.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private static List<string> _validTokens = new List<string>();

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DBAppCon");
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel model)
        {
            var user = ValidateUser(model.Username, model.Password);

            if (user != null)
            {
                var token = GenerateJwtToken(user.Username);
                _validTokens.Add(token);
                return Ok(new { Token = token, Username = user.Username });
            }

            return Unauthorized("Invalid credentials");
        }

        private User ValidateUser(string username, string password)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                var query = "SELECT username, password FROM users WHERE username = @username";
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string hashedPassword = reader["password"].ToString();
                            if (VerifyPassword(password, hashedPassword))
                            {
                                return new User
                                {
                                    Username = reader["username"].ToString()
                                };
                            }
                        }
                    }
                }
            }

            return null;
        }


        private string GenerateJwtToken(string username)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username)
            };

            var token = new JwtSecurityToken(
                _configuration["JwtSettings:Issuer"],
                _configuration["JwtSettings:Audience"],
                claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToInt32(_configuration["JwtSettings:DurationInMinutes"])),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string HashPassword(string password)
        {
            string salt = BCrypt.Net.BCrypt.GenerateSalt(12);
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, salt);
            return hashedPassword;
        }
        private bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }


        [HttpPost("logout")]
        public IActionResult Logout()
        {
            var token = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            bool success = false;
            string message = "Logout Failed";

            if (token != null && _validTokens.Contains(token))
            {
                _validTokens.Remove(token);
                success = true;
                message = "Logout Successfully";
            }

            var response = new ApiResponse<object>
            {
                Status = success,
                Message = message,
                Result = null
            };

            return new JsonResult(response);
        }

    }

    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class User
    {
        public string Username { get; set; }
    }
}