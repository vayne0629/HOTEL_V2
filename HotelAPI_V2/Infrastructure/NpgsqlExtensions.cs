using Npgsql;

namespace HotelAPI_V2.Infrastructure
{
    public static class NpgsqlExtensions
    {
        public static string GetSafeString(this NpgsqlDataReader r, int index)
        {
            if (r.IsDBNull(index)) return "";
            var value = r.GetValue(index);
            return value?.ToString() ?? "";
        }

        public static bool GetSafeBool(this NpgsqlDataReader r, int index)
        {
            if (r.IsDBNull(index)) return false;
            return r.GetBoolean(index);
        }

        public static long GetSafeInt64(this NpgsqlDataReader r, int index)
        {
            if (r.IsDBNull(index)) return 0;
            return r.GetInt64(index);
        }
    }
}
