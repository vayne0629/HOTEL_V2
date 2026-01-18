using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;

namespace HotelAPI_V2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoomsController : ControllerBase
    {
        private readonly string _connStr;

        public RoomsController(IConfiguration cfg)
        {
            _connStr = cfg.GetConnectionString("Pg")
                ?? throw new InvalidOperationException("缺少 ConnectionStrings:Pg");
        }
        public class RoomDto
        {
            public int Id { get; set; }
            public string RoomNumber { get; set; } = "";
            public string RoomType { get; set; } = "";
            public int? Floor { get; set; }
            public string? Note { get; set; }
            public bool IsActive { get; set; }
        }
        // 取得全部房間（可先只拿 is_active = true）
        [HttpGet]
        public async Task<ActionResult<List<RoomDto>>> GetAllAsync()
        {
            var list = new List<RoomDto>();

            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            var sql = @"
                SELECT id, room_number, room_type, floor, note, is_active
                FROM rooms
                ORDER BY room_number;
            ";

            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new RoomDto
                {
                    Id = reader.GetInt32(0),
                    RoomNumber = reader.GetString(1),
                    RoomType = reader.GetString(2),
                    Floor = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    Note = reader.IsDBNull(4) ? null : reader.GetString(4),
                    IsActive = reader.GetBoolean(5)
                });
            }

            return Ok(list);
        }

        // 新增房間
        [HttpPost]
        public async Task<ActionResult<RoomDto>> CreateAsync([FromBody] RoomDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RoomNumber) ||
                string.IsNullOrWhiteSpace(dto.RoomType))
            {
                return BadRequest("房號與房型必填");
            }

            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            var sql = @"
                INSERT INTO rooms (room_number, room_type, floor, note, is_active)
                VALUES (@room_number, @room_type, @floor, @note, @is_active)
                RETURNING id;
            ";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("room_number", dto.RoomNumber);
            cmd.Parameters.AddWithValue("room_type", dto.RoomType);
            cmd.Parameters.AddWithValue("floor", (object?)dto.Floor ?? DBNull.Value);
            cmd.Parameters.AddWithValue("note", (object?)dto.Note ?? DBNull.Value);
            cmd.Parameters.AddWithValue("is_active", dto.IsActive);

            var newId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            dto.Id = newId;

            return CreatedAtAction(nameof(GetAllAsync), new { id = newId }, dto);
        }

        // 更新房間
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateAsync(int id, [FromBody] RoomDto dto)
        {
            if (id <= 0) return BadRequest("Id 錯誤");

            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            var sql = @"
                UPDATE rooms
                SET room_number = @room_number,
                    room_type   = @room_type,
                    floor       = @floor,
                    note        = @note,
                    is_active   = @is_active
                WHERE id = @id;
            ";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("room_number", dto.RoomNumber);
            cmd.Parameters.AddWithValue("room_type", dto.RoomType);
            cmd.Parameters.AddWithValue("floor", (object?)dto.Floor ?? DBNull.Value);
            cmd.Parameters.AddWithValue("note", (object?)dto.Note ?? DBNull.Value);
            cmd.Parameters.AddWithValue("is_active", dto.IsActive);
            cmd.Parameters.AddWithValue("id", id);

            var rows = await cmd.ExecuteNonQueryAsync();
            if (rows == 0) return NotFound();

            return NoContent();
        }

        // 停用（軟刪除）
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> SoftDeleteAsync(int id)
        {
            if (id <= 0) return BadRequest("Id 錯誤");

            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            var sql = @"UPDATE rooms SET is_active = FALSE WHERE id = @id;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);

            var rows = await cmd.ExecuteNonQueryAsync();
            if (rows == 0) return NotFound();

            return NoContent();
        }
    }
}
