# PlagiarismChecker

**Контрольная работа №2. Синхронное межсервисное взаимодействие**

## Описание
Платформа состоит из трёх микросервисов и шлюза API Gateway:

1. **API Gateway** (APIGateway) — принимает запросы от пользователей, маршрутизирует к FileStorageService и FileAnalysisService через [Ocelot](https://github.com/ThreeMammals/Ocelot).
2. **FileStorageService** — хранит текстовые отчёты и облака слов на диске, а метаданные (`Id`, `Name`, `Hash`, `Location`, `UploadedAt`) в PostgreSQL через EF Core.
3. **FileAnalysisService** — загружает файл из хранилища, считает: количество абзацев, слов и символов; вызывает внешний Word-Cloud API (QuickChart), сохраняет полученную картинку через FileStorageService и результаты анализа в PostgreSQL.
4. **Shared** — библиотека моделей (`FileMetadata`, `AnalysisResult`) общая для сервисов.

## Архитектура

```text
User → API Gateway (http://localhost:5020)
  ├─ POST /files         → FileStorageService (http://localhost:5062/api/files)
  ├─ GET  /files/{id}    → FileStorageService
  ├─ POST /analysis/{id} → FileAnalysisService (http://localhost:5250/api/analysis/{id})
  ├─ GET  /analysis/{id} → FileAnalysisService
  └─ GET  /analysis/cloud/{cloudId} → FileAnalysisService → FileStorageService (для картинки)
```

Все сервисы собираются в едином решении `PlagiarismChecker.sln`.

## Технологии
- C# / .NET 9
- ASP.NET Core Web API
- PostgreSQL + EF Core (Npgsql)
- Docker Compose (опционально для БД)
- Ocelot API Gateway
- QuickChart Word-Cloud API
- Swagger/OpenAPI (AddOpenApi)

## Установка и запуск

1. **PostgreSQL**
   - Локально: `brew install postgresql && brew services start postgresql` + `CREATE DATABASE plagiarism;`
   - Docker: `docker compose up -d`

2. **Миграции** (каждый сервис в своём каталоге)
   ```bash
   cd FileStorageService
   dotnet ef database update

   cd ../FileAnalysisService
   dotnet ef database update
   ```

3. **Запуск сервисов**
   ```bash
   cd FileStorageService
   dotnet run  # HTTP: 5062

   cd ../FileAnalysisService
   dotnet run  # HTTP: 5250

   cd ../APIGateway
   dotnet run  # HTTP: 5020
   ```

4. **Тестирование**
   Используйте файл `APIGateway/APIGateway.http` или `curl`:
   ```bash
   curl -F file=@report.txt http://localhost:5020/files
   curl -O http://localhost:5020/files/{id}
   curl -X POST http://localhost:5020/analysis/{id}
   curl http://localhost:5020/analysis/{id}
   curl http://localhost:5020/analysis/cloud/{cloudId} --output cloud.png
   ```

## Документация
- Swagger UI каждого сервиса:
  - http://localhost:5062/swagger
  - http://localhost:5250/swagger
  - (Gateway не объединяет UI, используйте HTTP-файл)

## Качество кода и тесты
- Модульная структура, чистый код, обработка ошибок.
- Настроено автоматизированное тестирование через GitHub Actions.
- Поддерживается покрытие тестами (текущее покрытие 85.9%, требуемый порог ≥65%).
- Настроен анализ покрытия кода через Coverlet и ReportGenerator.
- Исключены файлы миграций из анализа покрытия кода.

### Запуск тестов локально
```bash
# Запуск тестов с покрытием
cd FileStorageService.Tests
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:ExcludeByFile="**/*Migrations/*.cs"

cd ../FileAnalysisService.Tests
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:ExcludeByFile="**/*Migrations/*.cs"
```

---
Все требования по заданию реализованы: маршрутизация через API Gateway, разделение ответственности микросервисов, настройки EF Core, Swagger, внешнее API Word-Cloud и сохранение результатов.

## История изменений
История изменений проекта доступна в файле [CHANGELOG.md](CHANGELOG.md)

## Дополнительная документация
- [Руководство по CI и тестированию](TESTING.md) - подробная информация о CI, запуске тестов и анализе покрытия кода
