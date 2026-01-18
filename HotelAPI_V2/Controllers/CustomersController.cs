using Microsoft.AspNetCore.Mvc;
using HotelAPI_V2.Services;
using HotelAPI_V2.Models;

namespace HotelAPI_V2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _service;

        public CustomersController(ICustomerService service)
        {
            _service = service;
        }

        // GET /api/customers/search?q=...
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string? q)
        {
            var list = await _service.SearchAsync(q);
            return Ok(list);
        }

        // GET /api/customers/{id}
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetOne(long id)
        {
            var dto = await _service.GetByIdAsync(id);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        // GET /api/customers/detail-by-idnumber?value=AA123456789
        [HttpGet("detail-by-idnumber")]
        public async Task<IActionResult> GetByIdNumber([FromQuery] string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return BadRequest("請提供身分證號");

            var dto = await _service.GetByIdNumberAsync(value);
            if (dto == null) return NotFound();
            return Ok(dto);
        }
        [HttpGet("search-by-field")]
        public async Task<IActionResult> SearchByField([FromQuery] string field, [FromQuery] string keyword)
        {
            if (string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(keyword))
                return BadRequest("field 與 keyword 不可為空");

            var list = await _service.SearchByFieldAsync(field, keyword);
            return Ok(list);
        }
        // ====== 更新客戶資料 ======

        // WinForms 那邊丟過來的 JSON 只要欄位名稱對得起來就可以
        public class CustomerUpdateRequest
        {
            public string? Name { get; set; }
            public string? IDNumber { get; set; }
            public string? DateOfBirth { get; set; }
            public string? Phone { get; set; }
            public string? Phone2 { get; set; }
            public string? TaxId { get; set; }
            public string? CarNumber1 { get; set; }
            public string? CarNumber2 { get; set; }
            public string? Company { get; set; }
            public string? Habit { get; set; }
            public string? BookingSource { get; set; }
            public string? BlReason { get; set; }
            public bool Blacklist { get; set; }
            public bool Line { get; set; }
        }

        // PUT /api/customers/{id}
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] CustomerUpdateRequest req)
        {
            if (id <= 0)
                return BadRequest("Id 不合法");

            // 呼叫 service 做實際更新（下一步會補 ICustomerService）
            var ok = await _service.UpdateAsync(id, req);

            if (!ok)
                return NotFound("找不到這個客戶");

            // 更新成功，不用回內容，回 204 就好
            return NoContent();
        }
    }
}
