using Npgsql;
using HotelAPI_V2.Models;

namespace HotelAPI_V2.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly string _connStr;

        public BookingRepository(IConfiguration cfg)
        {
            _connStr = cfg.GetConnectionString("Pg")
                ?? throw new InvalidOperationException("缺少 ConnectionStrings:Pg");
        }

        public async Task<long?> CreateAsync(BookingCreateRequest req)
        {
            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            if (!DateTime.TryParse(req.CheckInDate, out var checkInDate))
                throw new Exception("CheckInDate 格式錯誤，必須 yyyy-MM-dd");

            const string sql = @"
                INSERT INTO public.bookings_02
                    (customerid, checkindate, roomnumber, bookingsource, bookingname, amount)
                VALUES
                    ((SELECT id FROM public.customers_02 WHERE UPPER(idnumber)=UPPER(@idnum)),
                     @checkInDate, @room, @source, @name, @amount)
                RETURNING id;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@idnum", req.IDNumber);
            cmd.Parameters.AddWithValue("@checkInDate", checkInDate);
            cmd.Parameters.AddWithValue("@room", (object?)req.RoomNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@source", (object?)req.BookingSource ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@name", (object?)req.BookingName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@amount", req.Amount);

            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value)
                return null;

            return Convert.ToInt64(result);
        }
        public async Task<bool> SoftDeleteAsync(long id, string deletedBy)
        {
            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            var sql = @"
UPDATE public.bookings_02
SET 
    isdeleted = TRUE,
    deletedby = @deletedby,
    deletedat = NOW() AT TIME ZONE 'Asia/Taipei'
WHERE id = @id;
";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@deletedby", deletedBy);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}
