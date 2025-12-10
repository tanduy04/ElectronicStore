using ElectronicStore.Api.Services;
using ElectronicStore.Api.Data;
using ElectronicStore.Api.Service;
using ElectronicStore.Api.Service.MailService;
using Google.Apis.Http;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Net.Http.Headers;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        opt.JsonSerializerOptions.WriteIndented = true;
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ElectronicStore API", Version = "v1" });

    // Add JWT Bearer Auth
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,  // B?t bu?c ph?i là Http
        Scheme = "bearer", // ch? th??ng, quan tr?ng
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter JWT token without 'Bearer ' prefix"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddDbContext<ElectronicStoreContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("ElectronicStortConnection"));
});
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var key = Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]);
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
// //Đọc khóa Gemini từ appsettings
//var geminiApiKey = builder.Configuration["Gemini:ApiKey"];

//// Đăng ký HttpClient cho Gemini
//builder.Services.AddHttpClient("Gemini", client =>
//{
//    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/");
//    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
//});
//builder.Services.AddSingleton(new GeminiConfig { ApiKey = geminiApiKey });



// Lưu API key để dùng trong controller
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(3);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("OpenCorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
builder.Services.AddTransient<EmailService>();




//ttest Chatbot


builder.Services.AddSingleton<GeminiService>();
builder.Services.AddSingleton<QdrantService>();
builder.Services.AddSingleton<QADataService>();
builder.Services.AddScoped<RagChatbotService>();
builder.Services.AddScoped<HybridRagChatbotService>();


//Testchatbot



builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<EmailService>();
// Trong Program.cs:
// 1. Đăng ký HttpClient Factory
builder.Services.AddHttpClient("Gemini", client =>
{
    // Đặt địa chỉ cơ sở của API Gemini
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1/");
    // Hoặc chỉ cần "https://generativelanguage.googleapis.com/"
}); ;

// 2. Đăng ký cấu hình Gemini (Nếu chưa có)
builder.Services.Configure<GeminiConfig>(
    builder.Configuration.GetSection("Gemini"));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<GeminiConfig>>().Value);



// 3. Đăng ký Dịch vụ Tìm kiếm Vector (Quan trọng nhất)
builder.Services.AddScoped<IVectorSearchService, VectorSearchService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("OpenCorsPolicy");
app.UseStaticFiles();
app.UseSession();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
using (var scope = app.Services.CreateScope())
{
    var qdrantService = scope.ServiceProvider.GetRequiredService<QdrantService>();
    try
    {
        await qdrantService.InitializeCollectionAsync();
        Console.WriteLine("? Qdrant collection initialized successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"?? Warning: Could not initialize Qdrant collection: {ex.Message}");
        Console.WriteLine("Make sure Qdrant is running on Docker");
    }
}
app.Run();
public class GeminiConfig
{
    public string ApiKey { get; set; } = "";
}
