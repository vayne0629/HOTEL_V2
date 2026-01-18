using Npgsql;
using HotelAPI_V2.Infrastructure;
using HotelAPI_V2.Models;

namespace HotelAPI_V2.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly string _connStr;

        public CustomerRepository(IConfiguration cfg)
        {
            _connStr = cfg.GetConnectionString("Pg")
                ?? throw new InvalidOperationException("缺少 ConnectionStrings:Pg");
        }

        // ============================
        // 1. 搜尋客人 (name / phone / idnumber / carnumber)
        // ============================
        public async Task<List<CustomerSearchResultDto>> SearchAsync(string? keyword)
        {
            var result = new List<CustomerSearchResultDto>();

            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            const string sql = @"
                SELECT id, name, phone, idnumber, carnumber1
                FROM public.customers_02
                WHERE @q = '' 
                   OR name ILIKE '%' || @q || '%'
                   OR phone ILIKE '%' || @q || '%'
                   OR idnumber ILIKE '%' || @q || '%'
                   OR carnumber1 ILIKE '%' || @q || '%'
                ORDER BY id DESC
                LIMIT 100;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@q", (object?)(keyword ?? string.Empty));

            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                result.Add(new CustomerSearchResultDto
                {
                    Id = rd.GetSafeInt64(0),
                    Name = rd.GetSafeString(1),
                    Phone = rd.GetSafeString(2),
                    IDNumber = rd.GetSafeString(3),
                    CarNumber1 = rd.GetSafeString(4)
                });
            }

            return result;
        }

        // ============================
        // 2. 依 ID 取得完整資料
        // ============================
        public async Task<CustomerDetailDto?> GetByIdAsync(long id)
        {
            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            const string sql = @"
                SELECT id, name, idnumber, dateofbirth, phone, phone2, taxid,
                       carnumber1, carnumber2, blacklist, habit, bookingsource,
                       line, blreason, company
                FROM public.customers_02
                WHERE id = @id
                LIMIT 1;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await using var rd = await cmd.ExecuteReaderAsync();
            if (!await rd.ReadAsync()) return null;

            return Map(rd);
        }

        // ============================
        // 3. 依身分證字號取資料
        // ============================
        public async Task<CustomerDetailDto?> GetByIdNumberAsync(string idNumber)
        {
            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            const string sql = @"
                SELECT id, name, idnumber, dateofbirth, phone, phone2, taxid,
                       carnumber1, carnumber2, blacklist, habit, bookingsource,
                       line, blreason, company
                FROM public.customers_02
                WHERE UPPER(idnumber) = UPPER(@idnum)
                LIMIT 1;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@idnum", idNumber);

            await using var rd = await cmd.ExecuteReaderAsync();
            if (!await rd.ReadAsync()) return null;

            return Map(rd);
        }
        public async Task<List<CustomerDetailDto>> SearchByFieldAsync(string field, string keyword)
        {
            var list = new List<CustomerDetailDto>();

            if (string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(keyword))
                return list;

            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            string sql;
            switch (field)
            {
                case "name":
                    sql = @"
                SELECT id, name, idnumber, dateofbirth, phone, phone2, taxid,
                       carnumber1, carnumber2, blacklist, habit, bookingsource,
                       line, blreason, company
                FROM public.customers_02
                WHERE name ILIKE '%' || @kw || '%'
                ORDER BY id DESC
                LIMIT 100;";
                    break;

                case "phone":
                    // 這裡前端已經保證只有 10 碼或 3 碼（純數字）
                    if (keyword.Length == 10)
                    {
                        // ✅ 手機全碼：比對「去掉 - 之後的完整號碼」
                        sql = @"
            SELECT id, name, idnumber, dateofbirth, phone, phone2, taxid,
                   carnumber1, carnumber2, blacklist, habit, bookingsource,
                   line, blreason, company
            FROM public.customers_02
            WHERE REPLACE(phone, '-', '') = @kw
            ORDER BY id DESC
            LIMIT 100;";
                    }
                    else // 3 碼 → 尾三碼
                    {
                        sql = @"
            SELECT id, name, idnumber, dateofbirth, phone, phone2, taxid,
                   carnumber1, carnumber2, blacklist, habit, bookingsource,
                   line, blreason, company
            FROM public.customers_02
            WHERE RIGHT(REPLACE(phone, '-', ''), 3) = @kw
            ORDER BY id DESC
            LIMIT 100;";
                    }
                    break;

                case "idnumber":
                    // 一樣假設前端已經驗證格式，只針對全碼 / 尾9 / 尾3
                    if (keyword.Length == 10)
                    {
                        sql = @"
                    SELECT id, name, idnumber, dateofbirth, phone, phone2, taxid,
                           carnumber1, carnumber2, blacklist, habit, bookingsource,
                           line, blreason, company
                    FROM public.customers_02
                    WHERE UPPER(idnumber) = UPPER(@kw)
                    ORDER BY id DESC
                    LIMIT 100;";
                    }
                    else if (keyword.Length == 9)
                    {
                        sql = @"
                    SELECT id, name, idnumber, dateofbirth, phone, phone2, taxid,
                           carnumber1, carnumber2, blacklist, habit, bookingsource,
                           line, blreason, company
                    FROM public.customers_02
                    WHERE RIGHT(idnumber, 9) = @kw
                    ORDER BY id DESC
                    LIMIT 100;";
                    }
                    else // 3 碼
                    {
                        sql = @"
                    SELECT id, name, idnumber, dateofbirth, phone, phone2, taxid,
                           carnumber1, carnumber2, blacklist, habit, bookingsource,
                           line, blreason, company
                    FROM public.customers_02
                    WHERE RIGHT(idnumber, 3) = @kw
                    ORDER BY id DESC
                    LIMIT 100;";
                    }
                    break;

                case "car1":
                default:
                    sql = @"
                SELECT id, name, idnumber, dateofbirth, phone, phone2, taxid,
                       carnumber1, carnumber2, blacklist, habit, bookingsource,
                       line, blreason, company
                FROM public.customers_02
                WHERE carnumber1 ILIKE '%' || @kw || '%'
                ORDER BY id DESC
                LIMIT 100;";
                    break;
            }

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@kw", keyword);

            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                list.Add(Map(rd)); // 用你原本的 Map
            }

            return list;
        }

        // ============================
        // 共用 Mapping
        // ============================
        private static CustomerDetailDto Map(NpgsqlDataReader rd)
        {
            return new CustomerDetailDto
            {
                Id = rd.GetSafeInt64(0),
                Name = rd.GetSafeString(1),
                IDNumber = rd.GetSafeString(2),
                DateOfBirth = rd.GetSafeString(3),
                Phone = rd.GetSafeString(4),
                Phone2 = rd.GetSafeString(5),
                TaxID = rd.GetSafeString(6),
                CarNumber1 = rd.GetSafeString(7),
                CarNumber2 = rd.GetSafeString(8),
                Blacklist = rd.GetSafeBool(9),
                Habit = rd.GetSafeString(10),
                BookingSource = rd.GetSafeString(11),
                Line = rd.GetSafeBool(12),
                BLReason = rd.GetSafeString(13),
                Company = rd.GetSafeString(14)
            };
        }
    }
}
