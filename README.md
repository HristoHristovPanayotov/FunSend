# FunSend

An online platform for sharing short videos — diploma project, stage 1.

Registered users upload short vertical videos; everybody else watches them in
a continuous feed, likes them, follows the authors and reports what breaks the
rules. Administrators moderate the content and read the statistics.

The full design document (concept, research, database design and the plan up
to the defence on 19.04.2027) is kept separately; this repository holds the
code and the database.

## How it is put together

Three layers with one direction of dependencies, `Web -> Services -> Data`:

| Project | What is in it |
| --- | --- |
| `src/FunSend.Web` | ASP.NET Core 8 MVC: controllers, Razor views, view models, start-up and security configuration |
| `src/FunSend.Services` | the business logic: visibility, uploads, likes, follows, notifications, statistics |
| `src/FunSend.Data` | EF Core 8 Code First: entities, `FunSendDbContext`, migrations, seeding |
| `tests/FunSend.Tests` | NUnit tests of the service layer |

A controller never touches the database directly. Everything that decides
something — who may see a video, who may delete it, what counts as a valid
upload — lives in the service layer, which is why it can be tested without a
web server.

Two design patterns carry weight here. **Strategy**: video storage sits behind
`IVideoStorage`, so the local-disk implementation used for the demo can be
replaced by cloud storage without touching anything else. **Dependency
injection**: every service is registered by its interface, which is also what
lets the tests put an in-memory storage in place of the real one.

## What stage 1 contains

- Identity with the three roles (Viewer, Creator, Admin), a secure
  authentication cookie and CSRF protection on every POST
- the whole database: eighteen tables, the `InitialCreate` migration, the
  seeded roles, categories and demo accounts, and `docs/database/schema.sql`
  as the readable reference
- the services: feeds filtered by visibility, upload with format, size and
  length checks, likes, follows, saved videos, notifications and statistics
- the pages: feed, personal feed, player, upload, search, profile, saved
  videos, notifications, administrator dashboard, and custom 403/404/500 pages
- 87 unit tests, over 90% line coverage of the business layer, and a GitHub
  Actions workflow that builds, tests and fails if coverage drops under 65%

Comments, the report queue and the moderation screens belong to the later
stages of the plan.

## Running it

Needed: the .NET 8 SDK and MS SQL Server (LocalDB is enough).

```bash
dotnet restore FunSend.sln
dotnet run --project src/FunSend.Web
```

The application applies the migrations and seeds the database on start-up, so
an empty server is enough. The demo accounts are created only when a password
is configured:

```bash
dotnet user-secrets set "Seed:DemoPassword" "<a password of your choice>" --project src/FunSend.Web
```

They are then `viewer@funsend.local`, `creator@funsend.local` and
`admin@funsend.local`, one per role. In `Development` the password comes from
`appsettings.Development.json`; set `Seed:CreateDemoAccounts` to `false` for
any deployment that is not the project demo.

Uploaded files are written to `src/FunSend.Web/App_Data/videos`, outside
`wwwroot` on purpose: they are served by `Videos/Stream`, which checks the
visibility rule on every request, so a private video cannot be fetched with a
direct link.

## Configuration

| Key | Meaning |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | the database |
| `VideoUpload:MaxFileSizeBytes` | upload size limit, 100 MB by default |
| `VideoUpload:MaxDurationSeconds` | length limit, 180 s by default |
| `VideoUpload:AllowedExtensions` | `.mp4`, `.webm`, `.mov` |
| `VideoStorage:RootPath` | where the files are written |
| `Seed:CreateDemoAccounts`, `Seed:DemoPassword` | the demo accounts |

## Tests

```bash
dotnet test FunSend.sln
```

With a coverage report, as CI runs it:

```bash
dotnet test FunSend.sln --collect:"XPlat Code Coverage" --settings tests/FunSend.Tests/coverlet.runsettings
```

## Database changes

```bash
dotnet ef migrations add <Name> --project src/FunSend.Data --startup-project src/FunSend.Web
```

Keep `docs/database/schema.sql` in step with the model, since the
documentation refers to it.

## Licence

MIT, see [LICENSE](LICENSE).
