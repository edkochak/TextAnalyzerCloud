# Руководство по CI и тестированию

## Непрерывная интеграция (CI)

Проект использует GitHub Actions для непрерывной интеграции. При каждом push или pull request в ветки `main` или `master` выполняются следующие шаги:

1. Восстановление зависимостей
2. Сборка проекта
3. Запуск тестов с анализом покрытия кода
4. Загрузка отчетов о покрытии в Codecov
5. Генерация отчетов о покрытии
6. Проверка порога покрытия (≥65%)

### Настройка Codecov

Для успешной загрузки отчетов о покрытии кода в Codecov необходимо выполнить следующие шаги:

1. **Регистрация репозитория в Codecov**:
   - Войдите в свой аккаунт на [Codecov](https://codecov.io/)
   - Выберите "Add new repository"
   - Найдите и выберите ваш репозиторий GitHub
   - Следуйте инструкциям по подключению репозитория

2. **Настройка токена**:
   - После подключения репозитория перейдите в его настройки на Codecov
   - Найдите раздел "Repository Upload Token"
   - Скопируйте токен
   - Добавьте токен в секреты GitHub:
     - Перейдите в репозиторий на GitHub
     - Нажмите "Settings" → "Secrets and variables" → "Actions"
     - Создайте новый секрет с именем `CODECOV_TOKEN` и значением вашего токена

3. **Проверка названия репозитория**:
   - Убедитесь, что имя репозитория в Codecov совпадает с именем репозитория на GitHub
   - В CI конфигурации используется параметр `slug: ${{ github.repository }}` для правильного определения имени репозитория

## Локальное тестирование

### Запуск всех тестов

```bash
dotnet test PlagiarismChecker.sln
```

### Запуск тестов с покрытием

```bash
# Создаем директорию для отчетов
mkdir -p coverage

# FileStorageService.Tests
dotnet test FileStorageService.Tests/FileStorageService.Tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput=./coverage/fss_coverage.opencover.xml /p:ExcludeByFile="**/*Migrations/*.cs"

# FileAnalysisService.Tests
dotnet test FileAnalysisService.Tests/FileAnalysisService.Tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput=./coverage/fas_coverage.opencover.xml /p:ExcludeByFile="**/*Migrations/*.cs"
```

### Генерация HTML-отчета о покрытии

Установите инструмент ReportGenerator:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

Создайте отчет:

```bash
reportgenerator -reports:"./coverage/*.xml" -targetdir:"./coveragereport" -reporttypes:"Html;TextSummary"
```

Откройте отчет в браузере:

```bash
open ./coveragereport/index.html  # для macOS
# или
xdg-open ./coveragereport/index.html  # для Linux
# или
start ./coveragereport/index.html  # для Windows
```

## Структура тестов

1. **FileStorageService.Tests** - тесты для сервиса хранения файлов
   - Тесты контроллеров
   - Тесты сервисов
   - Тесты для Program.cs

2. **FileAnalysisService.Tests** - тесты для сервиса анализа файлов
   - Тесты контроллеров
   - Тесты сервисов
   - Тесты для Program.cs

## Пропуск миграций в тестах

Для пропуска миграций БД в тестовом режиме используется переменная окружения `SKIP_DB_MIGRATION`:

```csharp
// Пропуск миграций при тестировании
if (Environment.GetEnvironmentVariable("SKIP_DB_MIGRATION") != "true")
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
    }
}
```

## Написание новых тестов

При добавлении новых функций всегда создавайте соответствующие тесты, чтобы поддерживать высокий уровень покрытия кода.

### Пример теста контроллера:

```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    var mockService = new Mock<IYourService>();
    mockService.Setup(s => s.YourMethod()).ReturnsAsync(expectedResult);
    var controller = new YourController(mockService.Object);
    
    // Act
    var result = await controller.YourAction();
    
    // Assert
    var actionResult = Assert.IsType<ActionResult<ExpectedType>>(result);
    var value = Assert.IsType<ExpectedType>(actionResult.Value);
    Assert.Equal(expectedValue, value.Property);
}
```

### Совет по тестам с базой данных

Используйте провайдер InMemory для тестирования:

```csharp
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase(databaseName: "TestDatabase")
    .Options;

using (var context = new ApplicationDbContext(options))
{
    // Arrange - подготовка данных
    
    // Act - тестируемый метод
    
    // Assert - проверка результатов
}
```
