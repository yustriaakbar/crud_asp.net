using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;
using WebApplication1.Models;
using WebApplication1.Helpers;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private static List<string> _validTokens = new List<string>();

        public UserController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public JsonResult Get(string? email = null)
        {
            string query = @"SELECT id, name, username, email FROM crud_new.users";

            if (!string.IsNullOrEmpty(email))
            {
                query += " WHERE email = @Email";
            }

            List<object> userList = new List<object>();

            string sqlDataSource = _configuration.GetConnectionString("DBAppCon");

            try
            {
                using (MySqlConnection mycon = new MySqlConnection(sqlDataSource))
                {
                    mycon.Open();
                    using (MySqlCommand myCommand = new MySqlCommand(query, mycon))
                    {
                        if (!string.IsNullOrEmpty(email))
                        {
                            myCommand.Parameters.AddWithValue("@Email", email);
                        }

                        using (var reader = myCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var user = new
                                {
                                    Id = reader["id"],
                                    Name = reader["name"],
                                    Username = reader["username"],
                                    Email = reader["email"]
                                };
                                userList.Add(user);
                            }
                        }
                    }
                }

                var response = new ApiResponse<List<object>>
                {
                    Status = true,
                    Message = null,
                    Result = userList
                };

                return new JsonResult(response);
            }
            catch (Exception ex)
            {
                var response = new ApiResponse<object>
                {
                    Status = false,
                    Message = ex.Message,
                    Result = null
                };
                return new JsonResult(response);
            }
        }

        [HttpPost]
        public JsonResult Post(User user)
        {
            string query = @"INSERT INTO crud_new.users (Name, Username, Email, Password) VALUES (@Name, @Username, @Email, @Password);";
            string sqlDataSource = _configuration.GetConnectionString("DBAppCon");
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password);

            using (MySqlConnection mycon = new MySqlConnection(sqlDataSource))
            {
                mycon.Open();
                using (MySqlCommand myCommand = new MySqlCommand(query, mycon))
                {
                    myCommand.Parameters.AddWithValue("@Name", user.Name);
                    myCommand.Parameters.AddWithValue("@Username", user.Username);
                    myCommand.Parameters.AddWithValue("@Email", user.Email);
                    myCommand.Parameters.AddWithValue("@Password", hashedPassword);

                    int rowsAffected = myCommand.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        var response = new ApiResponse<object>
                        {
                            Status = true,
                            Message = "Added Successfully",
                            Result = null
                        };
                        return new JsonResult(response);
                    }
                    else
                    {
                        var response = new ApiResponse<object>
                        {
                            Status = false,
                            Message = "Failed to Add User",
                            Result = null
                        };
                        return new JsonResult(response);
                    }
                }
            }
        }

        [HttpPut]
        public JsonResult Put(User user)
        {
            string query = @"
                UPDATE crud_new.users 
                SET Name = @Name, 
                    Username = @Username, 
                    Email = @Email 
                WHERE Id = @Id;
            ";

            DataTable table = new DataTable();
            string sqlDataSource = _configuration.GetConnectionString("DBAppCon");
            MySqlDataReader myReader;

            using (MySqlConnection mycon = new MySqlConnection(sqlDataSource))
            {
                mycon.Open();
                string checkQuery = "SELECT COUNT(*) FROM crud_new.users WHERE Id = @IdCheck";
                using (MySqlCommand checkCommand = new MySqlCommand(checkQuery, mycon))
                {
                    checkCommand.Parameters.AddWithValue("@IdCheck", user.Id);
                    int userCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                    if (userCount > 0)
                    {
                        using (MySqlCommand myCommand = new MySqlCommand(query, mycon))
                        {
                            myCommand.Parameters.AddWithValue("@Id", user.Id);
                            myCommand.Parameters.AddWithValue("@Name", user.Name);
                            myCommand.Parameters.AddWithValue("@Username", user.Username);
                            myCommand.Parameters.AddWithValue("@Email", user.Email);

                            myReader = myCommand.ExecuteReader();
                            table.Load(myReader);

                            myReader.Close();
                        }

                        var response = new ApiResponse<object>
                        {
                            Status = true,
                            Message = "Updated Successfully",
                            Result = null
                        };
                        return new JsonResult(response);
                    }
                    else
                    {
                        var response = new ApiResponse<object>
                        {
                            Status = false,
                            Message = "Invalid UserID",
                            Result = null
                        };
                        return new JsonResult(response);
                    }
                }
            }
        }

        [HttpPut("update-password")]
        public JsonResult UpdatePassword([FromBody] PasswordUpdateRequest request)
        {
            int Id = request.Id;
            string Password = request.password;
            string query = @"
                UPDATE crud_new.users 
                SET Password = @Password 
                WHERE Id = @Id;
            ";

            DataTable table = new DataTable();
            string sqlDataSource = _configuration.GetConnectionString("DBAppCon");
            MySqlDataReader myReader;

            using (MySqlConnection mycon = new MySqlConnection(sqlDataSource))
            {
                mycon.Open();
                string checkQuery = "SELECT COUNT(*) FROM crud_new.users WHERE Id = @IdCheck";
                using (MySqlCommand checkCommand = new MySqlCommand(checkQuery, mycon))
                {
                    checkCommand.Parameters.AddWithValue("@IdCheck", Id);
                    int userCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                    if (userCount > 0)
                    {
                        using (MySqlCommand myCommand = new MySqlCommand(query, mycon))
                        {
                            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(Password);
                            myCommand.Parameters.AddWithValue("@Id", Id);
                            myCommand.Parameters.AddWithValue("@Password", hashedPassword);

                            myReader = myCommand.ExecuteReader();
                            table.Load(myReader);

                            myReader.Close();
                        }

                        var response = new ApiResponse<object>
                        {
                            Status = true,
                            Message = "Password Updated Successfully",
                            Result = null
                        };
                        return new JsonResult(response);
                    }
                    else
                    {
                        var response = new ApiResponse<object>
                        {
                            Status = false,
                            Message = "Invalid UserID",
                            Result = null
                        };
                        return new JsonResult(response);
                    }
                }
            }
        }

        [HttpDelete("{id}")]
        public JsonResult Delete(int id)
        {
            string query = @"delete from crud_new.users where Id=@Id;";

            DataTable table = new DataTable();
            string sqlDataSource = _configuration.GetConnectionString("DBAppCon");
            MySqlDataReader myReader;
            using (MySqlConnection mycon = new MySqlConnection(sqlDataSource))
            {
                mycon.Open();
                using (MySqlCommand myCommand = new MySqlCommand(query, mycon))
                {
                    myCommand.Parameters.AddWithValue("@Id", id);

                    myReader = myCommand.ExecuteReader();
                    table.Load(myReader);

                    myReader.Close();
                    mycon.Close();
                }
            }
            var response = new ApiResponse<object>
            {
                Status = true,
                Message = "Deleted Successfully",
                Result = null
            };
            return new JsonResult(response);
        }
    }
    public class PasswordUpdateRequest
    {
        public int Id { get; set; }
        public string password { get; set; }
    }
}