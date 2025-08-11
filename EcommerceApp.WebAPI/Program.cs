using ECommerceApp.WebAPI.Data;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.SwaggerUI;
// var builder = WebApplication.CreateBuilder(args);
// builder.Services.AddOpenApi();
// builder.Services.AddDbContext<ApplicationDbContext>(opts =>
//     opts.UseNpgsql(
//         builder.Configuration.GetConnectionString("DefaultConnection"),
//         npgsqlOpts => npgsqlOpts.CommandTimeout(60)
//     )
// );
// var app = builder.Build();
// if (app.Environment.IsDevelopment())
// {
//     app.MapOpenApi();
// }
// app.UseHttpsRedirection();
// app.Run();
 
 
var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = false;
    })
    .AddJsonOptions(options =>
    {
        // Use property names as defined in C# models
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        // .NET 9 improvements for JSON serialization
        options.JsonSerializerOptions.TypeInfoResolverChain.Clear();
        options.JsonSerializerOptions.TypeInfoResolverChain.Add(System.Text.Json.Serialization.Metadata.JsonTypeInfoResolver.Combine());
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure Entity Framework Core with PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.CommandTimeout(60);
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        });
    
    // .NET 9 EF Core optimizations
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Add health checks (recommended for production apps)
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "ECommerce API V1");
    });
}

// Security headers for production
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// Add security headers middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    await next();
});

app.UseAuthorization();

app.MapControllers();

// Map health checks
app.MapHealthChecks("/health");

app.Run();
