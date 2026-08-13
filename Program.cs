using BusinessObjects;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Repositories.Interfaces;
using Services.Interfaces;
using System.Text;
using System.Text.Json.Serialization;
using TMPMS.Data;
using TMPMS.Repositories;
using TMPMS.Repositories.Interfaces;
using TMPMS.Services;
using TMPMS.Services.Interfaces;
using TMPMS.Hubs;
using TMPMS.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IDiagnosisRepository, DiagnosisRepository>();
builder.Services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
builder.Services.AddScoped<IHerbalMedicineRepository, HerbalMedicineRepository>();
builder.Services.AddScoped<IHerbalInteractionRepository, HerbalInteractionRepository>();
builder.Services.AddScoped<IWarehouseRepository, WarehouseRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>(); 
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<IPrescriptionItemRepository, PrescriptionItemRepository>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<INewsArticleRepository, NewsArticleRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IMedicineRepository, MedicineRepository>();
builder.Services.AddScoped<IVoucherRepository, VoucherRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDiagnosisService, DiagnosisService>();
builder.Services.AddScoped<IPrescriptionService, PrescriptionService>();
builder.Services.AddScoped<IHerbalMedicineService, HerbalMedicineService>();
builder.Services.AddScoped<IHerbalInteractionService, HerbalInteractionService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IHealthQuizService, HealthQuizService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<INewsArticleService, NewsArticleService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<ILoyaltyService, LoyaltyService>();
builder.Services.AddScoped<IHealthReelsService, HealthReelsService>();
builder.Services.AddScoped<IShippingFeeService, ShippingFeeService>();
builder.Services.AddScoped<IMeridianAnalysisService, MeridianAnalysisService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IMedicineService, MedicineService>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddScoped<IWheelService, WheelService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICartItemService, CartItemService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPrescriptionOcrService, PrescriptionOcrService>();
builder.Services.AddScoped<IMedicineImageSearchService, MedicineImageSearchService>();
builder.Services.AddHttpClient();

builder.Services.AddMemoryCache();
builder.Services.AddScoped<ISmsService, ZaloZnsService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddSignalR();

builder.Services.AddSingleton<TrackingSimulationService>();

builder.Services.AddHostedService(provider => provider.GetRequiredService<TrackingSimulationService>());
builder.Services.AddHostedService<AppointmentStatusBackgroundService>();

builder.Services.AddDbContext<TMPMSDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnectionString"));
});

// Identity
builder.Services.AddIdentity<User, Role>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<TMPMSDbContext>()
.AddDefaultTokenProviders();

// JWT & Authentication
var jwtKey = builder.Configuration["JWT:SecretKey"]
    ?? throw new InvalidOperationException("JWT:SecretKey is not configured.");
var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"] ?? "TMPMS_BE",
        ValidAudience = builder.Configuration["JWT:Audience"] ?? "TMPMS_FE",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && (path.StartsWithSegments("/hubs") || path.StartsWithSegments("/trackingHub") || path.StartsWithSegments("/api/hubs") || path.StartsWithSegments("/api/trackingHub")))
            {
                context.Token = accessToken;
            }
            // Nếu không có Authorization header (client cũ dùng Bearer header vẫn hoạt động bình
            // thường), thử lấy token từ httpOnly cookie do AuthController phát ra khi đăng nhập.
            else if (string.IsNullOrEmpty(context.Token) && context.Request.Headers.Authorization.Count == 0)
            {
                var cookieToken = context.Request.Cookies["access_token"];
                if (!string.IsNullOrEmpty(cookieToken))
                {
                    context.Token = cookieToken;
                }
            }
            return Task.CompletedTask;
        }
    };
});

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
    });
}

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
    });

IConfiguration configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", true, true).Build();

builder.Services.AddEndpointsApiExplorer();

//builder.Services
//    .AddAuthentication(x =>
//    {
//        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//    })
//    .AddJwtBearer(x =>
//    {
//        x.SaveToken = true;
//        x.TokenValidationParameters = new TokenValidationParameters
//        {
//            ValidateIssuer = true,
//            ValidateAudience = true,
//            ValidateLifetime = true,
//            ValidateIssuerSigningKey = true,
//            ValidIssuer = configuration["JWT:Issuer"],
//            ValidAudience = configuration["JWT:Audience"],
//            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:SecretKey"]))
//        };
//    });

builder.Services.AddSwaggerGen(c =>
{
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Name = "JWT Authentication",
        Description = "JWT Authentication for TMPMS System",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    };

    c.AddSecurityDefinition("Bearer", jwtSecurityScheme);

    var securityRequirement = new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    };

    c.AddSecurityRequirement(securityRequirement);
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin",
        policy => policy.RequireRole("Admin"));

    options.AddPolicy("Pharmacy",
        policy => policy.RequireRole("Pharmacy"));

    options.AddPolicy("User",
        policy => policy.RequireRole("User"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy.WithOrigins("http://localhost:5173", "http://localhost:5174", "https://tmpms.vercel.app", "http://127.0.0.1:5173", "http://localhost",
                                    "https://tmpms.io.vn", "https://www.tmpms.io.vn",
                                    "https://localhost:64647")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
});

var app = builder.Build();

// Global exception handler: log the full exception server-side, return a sanitized message to the client.
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async context =>
    {
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (feature?.Error is Exception ex)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Unhandled exception for {Path}", context.Request.Path);
        }
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { message = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau." });
    });
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    await next();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Serve static files (uploads/medicines) với đường dẫn tuyệt đối.
// Ưu tiên source wwwroot vì ảnh upload được ghi vào đó (WebRootPath khi chạy dotnet run).
var sourceWwwroot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
var uploadsPhysicalPath = Directory.Exists(sourceWwwroot)
    ? sourceWwwroot
    : Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location)!, "wwwroot");
if (!Directory.Exists(uploadsPhysicalPath))
    Directory.CreateDirectory(uploadsPhysicalPath);

// Serve ở CẢ HAI dạng đường dẫn (có và không /api) — reverse proxy production chỉ
// forward /api/* sang backend, còn dev local/khác lại gọi không /api. Đừng đổi RequestPath
// của 1 trong 2 block dưới đây; nếu cần đổi, hãy thêm block mới, không sửa/xoá cái cũ,
// để tránh vòng lặp "sửa rồi bị đổi ngược" đã xảy ra nhiều lần.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPhysicalPath),
    RequestPath = ""
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPhysicalPath),
    RequestPath = "/api"
});

// Disabled for free local hosting behind Cloudflare tunnel (HTTP upstream).
// app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var context = scope.ServiceProvider.GetRequiredService<TMPMSDbContext>();
    var db = scope.ServiceProvider.GetRequiredService<TMPMSDbContext>();

    db.Database.Migrate();

    // Roles
    var roles = new List<Role>
    {
        new Role { Name = "Admin", Description = "System Administrator" },
        new Role { Name = "Pharmacy", Description = "Pharmacy Staff" },
        new Role { Name = "User", Description = "Customer" },
        new Role { Name = "Staff", Description = "Clinic Staff" },
        new Role { Name = "Doctor", Description = "Doctor / Physician" },
        new Role { Name = "Accountant", Description = "Accountant" },
        new Role { Name = "Warehouse", Description = "Warehouse Staff" }
    };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role.Name))
        {
            await roleManager.CreateAsync(role);
        }
    }

    // Default demo accounts (weak, well-known passwords) — Development only.
    // On production these must be created/rotated manually with real credentials.
    if (app.Environment.IsDevelopment())
    {
        // Admin
        if (await userManager.FindByEmailAsync("admin@tmpms.com") == null)
        {
            var admin = new User
            {
                UserName = "admin",
                Email = "admin@tmpms.com",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await userManager.CreateAsync(admin, "Admin@123");
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        // Pharmacy
        if (await userManager.FindByEmailAsync("pharmacy@tmpms.com") == null)
        {
            var pharmacy = new User
            {
                UserName = "pharmacy",
                Email = "pharmacy@tmpms.com",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await userManager.CreateAsync(pharmacy, "Pharmacy@123");
            await userManager.AddToRoleAsync(pharmacy, "Pharmacy");
        }

        // User
        if (await userManager.FindByEmailAsync("user@tmpms.com") == null)
        {
            var user = new User
            {
                UserName = "user",
                Email = "user@tmpms.com",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await userManager.CreateAsync(user, "User@123");
            await userManager.AddToRoleAsync(user, "User");
        }
    }

    // Vouchers
    if (!context.Vouchers.Any())
    {
        context.Vouchers.AddRange(
            new Voucher
            {
                Code = "HEALTH10",
                Name = "Giảm 10% đơn hàng sức khỏe",
                DiscountType = "percent",
                DiscountValue = 10,
                MinOrderValue = 100000,
                MaxDiscount = 50000,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(3),
                UsageLimit = 100,
                UsedCount = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Voucher
            {
                Code = "FREESHIP",
                Name = "Giảm giá vận chuyển 20K",
                DiscountType = "flat",
                DiscountValue = 20000,
                MinOrderValue = 150000,
                MaxDiscount = 20000,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(3),
                UsageLimit = 100,
                UsedCount = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Voucher
            {
                Code = "DISCOUNT50",
                Name = "Giảm giá trực tiếp 50K",
                DiscountType = "flat",
                DiscountValue = 50000,
                MinOrderValue = 500000,
                MaxDiscount = 50000,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(3),
                UsageLimit = 50,
                UsedCount = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        );
        await context.SaveChangesAsync();
    }

    // Suppliers
    if (!context.Suppliers.Any())
    {
        context.Suppliers.AddRange(
            new Supplier
            {
                CompanyName = "Công ty Dược Liệu Trung Ương 1",
                ContactPerson = "Nguyễn Văn Hùng",
                Email = "lh@duoclieutw1.vn",
                Phone = "02438254123",
                Address = "Số 138 Giảng Võ, Ba Đình, Hà Nội",
                TaxCode = "0100108921",
                Status = "Active"
            },
            new Supplier
            {
                CompanyName = "Tập đoàn Y Dược Bảo Long",
                ContactPerson = "Trần Thị Mai",
                Email = "contact@baolongpharm.com",
                Phone = "02839201199",
                Address = "KCN Tân Bình, Tân Phú, TP.HCM",
                TaxCode = "0302198421",
                Status = "Active"
            },
            new Supplier
            {
                CompanyName = "Viện Dược Liệu Đông Y Việt Nam",
                ContactPerson = "Lê Hoàng Nam",
                Email = "info@vienduoclieu.org.vn",
                Phone = "02438521999",
                Address = "Số 3B Quang Trung, Hoàn Kiếm, Hà Nội",
                TaxCode = "0100293812",
                Status = "Active"
            }
        );
        await context.SaveChangesAsync();
    }

    // Pharmacy Stores Seeding
    if (!context.Stores.Any())
    {
        context.Stores.AddRange(
            // Hà Nội
            new PharmacyStore { ProvinceId = "hn", ProvinceName = "Hà Nội", DistrictId = "cg", DistrictName = "Quận Cầu Giấy", Name = "Nhà thuốc FPT Long Châu 102 Cầu Giấy", Address = "102 Cầu Giấy, Quan Hoa, Cầu Giấy, Hà Nội", Latitude = 21.036214, Longitude = 105.795412, Phone = "1800 6928", Hours = "07:00 - 22:00", IsActive = true },
            new PharmacyStore { ProvinceId = "hn", ProvinceName = "Hà Nội", DistrictId = "cg", DistrictName = "Quận Cầu Giấy", Name = "Nhà thuốc FPT Long Châu 24 Xuân Thủy", Address = "24 Xuân Thủy, Dịch Vọng Hậu, Cầu Giấy, Hà Nội", Latitude = 21.036812, Longitude = 105.786521, Phone = "1800 6928", Hours = "07:00 - 22:00", IsActive = true },
            new PharmacyStore { ProvinceId = "hn", ProvinceName = "Hà Nội", DistrictId = "dd", DistrictName = "Quận Đống Đa", Name = "Nhà thuốc FPT Long Châu 54 Chùa Bộc", Address = "54 Chùa Bộc, Quang Trung, Đống Đa, Hà Nội", Latitude = 21.007621, Longitude = 105.828412, Phone = "1800 6928", Hours = "06:30 - 22:30", IsActive = true },
            new PharmacyStore { ProvinceId = "hn", ProvinceName = "Hà Nội", DistrictId = "hk", DistrictName = "Quận Hoàn Kiếm", Name = "Nhà thuốc FPT Long Châu 9 Hai Bà Trưng", Address = "9 Hai Bà Trưng, Tràng Tiền, Hoàn Kiếm, Hà Nội", Latitude = 21.023512, Longitude = 105.852412, Phone = "1800 6928", Hours = "07:00 - 22:00", IsActive = true },

            // TP. Hồ Chí Minh
            new PharmacyStore { ProvinceId = "hcm", ProvinceName = "TP. Hồ Chí Minh", DistrictId = "q1", DistrictName = "Quận 1", Name = "Nhà thuốc FPT Long Châu 12 Nguyễn Huệ", Address = "12 Nguyễn Huệ, Bến Nghé, Quận 1, Hồ Chí Minh", Latitude = 10.774512, Longitude = 106.704212, Phone = "1800 6928", Hours = "24/7 (Mở cả đêm)", IsActive = true },
            new PharmacyStore { ProvinceId = "hcm", ProvinceName = "TP. Hồ Chí Minh", DistrictId = "q1", DistrictName = "Quận 1", Name = "Nhà thuốc FPT Long Châu 85 Trần Hưng Đạo", Address = "85 Trần Hưng Đạo, Cô Giang, Quận 1, Hồ Chí Minh", Latitude = 10.767512, Longitude = 106.692412, Phone = "1800 6928", Hours = "07:00 - 22:00", IsActive = true },

            // Đà Nẵng (Dữ liệu thật với tọa độ lat/lng chính xác)
            new PharmacyStore { ProvinceId = "dn", ProvinceName = "Đà Nẵng", DistrictId = "hc", DistrictName = "Quận Hải Châu", Name = "Nhà thuốc FPT Long Châu 52 Nguyễn Văn Linh", Address = "52 Nguyễn Văn Linh, Phường Nam Dương, Quận Hải Châu, Đà Nẵng", Latitude = 16.060783, Longitude = 108.217316, Phone = "1800 6928", Hours = "07:00 - 22:00", IsActive = true },
            new PharmacyStore { ProvinceId = "dn", ProvinceName = "Đà Nẵng", DistrictId = "tk", DistrictName = "Quận Thanh Khê", Name = "Nhà thuốc FPT Long Châu 112 Điện Biên Phủ", Address = "112 Điện Biên Phủ, Phường Chính Gián, Quận Thanh Khê, Đà Nẵng", Latitude = 16.065842, Longitude = 108.204561, Phone = "1800 6928", Hours = "07:00 - 22:00", IsActive = true },
            new PharmacyStore { ProvinceId = "dn", ProvinceName = "Đà Nẵng", DistrictId = "tk", DistrictName = "Quận Thanh Khê", Name = "Nhà thuốc Pharmacity 340 Hùng Vương", Address = "340 Hùng Vương, Phường Vĩnh Trung, Quận Thanh Khê, Đà Nẵng", Latitude = 16.067341, Longitude = 108.212450, Phone = "1800 6821", Hours = "06:00 - 23:30", IsActive = true },
            new PharmacyStore { ProvinceId = "dn", ProvinceName = "Đà Nẵng", DistrictId = "tk", DistrictName = "Quận Thanh Khê", Name = "Nhà thuốc An Khang 456 Lê Duẩn", Address = "456 Lê Duẩn, Phường Chính Gián, Quận Thanh Khê, Đà Nẵng", Latitude = 16.068912, Longitude = 108.208123, Phone = "1900 1572", Hours = "06:30 - 22:00", IsActive = true },
            new PharmacyStore { ProvinceId = "dn", ProvinceName = "Đà Nẵng", DistrictId = "hc", DistrictName = "Quận Hải Châu", Name = "Nhà thuốc Phước Thọ (Đông Y & Tây Y)", Address = "188 Đường 3 Tháng 2, Phường Thuận Phước, Quận Hải Châu, Đà Nẵng", Latitude = 16.084530, Longitude = 108.219842, Phone = "0236 3822 559", Hours = "07:00 - 21:30", IsActive = true },
            new PharmacyStore { ProvinceId = "dn", ProvinceName = "Đà Nẵng", DistrictId = "nhs", DistrictName = "Quận Ngũ Hành Sơn", Name = "Nhà thuốc FPT Long Châu 235 Ngũ Hành Sơn", Address = "235 Ngũ Hành Sơn, Phường Mỹ An, Quận Ngũ Hành Sơn, Đà Nẵng", Latitude = 16.046124, Longitude = 108.240518, Phone = "1800 6928", Hours = "07:00 - 22:00", IsActive = true }
        );
        await context.SaveChangesAsync();
    }

    // News articles
    if (!context.NewsArticles.Any())
    {
        context.NewsArticles.AddRange(
            new NewsArticle
            {
                Title = "5 loại dược liệu tăng đề kháng mùa giao mùa",
                Excerpt = "Đông trùng hạ thảo, linh chi, nhân sâm... giúp cơ thể thích nghi tốt hơn khi thời tiết thay đổi.",
                Content = "Mùa giao mùa là thời điểm hệ miễn dịch dễ suy yếu. Các dược liệu như đông trùng hạ thảo, linh chi đỏ, nhân sâm Hàn Quốc từ lâu đã được y học cổ truyền sử dụng để bồi bổ nguyên khí, tăng cường sức đề kháng. Nên dùng đều đặn, đúng liều lượng và tham khảo ý kiến thầy thuốc trước khi kết hợp nhiều loại dược liệu cùng lúc.",
                Tag = "Dinh dưỡng",
                ImageUrl = "",
                PublishedDate = DateTime.UtcNow.AddDays(-3),
                IsActive = true
            },
            new NewsArticle
            {
                Title = "Chế độ ăn hỗ trợ người mới ốm dậy",
                Excerpt = "Ưu tiên món dễ tiêu, bổ sung đạm và vi chất để cơ thể phục hồi nhanh hơn.",
                Content = "Sau khi ốm dậy, hệ tiêu hoá còn yếu nên ưu tiên cháo, súp, canh hầm dễ tiêu. Bổ sung đạm từ thịt nạc, trứng, đậu; kết hợp rau củ giàu vitamin để tăng sức đề kháng. Nên chia nhỏ bữa ăn trong ngày thay vì ăn quá no một lúc.",
                Tag = "Dinh dưỡng",
                ImageUrl = "",
                PublishedDate = DateTime.UtcNow.AddDays(-7),
                IsActive = true
            },
            new NewsArticle
            {
                Title = "Phòng ngừa cảm cúm khi thời tiết chuyển lạnh",
                Excerpt = "Giữ ấm cơ thể, bổ sung vitamin C và tiêm phòng đầy đủ là cách phòng bệnh hiệu quả.",
                Content = "Thời tiết chuyển lạnh là điều kiện thuận lợi cho virus cúm phát triển. Nên giữ ấm cơ thể, đặc biệt vùng cổ và bàn chân, bổ sung vitamin C từ trái cây họ cam quýt, rửa tay thường xuyên và hạn chế tiếp xúc nơi đông người khi có dịch.",
                Tag = "Phòng chữa bệnh",
                ImageUrl = "",
                PublishedDate = DateTime.UtcNow.AddDays(-2),
                IsActive = true
            },
            new NewsArticle
            {
                Title = "Dấu hiệu cảnh báo cần đi khám sớm",
                Excerpt = "Sốt kéo dài, đau ngực, khó thở là những triệu chứng không nên chủ quan.",
                Content = "Nhiều bệnh lý nguy hiểm khởi phát âm thầm. Khi có các dấu hiệu như sốt kéo dài trên 3 ngày, đau tức ngực, khó thở, sụt cân không rõ nguyên nhân, người bệnh nên đi khám sớm thay vì tự điều trị tại nhà để tránh biến chứng.",
                Tag = "Phòng chữa bệnh",
                ImageUrl = "",
                PublishedDate = DateTime.UtcNow.AddDays(-10),
                IsActive = true
            },
            new NewsArticle
            {
                Title = "Chăm sóc da mùa hanh khô bằng thảo dược",
                Excerpt = "Mật ong, tinh dầu tràm, lô hội là những nguyên liệu tự nhiên giúp da mềm mịn.",
                Content = "Mùa hanh khô khiến da dễ mất nước, nứt nẻ. Các nguyên liệu tự nhiên như mật ong rừng, lô hội, tinh dầu tràm có tác dụng dưỡng ẩm, làm dịu da. Nên kết hợp uống đủ nước và dưỡng ẩm đều đặn mỗi ngày.",
                Tag = "Khỏe đẹp",
                ImageUrl = "",
                PublishedDate = DateTime.UtcNow.AddDays(-5),
                IsActive = true
            },
            new NewsArticle
            {
                Title = "Ngủ đủ giấc — bí quyết trẻ hoá từ bên trong",
                Excerpt = "Giấc ngủ chất lượng giúp cơ thể tái tạo tế bào và cải thiện sắc vóc tự nhiên.",
                Content = "Ngủ đủ 7-8 tiếng mỗi đêm giúp cơ thể sản sinh collagen tự nhiên, cải thiện làn da và tinh thần. Nên hạn chế dùng thiết bị điện tử trước khi ngủ và giữ phòng ngủ thoáng mát để có giấc ngủ sâu.",
                Tag = "Khỏe đẹp",
                ImageUrl = "",
                PublishedDate = DateTime.UtcNow.AddDays(-1),
                IsActive = true
            }
        );
        await context.SaveChangesAsync();
    }

    if (!context.Warehouses.Any())
    {
        context.Warehouses.Add(new Warehouse
        {
            Name = "Kho Tổng Dược Liệu & Thuốc Đông Y",
            Address = "Số 138 Giảng Võ, Ba Đình, Hà Nội"
        });
        await context.SaveChangesAsync();
    }

    await DiagnosisSeeder.SeedAsync(context);
    await TMPMS.Database.HealthQuizSeeder.SeedAsync(context);
    await HerbalMedicineSeeder.SeedAsync(context);
    await HerbalInteractionSeeder.SeedAsync(context);
    await DiverseProductSeeder.SeedAsync(context);
    await SampleDataSeeder.SeedAsync(context);
}
// Map ở cả 2 dạng đường dẫn (có và không /api) — reverse proxy production chỉ forward
// /api/* sang backend, dev local lại gọi không /api. Xem comment ở UseStaticFiles phía trên.
app.MapHub<TrackingHub>("/trackingHub");
app.MapHub<TrackingHub>("/api/trackingHub");
app.MapHub<TMPMS.Hubs.PharmacyChatHub>("/hubs/pharmacy-chat");
app.MapHub<TMPMS.Hubs.PharmacyChatHub>("/api/hubs/pharmacy-chat");
app.MapControllers();

// SPA fallback: serve index.html for client-side routes served from wwwroot.
app.MapFallbackToFile("index.html");

app.Run();
