using Microsoft.AspNetCore.Mvc;
using HotelAPI_V2.Models;
using HotelAPI_V2.Services;

namespace HotelAPI_V2.Controllers
{
    [ApiController]
    [Route("api/cleaning-qr")]
    public class CleaningQrController : ControllerBase
    {
        private readonly CleaningQrService _svc;

        public CleaningQrController(CleaningQrService svc)
        {
            _svc = svc;
        }

        [HttpPost("complete")]
        public async Task<IActionResult> Complete([FromBody] QrCompleteRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.RoomNumber) || string.IsNullOrWhiteSpace(req.AreaCode))
                return BadRequest("RoomNumber / AreaCode 必填");

            var result = await _svc.UpsertDoneAsync(
                req.RoomNumber.Trim(),
                req.AreaCode.Trim(),
                req.CleanerId,
                req.CleanerName
                        );

            return Ok(result);
        }
    }
}
