using HotelAPI_V2.Repositories;
using HotelAPI_V2.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DI
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<CleaningQrService>();

var app = builder.Build();

// ===== 基本健康檢查 =====
app.MapGet("/", () => Results.Text("HOTEL_V2 API is running", "text/plain"));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// ===== Swagger（先全部開，確定沒問題）=====
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
