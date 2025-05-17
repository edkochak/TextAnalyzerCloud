using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Net;
using Microsoft.EntityFrameworkCore;
using FileStorageService.Data;

namespace FileStorageService.Tests;

public class ProgramTests
{
    private class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Находим и удаляем регистрацию DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Находим и удаляем регистрацию DbContextPool
                var descriptorPool = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions));

                if (descriptorPool != null)
                {
                    services.Remove(descriptorPool);
                }

                // Регистрируем InMemory DbContext
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });
            });

            // Отключаем автоматическую миграцию базы данных
            Environment.SetEnvironmentVariable("SKIP_DB_MIGRATION", "true");

            return base.CreateHost(builder);
        }
    }

    [Fact]
    public async Task GetWeatherForecast_ReturnsSuccessAndForecastData()
    {
        // Arrange
        await using var application = new TestWebApplicationFactory();
        using var client = application.CreateClient();

        // Act
        var response = await client.GetAsync("/weatherforecast");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("temperature", content.ToLower());
        Assert.Contains("date", content.ToLower());
        Assert.Contains("summary", content.ToLower());
    }

    [Fact]
    public void WeatherForecast_CalculatesTemperatureF_Correctly()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Now);
        var tempC = 20;
        var summary = "Mild";

        // Act
        var forecast = new WeatherForecast(date, tempC, summary);

        // Assert
        Assert.Equal(67, forecast.TemperatureF); // 32 + (int)(20 / 0.5556) приблизительно равно 67
    }
}
