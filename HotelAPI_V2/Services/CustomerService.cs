using HotelAPI_V2.Models;
using HotelAPI_V2.Repositories;
using Npgsql;
using static HotelAPI_V2.Controllers.CustomersController;

namespace HotelAPI_V2.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _repo;
        private readonly string _connStr;

        // ⭐ 只保留「一個」建構子，DI 會把 repo + cfg 都灌進來
        public CustomerService(ICustomerRepository repo, IConfiguration cfg)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _connStr = cfg.GetConnectionString("Pg")
                ?? throw new InvalidOperationException("缺少 ConnectionStrings:Pg");
        }

        public Task<List<CustomerSearchResultDto>> SearchAsync(string? keyword)
            => _repo.SearchAsync(keyword);

        public Task<CustomerDetailDto?> GetByIdAsync(long id)
            => _repo.GetByIdAsync(id);

        public Task<CustomerDetailDto?> GetByIdNumberAsync(string idNumber)
            => _repo.GetByIdNumberAsync(idNumber);

        public Task<List<CustomerDetailDto>> SearchByFieldAsync(string field, string keyword)
            => _repo.SearchByFieldAsync(field, keyword);

        // ⭐ 這裡你用 Npgsql 直接更新 DB 沒關係，還是經過 API
        public async Task<bool> UpdateAsync(long id, CustomerUpdateRequest req)
        {
            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            var sql = @"
UPDATE customers_02
SET
    name          = @name,
    idnumber      = @idnumber,
    dateofbirth   = @dob,
    phone         = @phone,
    phone2        = @phone2,
    taxid         = @taxid,
    carnumber1    = @car1,
    carnumber2    = @car2,
    company       = @company,
    habit         = @habit,
    bookingsource = @bs,
    blreason      = @bl,
    blacklist     = @blacklist,
    line          = @line
WHERE id = @id;
";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@name", (object?)req.Name ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@idnumber", (object?)req.IDNumber ?? DBNull.Value);
            if (string.IsNullOrWhiteSpace(req.DateOfBirth))
                cmd.Parameters.AddWithValue("@dob", DBNull.Value);
            else
            {
                if (DateTime.TryParse(req.DateOfBirth, out var dob))
                    cmd.Parameters.AddWithValue("@dob", dob);
                else
                    cmd.Parameters.AddWithValue("@dob", DBNull.Value); // 轉換失敗就不塞
            }
            cmd.Parameters.AddWithValue("@phone", (object?)req.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@phone2", (object?)req.Phone2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@taxid", (object?)req.TaxId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@car1", (object?)req.CarNumber1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@car2", (object?)req.CarNumber2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@company", (object?)req.Company ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@habit", (object?)req.Habit ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@bs", (object?)req.BookingSource ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@bl", (object?)req.BlReason ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@blacklist", req.Blacklist);
            cmd.Parameters.AddWithValue("@line", req.Line);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}
