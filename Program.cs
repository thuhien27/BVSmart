using BenhvienSmart.Data;
using BenhvienSmart.Models;
using BenhvienSmart.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

// --- 1. ĐĂNG KÝ CÁC DỊCH VỤ (PHẢI TRƯỚC BUILDER.BUILD) ---

builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson(options =>
    {
        // Chống lỗi vòng lặp cho Newtonsoft
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    });

// THÊM DÒNG NÀY: Cấu hình cho cả bộ mặc định System.Text.Json để dự phòng
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.AddHttpClient();

builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options =>
    {
        options.Cookie.Name = "UserAuth";
        options.LoginPath = "/Home/Login"; // Đổi Account thành Home
        options.AccessDeniedPath = "/Home/AccessDenied"; // Đổi Account thành Home
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("DoctorOrAdmin", policy => policy.RequireRole("Admin", "Doctor"));
});

// Cấu hình Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<ISchedulingService, SchedulingService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<IAIService, AIService>();

// --- 2. XÂY DỰNG ỨNG DỤNG ---
var app = builder.Build();

// --- 3. CẤU HÌNH PIPELINE (THỨ TỰ RẤT QUAN TRỌNG) ---

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

// THỨ TỰ BẮT BUỘC: Authentication -> Authorization -> Session
app.UseSession(); // Đưa Session lên đầu để các bước sau có dữ liệu
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

// --- 4. CHẠY ỨNG DỤNG ---
app.Run();