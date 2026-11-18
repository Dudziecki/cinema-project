# CinemaProject — ЛР №4, №5 и №6 (вариант 9)

Учебная система кинотеатра на .NET 10, ASP.NET Core Minimal API, EF Core, PostgreSQL и Blazor Server.

## 1. Что входит в проект

- `Cinema.Api` — REST API, JWT-аутентификация, роли `Admin` и `Cashier`, EF Core и пример ADO.NET.
- `Cinema.Contracts` — DTO, которые используют API и Blazor-клиент.
- `Cinema.Blazor` — интерфейс просмотра фильмов/сеансов, CRUD фильмов для администратора и оформление билетов.
- `Cinema.Tests` — пять unit-тестов сервиса фильмов и один интеграционный тест API.
- `Cinema.Api/Dockerfile`, `Cinema.Blazor/Dockerfile`, `docker-compose.yml` — контейнеризация.
- `.github/workflows/ci.yml` — сборка, тесты и публикация двух образов в GitHub Container Registry.

## 2. Требования к компьютеру

1. .NET SDK 10.0.x.
2. PostgreSQL 15 или новее либо Docker Desktop.
3. Rider или VS Code с C# Dev Kit. IntelliJ IDEA не является полноценной .NET IDE; Visual Studio устанавливать не требуется.
4. Git — для отправки проекта в GitHub и запуска CI/CD.

Проверить SDK можно командой:

```powershell
dotnet --list-sdks
dotnet --info
```

Если SDK 10 отсутствует, установите .NET 10 SDK с официального сайта: https://dotnet.microsoft.com/download/dotnet/10.0

## 3. Открытие проекта

1. Распакуйте архив в папку, например `D:\Bsuir\CinemaProject`.
2. Откройте `CinemaProject.sln` в Rider. В VS Code откройте корневую папку проекта.
3. Откройте встроенный терминал IDE в корневой папке `CinemaProject`.
4. Выполните:

```powershell
dotnet restore CinemaProject.sln
dotnet build CinemaProject.sln
```

Команды ниже выполняются из корневой папки решения, если не указано иное.

## 4. PostgreSQL для локального запуска

Можно использовать установленный PostgreSQL и pgAdmin.

1. Откройте pgAdmin и подключитесь к локальному серверу PostgreSQL.
2. Откройте Query Tool базы `postgres`.
3. Выполните SQL (замените пароль на свой):

```sql
CREATE USER cinema_user WITH PASSWORD 'CinemaLocal_12345!';
CREATE DATABASE cinema_db OWNER cinema_user;
```

4. В корне проекта откройте терминал и настройте User Secrets для API:

```powershell
dotnet user-secrets init --project Cinema.Api
```

Сохраните строку подключения. Подставьте пароль, заданный при `CREATE USER`:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=cinema_db;Username=cinema_user;Password=CinemaLocal_12345!" --project Cinema.Api
```

Создайте секрет JWT. Он не должен попадать в Git:

```powershell
$jwt = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
dotnet user-secrets set "Jwt:Key" $jwt --project Cinema.Api
```

`Issuer` и `Audience` уже заданы в `appsettings.json`. В `Program.cs` настройки привязываются к `JwtOptions` через `IOptions<JwtOptions>`.

## 5. Миграции EF Core (ЛР №4)

Установите инструмент EF Core CLI:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.15
```

Если `dotnet-ef` уже установлен, обновите:

```powershell
dotnet tool update --global dotnet-ef --version 10.0.15
```

Создайте начальную миграцию:

```powershell
dotnet ef migrations add InitialCreate --project Cinema.Api --startup-project Cinema.Api --output-dir Data/Migrations
```

Примените её к PostgreSQL:

```powershell
dotnet ef database update --project Cinema.Api --startup-project Cinema.Api
```

Проверьте в pgAdmin, что появились таблицы `movies`, `genres`, `movie_genres`, `halls`, `projection_systems`, `screenings`, `tickets`, `app_users` и `__EFMigrationsHistory`.

Для демонстрации удаления последней миграции (только если она ещё не применена к базе):

```powershell
dotnet ef migrations remove --project Cinema.Api --startup-project Cinema.Api
```

После этого при необходимости создайте миграцию заново.

При первом запуске API `DbSeeder` добавит тестовые жанры, фильмы, два зала, один сеанс и двух пользователей.

## 6. ADO.NET и EF Core (что показывать преподавателю)

Основной доступ к данным выполняется через EF Core в `CinemaDbContext`, `EfMovieRepository` и `MovieService`.

Отдельный пример прямого ADO.NET находится в `Cinema.Api/Services/AdoNetTicketRepository.cs`. В нём используются `DbConnection`, `DbCommand`, `DbParameter`, `DbDataReader` и `DbTransaction`. SQL содержит параметры `@screening_id`, `@seat_number` и т. д., а продажа билета выполняется транзакционно: проверка сеанса/места → INSERT → COMMIT. При ошибке выполняется ROLLBACK.

В `CinemaDbContext.OnModelCreating` показаны связи:

- one-to-many: `Movie` → `Screening`, `Hall` → `Screening`, `Screening` → `Ticket`;
- many-to-many: `Movie` ↔ `Genre` через `MovieGenre`;
- one-to-one: `Hall` ↔ `ProjectionSystem`.

`EfMovieRepository` использует `Include` для жанров вместе с фильмами (избегаем N+1), `AsNoTracking` для чтения, `FindAsync` + `Remove` для обычного удаления, `ExecuteUpdateAsync` для изменения года и `ExecuteDeleteAsync` для массового удаления старых фильмов без сеансов (`DELETE /api/movies/bulk-old?beforeYear=1980`). Для обычного изменения используется отслеживаемая сущность и `SaveChangesAsync` — так можно показать `ChangeTracker`.

## 7. Запуск API и Blazor локально (ЛР №5 и часть ЛР №6)

Сначала запустите API в первом терминале:

```powershell
dotnet run --project Cinema.Api --urls http://localhost:5000
```

Откройте Swagger: http://localhost:5000/swagger

Во втором терминале запустите клиент:

```powershell
dotnet run --project Cinema.Blazor --urls http://localhost:5001
```

Откройте интерфейс: http://localhost:5001

Тестовые пользователи после первичного заполнения БД:

- администратор: `admin` / `Admin123!`;
- кассир: `cashier` / `Cashier123!`.

Администратор может создавать/изменять/удалять фильмы и создавать сеансы. Кассир может просматривать фильмы и сеансы, а также оформлять билеты. Удаление фильма запрещено, если для него уже создан сеанс.

Токен хранится в `AuthSession` как scoped-состояние Blazor Server. При полной перезагрузке вкладки потребуется снова войти. Это сознательное упрощение для учебной работы, не схема хранения токенов для production.

## 8. Проверка тестов (ЛР №6)

Из корня решения:

```powershell
dotnet test CinemaProject.sln
```

В проекте есть пять unit-тестов `MovieService` (создание, валидация названия, валидация длительности, обновление отсутствующего фильма, удаление) и интеграционный тест `GET /api/health` через `WebApplicationFactory<Program>`.

Интеграционный тест не требует запущенного PostgreSQL: он проверяет запуск ASP.NET Core и прохождение запроса через тестовый сервер. Операции с БД проверяются локально через приложение и PostgreSQL.

## 9. Docker: запуск всей системы

Docker нужен только на этом этапе; до него API и Blazor можно запускать обычным `dotnet run`.

1. Установите Docker Desktop для Windows: https://www.docker.com/products/docker-desktop/.
2. Запустите Docker Desktop и дождитесь статуса **Engine running**.
3. Убедитесь, что в терминале работают команды:

```powershell
docker --version
docker compose version
```

4. В корне проекта скопируйте пример переменных:

```powershell
Copy-Item .env.example .env
```

5. Откройте `.env` и измените оба значения. `POSTGRES_PASSWORD` — пароль БД. `JWT_KEY` — длинный случайный секрет; не публикуйте `.env` и не добавляйте его в Git.
6. Убедитесь, что миграция `InitialCreate` создана и папка `Cinema.Api/Data/Migrations` присутствует в проекте. Это обязательно: контейнер API при старте выполняет `Database.MigrateAsync()`.
7. Из корня проекта соберите образы и запустите три контейнера:

```powershell
docker compose up --build -d
```

Что происходит:

- `db` запускает PostgreSQL и хранит данные в постоянном volume `cinema_pgdata`;
- `api` собирается через multi-stage Dockerfile, ждёт готовности БД, применяет миграции и заполняет начальные данные;
- `blazor` запускает веб-клиент. Внутри Docker он обращается к API по адресу `http://api:8080` — это имя сервиса в сети Compose.

8. Проверьте состояние контейнеров:

```powershell
docker compose ps
```

9. Посмотрите логи, если приложение не стартовало:

```powershell
docker compose logs -f db
docker compose logs -f api
docker compose logs -f blazor
```

10. Откройте:

- Blazor: http://localhost:5001
- Swagger API: http://localhost:5000/swagger
- PostgreSQL с хоста (например, из pgAdmin): `localhost`, порт `5433`, пользователь `cinema_user`, БД `cinema_db`. Внутри сети Docker API подключается к `db:5432`.

11. Остановить контейнеры, сохранив данные PostgreSQL:

```powershell
docker compose down
```

12. Удалить контейнеры и том с базой (данные будут потеряны):

```powershell
docker compose down -v
```

Для пересборки после изменений в коде:

```powershell
docker compose up --build -d
```

## 10. GitHub Actions: CI/CD (ЛР №6)

1. Создайте пустой репозиторий на GitHub, например `cinema-project`.
2. В корне проекта выполните:

```powershell
git init
git add .
git commit -m "Initial cinema project"
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/cinema-project.git
git push -u origin main
```

Замените `YOUR_USERNAME` на свой логин GitHub.

3. Откройте репозиторий на GitHub → вкладка **Actions**. Workflow находится в `.github/workflows/ci.yml`.
4. При `push` в ветку `main` и при Pull Request запускается job:
   - checkout — получает исходный код;
   - setup-dotnet — устанавливает .NET 10 SDK;
   - restore — скачивает NuGet-зависимости;
   - build — компилирует решение;
   - test — запускает 6 тестов;
   - Docker Buildx — собирает образы API и Blazor;
   - на push в `main` выполняется вход в GHCR и публикация образов `cinema-api` и `cinema-blazor`.

5. Вкладка **Actions** покажет зелёный статус, когда все шаги завершатся успешно. Если шаг красный, откройте его и смотрите первую ошибку в логах.
6. Опубликованные образы ищите в GitHub → профиль → **Packages**. Для публикации используется автоматически предоставляемый `GITHUB_TOKEN`; отдельный пароль в репозитории создавать не надо.

## 11. Что продемонстрировать на защите

1. Таблицы и внешние ключи PostgreSQL, миграцию `InitialCreate`.
2. CRUD фильмов через EF Core и фильтрацию/пагинацию `GET /api/movies?search=...&page=1&pageSize=10&sortBy=year&descending=true`. Варианты `sortBy`: `title`, `year`, `duration`. Конфигурация JWT привязана через `IOptions<JwtOptions>`, логи сервиса пишутся через `ILogger<MovieService>`.
3. `Include`, `AsNoTracking`, `FindAsync`, `Remove`, `ExecuteUpdateAsync`, `ExecuteDeleteAsync` в `EfMovieRepository`.
4. Продажу билета через ADO.NET в `AdoNetTicketRepository`, параметры запроса и транзакцию.
5. Swagger: вход в систему и защищённые методы. Для Admin и Cashier используются разные роли.
6. Blazor: вход, CRUD фильмов администратором, просмотр сеансов и оформление билета кассиром.
7. Вывод `dotnet test` с пятью unit-тестами и одним integration test.
8. `docker compose ps` с тремя работающими контейнерами.
9. Успешный workflow GitHub Actions и опубликованные Docker-образы.
