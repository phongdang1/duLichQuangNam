﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using duLichQuangNam.Models;

namespace duLichQuangNam.Controllers
{
    [ApiController]
    [Route("api/destinations")]
    public class DestinationController : ControllerBase
    {
        private readonly string _connectionString;

        public DestinationController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            List<Destination> destinations = new();

            try
            {
                using SqlConnection connection = new(_connectionString);
                connection.Open();

                string sql = @"
                    SELECT 
                        d.Id, d.Name, d.Description, d.Type, d.Location, 
                        d.Open_Time, d.Close_Time, d.Price, d.Mail, d.Deleted,
                        i.ImageId, i.EntityType, i.EntityId, i.ImgUrl, i.IsPrimary
                    FROM destination d
                    LEFT JOIN img i ON i.EntityType = 'Destination' AND i.EntityId = d.Id
                    WHERE d.deleted = 0
                    ORDER BY d.Id";

                using SqlCommand command = new(sql, connection);
                using SqlDataReader reader = command.ExecuteReader();

                Dictionary<int, Destination> destinationDict = new();

                while (reader.Read())
                {
                    int destinationId = reader.GetInt32(0);

                    if (!destinationDict.ContainsKey(destinationId))
                    {
                        var destination = new Destination
                        {
                            Id = destinationId,
                            Name = reader.GetString(1),
                            Description = reader.GetString(2),
                            Type = reader.GetString(3),
                            Location = reader.GetString(4),
                            OpenTime = reader.GetDateTime(5),
                            CloseTime = reader.GetDateTime(6),
                            Price = reader.GetDecimal(7),
                            Mail = reader.GetString(8),
                            Deleted = reader.GetBoolean(9),
                            Images = new List<Img>()
                        };
                        destinationDict.Add(destinationId, destination);
                    }

                    if (!reader.IsDBNull(11))
                    {
                        var img = new Img
                        {
                            ImageId = reader.GetInt32(10),
                            EntityType = reader.GetString(11),
                            EntityId = reader.GetInt32(12),
                            ImgUrl = reader.GetString(13),
                            IsPrimary = reader.GetBoolean(14)
                        };
                        destinationDict[destinationId].Images.Add(img);
                    }
                }

                destinations = destinationDict.Values.ToList();
                return Ok(destinations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Database error: {ex.Message}");
            }
        }

        [HttpPost]
        public IActionResult Create([FromBody] Destination dto)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                conn.Open();
                var sql = @"INSERT INTO destination
                    (Name,Description,Type,Location,OpenTime,CloseTime,Price,Mail,Deleted)
                    OUTPUT INSERTED.Id
                    VALUES (@Name,@Desc,@Type,@Loc,@Open,@Close,@Price,@Mail,0)";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Name", dto.Name);
                cmd.Parameters.AddWithValue("@Desc", dto.Description);
                cmd.Parameters.AddWithValue("@Type", dto.Type);
                cmd.Parameters.AddWithValue("@Loc", dto.Location);
                cmd.Parameters.AddWithValue("@Open", (object?)dto.OpenTime ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Close", (object?)dto.CloseTime ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Price", (object?)dto.Price ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Mail", dto.Mail);
                var newId = (int)cmd.ExecuteScalar()!;
                return Ok(newId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("delete/{id}")]
        public IActionResult SoftDelete(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                conn.Open();

                var sql = "UPDATE destination SET Deleted = 1 WHERE Id = @Id";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    return NotFound($"Không tìm thấy điểm đến với ID = {id}");
                }

                return Ok($"Đã xóa mềm điểm đến có ID = {id}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi cơ sở dữ liệu: {ex.Message}");
            }
        }


        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            try
            {
                using SqlConnection connection = new(_connectionString);
                connection.Open();

                string sql = @"
                    SELECT 
                        d.Id, d.Name, d.Description, d.Type, d.Location, 
                        d.Open_Time, d.Close_Time, d.Price, d.Mail, d.Deleted,
                        i.ImageId, i.EntityType, i.EntityId, i.ImgUrl, i.IsPrimary
                    FROM destination d
                    LEFT JOIN img i ON i.EntityType = 'Destination' AND i.EntityId = d.Id
                    WHERE d.id = @id AND d.deleted = 0";

                using SqlCommand command = new(sql, connection);
                command.Parameters.AddWithValue("@id", id);

                using SqlDataReader reader = command.ExecuteReader();

                Destination? destination = null;

                while (reader.Read())
                {
                    if (destination == null)
                    {
                        destination = new Destination
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Description = reader.GetString(2),
                            Type = reader.GetString(3),
                            Location = reader.GetString(4),
                            OpenTime = reader.GetDateTime(5),
                            CloseTime = reader.GetDateTime(6),
                            Price = reader.GetDecimal(7),
                            Mail = reader.GetString(8),
                            Deleted = reader.GetBoolean(9),
                            Images = new List<Img>()
                        };
                    }

                    if (!reader.IsDBNull(11))
                    {
                        var img = new Img
                        {
                            ImageId = reader.GetInt32(10),
                            EntityType = reader.GetString(11),
                            EntityId = reader.GetInt32(12),
                            ImgUrl = reader.GetString(13),
                            IsPrimary = reader.GetBoolean(14)
                        };
                        destination.Images.Add(img);
                    }
                }

                if (destination == null)
                {
                    return NotFound("Destination not found.");
                }

                return Ok(destination);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Database error: {ex.Message}");
            }
        }
    }
}
