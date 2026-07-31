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

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IDiagnosisRepository, DiagnosisRepository>();
builder.Services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
builder.Services.AddScoped<IHerbalMedicineRepository, HerbalMedicineRepository>();
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

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDiagnosisService, DiagnosisService>();
builder.Services.AddScoped<IPrescriptionService, PrescriptionService>();
builder.Services.AddScoped<IHerbalMedicineService, HerbalMedicineService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();

builder.Services.AddMemoryCache();
builder.Services.AddScoped<ISmsService, TwilioSmsService>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<TrackingSimulationService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<TrackingSimulationService>());




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
var jwtKey = builder.Configuration["JWT:SecretKey"] ?? "TMPMS_SecretKey_For_JWT_Authentication_Secret_123456789";
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
            if (!string.IsNullOrEmpty(accessToken) && (path.StartsWithSegments("/hubs") || path.StartsWithSegments("/trackingHub")))
            {
                context.Token = accessToken;
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
        policy => policy.WithOrigins("http://localhost:5173", "https://tmpms.vercel.app", "http://127.0.0.1:5173")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var context = scope.ServiceProvider.GetRequiredService<TMPMSDbContext>();

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
                Phone = "02439342111",
                Address = "3B Quang Trung, Hoàn Kiếm, Hà Nội",
                TaxCode = "0100239102",
                Status = "Active"
            }
        );
        await context.SaveChangesAsync();
    }

    await DiagnosisSeeder.SeedAsync(context);
}
app.MapHub<TrackingHub>("/trackingHub");
app.MapHub<TMPMS.Hubs.PharmacyChatHub>("/hubs/pharmacy-chat");
app.MapControllers();

app.Run();
