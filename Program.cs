using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using VoiceInfo.Data;
using VoiceInfo.IService;
using VoiceInfo.Models;
using VoiceInfo.Services;
using CloudinaryDotNet;                    // Added for Cloudinary
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc; // Added for caching

var builder = WebApplication.CreateBuilder(args);

// Add Cloudinary configuration - NEW
var cloudinaryConfig = builder.Configuration.GetSection("Cloudinary");
var cloudinary = new Cloudinary(new Account(
    cloudinaryConfig["CloudName"],
    cloudinaryConfig["ApiKey"],
    cloudinaryConfig["ApiSecret"]
));
builder.Services.AddSingleton(cloudinary);

// Add Memory Cache - NEW
builder.Services.AddMemoryCache();

// Original DbContext configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Original Identity configuration
builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Original JWT configuration
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);

builder.Services.AddAuthentication(options =>
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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// Enhanced service registration - MODIFIED
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPostService>(provider =>
    new PostService(
        provider.GetRequiredService<ApplicationDbContext>(),
        provider.GetRequiredService<UserManager<User>>(),
        provider.GetRequiredService<Cloudinary>(),
        provider.GetRequiredService<IMemoryCache>(),
        provider.GetRequiredService<IConfiguration>(),
        provider.GetRequiredService<ICategory>()
    ));
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<ICategory, CategoryService>();
builder.Services.AddScoped<ITagService, TagService>();

// Enhanced controllers with caching - MODIFIED
builder.Services.AddControllers(options =>
{
    options.CacheProfiles.Add("Default30", new CacheProfile
    {
        Duration = 30 // Cache for 30 seconds
    });
})
.AddNewtonsoftJson();

// Original CORS configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Original Swagger configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "VoiceInfo API", Version = "v1" });
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "JWT Authentication",
        Description = "Enter JWT Bearer token **_only_**",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };
    c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, new string[] { } }
    });
});

// Add Response Caching - NEW
builder.Services.AddResponseCaching();

var app = builder.Build();

// Original seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await RoleSeeder.SeedRolesAsync(services);
    await AdminSeeder.SeedAdminAsync(services);
}

// Enhanced middleware pipeline - MODIFIED
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors();
app.UseResponseCaching(); // Added for response caching
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "VoiceInfo API V1");
    c.RoutePrefix = "swagger";
});

app.MapControllers();

app.Run();