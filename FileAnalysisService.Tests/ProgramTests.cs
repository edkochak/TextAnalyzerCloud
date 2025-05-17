using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Net;
using Microsoft.EntityFrameworkCore;
using FileAnalysisService.Data;

namespace FileAnalysisService.Tests;

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

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });

                // Замена HttpClient для тестирования
                var httpClientDescriptors = services.Where(
                    d => d.ServiceType.Name.Contains("HttpClient")).ToList();

                foreach (var d in httpClientDescriptors)
                {
                    services.Remove(d);
                }

                services.AddHttpClient("FileStorage", client =>
                {
                    client.BaseAddress = new Uri("http://localhost:5000");
                });

                services.AddHttpClient("WordCloud", client =>
                {
                    client.BaseAddress = new Uri("http://localhost:5001");
                });
            });

            // Отключаем автоматическую миграцию базы данных
            Environment.SetEnvironmentVariable("SKIP_DB_MIGRATION", "true");

            return base.CreateHost(builder);
        }
    }

    [Fact]
    public async Task Application_StartsWithoutException()
    {
        // Arrange & Act
        await using var application = new TestWebApplicationFactory();
        using var client = application.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert - проверяем только то, что приложение запустилось
        // Ответ может быть 404, так как у нас нет маршрута для корня, 
        // но важно, что приложение не выбросило исключение при запуске
        Assert.True(response.StatusCode == HttpStatusCode.NotFound);
    }
}
