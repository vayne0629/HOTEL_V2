using Microsoft.AspNetCore.Mvc;
using HotelAPI_V2.Models;
using HotelAPI_V2.Services;
using Npgsql;

namespace HotelAPI_V2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _service;
        private readonly string _connStr;

        public BookingsController(IBookingService service, IConfiguration cfg)
        {
            _service = service;
            _connStr = cfg.GetConnectionString("Pg")
                ?? throw new InvalidOperationException("缺少 ConnectionStrings:Pg");
        }
        // POST /api/bookings
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BookingCreateRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.IDNumber))
                return BadRequest("IDNumber 必填");

            if (string.IsNullOrWhiteSpace(req.CheckInDate))
                return BadRequest("CheckInDate 必填（yyyy-MM-dd）");

            var id = await _service.CreateAsync(req);

            if (id == null)
                return BadRequest("找不到該身分證對應的客人，無法新增入住紀錄");

            return Ok(new { Id = id.Value, Message = "入住紀錄新增成功" });
        }
        [HttpGet("history-by-customer")]
        public async Task<IActionResult> GetHistoryByCustomerId([FromQuery] long customerId)
        {
            if (customerId <= 0)
                return BadRequest("請提供正確的 customerId");

            var list = new List<BookingHistoryDto>();

            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            const string sql = @"
        SELECT id,
               customerid,
               checkindate,
               roomnumber,
               bookingsource,
               bookingname,
               amount
        FROM public.bookings_02
        WHERE customerid = @cid
          AND (isdeleted = FALSE OR isdeleted IS NULL)
        ORDER BY checkindate DESC, id DESC
        LIMIT 200;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cid", customerId);

            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                var dto = new BookingHistoryDto
                {
                    Id = rd.GetInt64(0),
                    CustomerId = rd.GetInt64(1),
                    CheckInDate = rd.IsDBNull(2)
                        ? ""
                        : rd.GetFieldValue<DateTime>(2).ToString("yyyy/MM/dd"),
                    RoomNumber = rd.IsDBNull(3) ? "" : rd.GetString(3),
                    BookingSource = rd.IsDBNull(4) ? "" : rd.GetString(4),
                    BookingName = rd.IsDBNull(5) ? "" : rd.GetString(5),
                    Amount = rd.IsDBNull(6) ? 0 : rd.GetDecimal(6)
                };
                list.Add(dto);
            }

            return Ok(list);
        }
        public class BookingSoftDeleteRequest
        {
            public string DeletedBy { get; set; } = "";
        }
        // POST /api/bookings/{id}/soft-delete
        [HttpPost("{id:long}/soft-delete")]
        public async Task<IActionResult> SoftDelete(long id, [FromBody] BookingSoftDeleteRequest req)
        {
            if (id <= 0)
                return BadRequest("id 不合法。");

            if (string.IsNullOrWhiteSpace(req.DeletedBy))
                return BadRequest("DeletedBy 不可空白。");

            var ok = await _service.SoftDeleteAsync(id, req.DeletedBy);

            if (!ok)
                return NotFound("找不到這筆入住紀錄。");

            return NoContent(); // 204 成功
        }
    }
}
