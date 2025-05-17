using FileAnalysisService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHttpClient("FileStorage", client =>
{
    var fsUrl = builder.Configuration.GetValue<string>("FileStorageService:BaseUrl")!;
    client.BaseAddress = new Uri(fsUrl);
});
builder.Services.AddHttpClient("WordCloud", client =>
{
    var wcUrl = builder.Configuration.GetValue<string>("WordCloudService:BaseUrl")!;
    client.BaseAddress = new Uri(wcUrl);
});
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Apply migrations, но пропускаем в режиме тестирования
if (Environment.GetEnvironmentVariable("SKIP_DB_MIGRATION") != "true")
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();

public partial class Program { }
