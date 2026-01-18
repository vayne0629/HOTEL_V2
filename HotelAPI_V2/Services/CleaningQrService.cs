using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
namespace HotelAPI_V2.Services
{
    public class CleaningQrService
    {
        private readonly HttpClient _http;
        private readonly string _url;
        private readonly string _serviceKey;
        private readonly string _table;

        public CleaningQrService(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _url = cfg["Supabase:Url"] ?? "";
            _serviceKey = cfg["Supabase:ServiceRoleKey"] ?? "";
            _table = cfg["Supabase:Table"] ?? "";
        }

        public async Task<object> UpsertDoneAsync(string roomNumber, string areaCode, long? cleanerId, string? cleanerName)
        {
            if (string.IsNullOrWhiteSpace(_url) || string.IsNullOrWhiteSpace(_serviceKey) || string.IsNullOrWhiteSpace(_table))
                throw new InvalidOperationException("Supabase Url/ServiceRoleKey/Table 設定未完成");

            // 台灣今天日期（cleaning_date 用台灣日期，避免跨日錯誤）
            var taipeiNow = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8));
            var cleaningDate = taipeiNow.Date;

            // ✅ 你的表格欄位：room_number / area_code / cleaning_date / status / cleaner_id / cleaner_name / updated_at
            var payload = new[]
            {
                new {
                    room_number = roomNumber,
                    area_code = areaCode,
                    cleaning_date = cleaningDate.ToString("yyyy-MM-dd"),
                    status = "DONE", // 如果你原本不是用 done，這行改成你系統使用的完成值
                    cleaner_id = cleanerId,                 // long? 直接丟
                    cleaner_name = string.IsNullOrWhiteSpace(cleanerName) ? null : cleanerName,
                    updated_at = DateTimeOffset.UtcNow
                }
            };

            var json = JsonSerializer.Serialize(payload);

            // ✅ on_conflict 依照你的 unique index 欄位
            var endpoint = $"{_url}/rest/v1/{_table}?on_conflict=cleaning_date,room_number,area_code";

            var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
            req.Headers.Add("apikey", _serviceKey);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceKey);
            req.Headers.Add("Prefer", "resolution=merge-duplicates,return=representation");
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Supabase Upsert 失敗: {resp.StatusCode}\n{body}");

            return new { ok = true, roomNumber, areaCode, cleaningDate = cleaningDate.ToString("yyyy-MM-dd"), supabase = body };
        }
    }
}
