using CityInfo.API;
using CityInfo.API.DbContexts;
using CityInfo.API.Services;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/cityinfo.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
//builder.Logging.ClearProviders();
//builder.Logging.AddConsole();
builder.Host.UseSerilog();

// Add services to the container.

// When consumer asks for a certain representation we don't support (406 Not Acceptable)
// And the nadd an XML formatter
builder.Services.AddControllers(opts =>
{
    opts.ReturnHttpNotAcceptable = true;
}).AddNewtonsoftJson().AddXmlDataContractSerializerFormatters();

builder.Services.AddProblemDetails();
//builder.Services.AddProblemDetails(opts =>
//{
//    opts.CustomizeProblemDetails = ctx =>
//    {
//        ctx.ProblemDetails.Extensions.Add("additionalInfo", "Additional info example");
//        ctx.ProblemDetails.Extensions.Add("server", Environment.MachineName)
//    };
//});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<FileExtensionContentTypeProvider>();

// Compiler directive
#if DEBUG
builder.Services.AddTransient<IMailService, LocalMailService>();
#else
builder.Services.AddTransient<IMailService, CloudMailService>();
#endif

builder.Services.AddSingleton<CitiesDataStore>();

// Register the DbContext for dependency injection.
builder.Services.AddDbContext<CityInfoContext>(
    dbContextOptions => dbContextOptions.UseSqlite(
        builder.Configuration["ConnectionStrings:CityInfoDBConnectionString"]));

// Our repository is best called as a scoped lifetime so it's created once per request.
builder.Services.AddScoped<ICityInfoRepository, CityInfoRepository>();

// We add AutoMapper and we tell it to retrieve the assemblies that are currently loaded in the application domain.
// An assembly is a unit of deployment such as a .dll or .exe file. In this context, GetAssemblies() retrieves all
// assemblies currently loaded in the application domain. This is used by AutoMapper to scan for types to map.
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Bearer token authentication middleware
builder.Services.AddAuthentication("Bearer").AddJwtBearer(opts =>
{
    opts.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Authentication:Issuer"], // Ensure we only accept tokens from this authority
        ValidAudience = builder.Configuration["Authentication:Audience"], // Check if the token is meant for this API
        IssuerSigningKey = new SymmetricSecurityKey(
            Convert.FromBase64String(builder.Configuration["Authentication:SecretForKey"]))
    };
});

// Add basic authorization policy:
builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("MustBeFromAntwerp", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("city", "Antwerp");
    });
});

var app = builder.Build();

///////////////////////////////////////////////////////////// Configure the HTTP request pipeline.
// This should be near or at the start of the HTTP pipeline.
if(!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// UseRouting is where the selected endpoint is first detected. 
app.UseRouting();

// 
app.UseAuthentication();

// In between UseRouting and UseEndpoints is where middlware that can detect the endpoint and do something based on it can execute (for example re-routing depending on some state)...
// For example authorization:
app.UseAuthorization();

// UseEndpoints is where the endpoints are actually exectuted:
// There are two ways of setting this up: convention based or attribute based. For APIs attribute based routing should be used.
app.UseEndpoints(endpoints =>
{
    // MapControllers adds endpoints for controller actions without specificying routes. I.e. No conventions will be applied.
    endpoints.MapControllers();
});

app.Run();
