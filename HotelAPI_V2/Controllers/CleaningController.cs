using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace HotelAPI_V2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CleaningController : ControllerBase
    {
        private readonly string _connStr;

        public CleaningController(IConfiguration cfg)
        {
            _connStr = cfg.GetConnectionString("Pg")
             ?? throw new InvalidOperationException("缺少 ConnectionStrings:Pg");
        }
        // ===== DTO =====
        // DTO：Swagger 送進來的格式要長這樣
        public class CleaningAreaStatusDto
        {
            public string AreaCode { get; set; } = "";
            public string AreaName { get; set; } = "";
            public string Status { get; set; } = "";      // PENDING / DONE / REWORK
            public string? CleanerName { get; set; }
        }

        public class RoomCleaningStatusDto
        {
            public string RoomNumber { get; set; } = "";
            public List<CleaningAreaStatusDto> Areas { get; set; } = new();
        }
        public class CleaningUpdateRequest
        {
            public string RoomNumber { get; set; } = "";
            public string AreaCode { get; set; } = "";
            public string Status { get; set; } = "";      // TODO / DOING / DONE
            public int? CleanerId { get; set; }           // 可空
            public string? CleanerName { get; set; }      // 可空
        }
        // ===============================
        // 1) 取得某一天所有房間的清潔狀態
        // GET /api/cleaning/daily?date=2025-12-06
        // ===============================
        [HttpGet("daily")]
        public async Task<IActionResult> GetDailyStatus([FromQuery] DateTime? date)
        {
            var day = (date ?? DateTime.Today).Date;

            var roomsDict = new Dictionary<string, RoomCleaningStatusDto>();

            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            // 說明：
            // cleaning_areas 定義每個房間有哪些區域
            // cleaning_status 是每天的實際狀態
            // LEFT JOIN，沒有清潔紀錄時 status 會是 NULL → 我們在 C# 端當成 PENDING
            var sql = @"
    select
        ca.room_number,
        ca.area_code,
        ca.area_name,
        cs.status,
        cs.cleaner_name
    from cleaning_areas ca
    left join cleaning_status cs
        on ca.room_number = cs.room_number
       and ca.area_code   = cs.area_code
       and cs.cleaning_date::date = @p_date
    where ca.is_active = true
    order by ca.room_number, ca.area_code;
            ";

            await using (var cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.Add("p_date", NpgsqlTypes.NpgsqlDbType.Date).Value = day;

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var roomNumber = reader.GetString(0);
                    var areaCode = reader.GetString(1);
                    var areaName = reader.GetString(2);
                    var status = reader.IsDBNull(3) ? "PENDING" : reader.GetString(3);
                    var cleaner = reader.IsDBNull(4) ? null : reader.GetString(4);

                    if (!roomsDict.TryGetValue(roomNumber, out var roomDto))
                    {
                        roomDto = new RoomCleaningStatusDto
                        {
                            RoomNumber = roomNumber
                        };
                        roomsDict[roomNumber] = roomDto;
                    }

                    roomDto.Areas.Add(new CleaningAreaStatusDto
                    {
                        AreaCode = areaCode,
                        AreaName = areaName,
                        Status = status,
                        CleanerName = cleaner
                    });
                }
            }

            var result = roomsDict.Values.OrderBy(r => r.RoomNumber).ToList();
            return Ok(result);
        }

        // POST /api/Cleaning/update
        [HttpPost("update")]
        public async Task<IActionResult> UpdateCleaningStatus([FromBody] CleaningUpdateRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.RoomNumber))
                return BadRequest("RoomNumber 必填");

            if (string.IsNullOrWhiteSpace(req.AreaCode))
                return BadRequest("AreaCode 必填");

            if (string.IsNullOrWhiteSpace(req.Status))
                return BadRequest("Status 必填");

            // 你目前的 enum 值（可以依你實際 enum 調整）
            var allowed = new[] { "TODO", "DOING", "DONE" };
            if (!allowed.Contains(req.Status))
                return BadRequest("Status 必須是 TODO / DOING / DONE 其中之一");
            var localDay = DateTime.Today.Date;      // 台灣時間的今天
            var nowLocal = DateTime.Now;             // 本機時間的現在
            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            // ⚠ 注意：欄位名都改成 room_number / area_code / cleaner_id / cleaner_name / updated_at
            // cleaning_date 我這邊直接用 CURRENT_DATE 當今天日期
            const string sql = @"
INSERT INTO public.cleaning_status (
    room_number,
    area_code,
    cleaning_date,
    status,
    cleaner_id,
    cleaner_name,
    updated_at
)
VALUES (
    @roomNumber,
    @areaCode,
    @cleaningDate,
    @status::cleaning_status_enum,
    @cleanerId,
    @cleanerName,
    @updatedAt
)
ON CONFLICT (room_number, area_code, cleaning_date)
DO UPDATE SET
    status       = @status::cleaning_status_enum,
    cleaner_id   = @cleanerId,
    cleaner_name = @cleanerName,
    updated_at   = NOW();
";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@roomNumber", req.RoomNumber);
            cmd.Parameters.AddWithValue("@areaCode", req.AreaCode);
            cmd.Parameters.AddWithValue("@status", req.Status);
            cmd.Parameters.Add("cleaningDate", NpgsqlTypes.NpgsqlDbType.Date).Value = localDay;
            cmd.Parameters.Add("updatedAt", NpgsqlTypes.NpgsqlDbType.Timestamp).Value = nowLocal;


            if (req.CleanerId.HasValue)
                cmd.Parameters.AddWithValue("@cleanerId", req.CleanerId.Value);
            else
                cmd.Parameters.AddWithValue("@cleanerId", DBNull.Value);

            cmd.Parameters.AddWithValue("@cleanerName",
                (object?)req.CleanerName ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();

            return Ok(new
            {
                Message = "清潔狀態已更新",
                req.RoomNumber,
                req.AreaCode,
                CleaningDate = DateTime.Today.ToString("yyyy/MM/dd"),
                req.Status
            });
        }
    }
}
