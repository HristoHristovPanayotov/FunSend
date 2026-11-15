# FunSend database

The database is built Code First from `FunSendDbContext` (project
`FunSend.Data`) on MS SQL Server; LocalDB is used while developing.
`schema.sql` in this folder is the readable reference for the documentation
and is kept in step with the context by hand.

## Entity relationship diagram

```mermaid
erDiagram
    AspNetUsers  ||--o{ Videos        : uploads
    AspNetUsers  ||--o{ Comments      : writes
    AspNetUsers  ||--o{ Likes         : gives
    AspNetUsers  ||--o{ SavedVideos   : saves
    AspNetUsers  ||--o{ Follows       : follows
    AspNetUsers  ||--o{ VideoViews    : watches
    AspNetUsers  ||--o{ Notifications : receives
    AspNetUsers  ||--o{ Reports       : files

    Categories   ||--o{ Videos        : groups

    Videos       ||--o{ Comments      : has
    Videos       ||--o{ Likes         : has
    Videos       ||--o{ SavedVideos   : has
    Videos       ||--o{ VideoHashtags : tagged_with
    Videos       ||--o{ VideoViews    : has
    Videos       ||--o{ Reports       : reported_in

    Hashtags     ||--o{ VideoHashtags : used_in
    Comments     ||--o{ Reports       : reported_in

    AspNetUsers {
        nvarchar Id PK
        nvarchar Email
        nvarchar DisplayName
        nvarchar Bio
        nvarchar AvatarFileName
        datetime2 CreatedAt
        bit IsBlocked
    }

    Categories {
        int Id PK
        nvarchar Name UK
    }

    Videos {
        int Id PK
        nvarchar Title
        nvarchar Description
        nvarchar StoredFileName
        bigint FileSizeBytes
        int DurationSeconds
        int Status
        int Visibility
        datetime2 CreatedAt
        nvarchar UserId FK
        int CategoryId FK
    }

    Comments {
        int Id PK
        nvarchar Text
        datetime2 CreatedAt
        bit IsRemoved
        int VideoId FK
        nvarchar UserId FK
    }

    Likes {
        int VideoId PK
        nvarchar UserId PK
        datetime2 CreatedAt
    }

    SavedVideos {
        int VideoId PK
        nvarchar UserId PK
        datetime2 CreatedAt
    }

    Follows {
        nvarchar FollowerId PK
        nvarchar FollowingId PK
        datetime2 CreatedAt
    }

    Hashtags {
        int Id PK
        nvarchar Name UK
    }

    VideoHashtags {
        int VideoId PK
        int HashtagId PK
    }

    VideoViews {
        bigint Id PK
        datetime2 ViewedAt
        int VideoId FK
        nvarchar UserId FK
    }

    Notifications {
        int Id PK
        int Type
        nvarchar Message
        bit IsRead
        datetime2 CreatedAt
        nvarchar UserId FK
        nvarchar ActorId FK
        int VideoId FK
    }

    Reports {
        int Id PK
        nvarchar Reason
        int Status
        datetime2 CreatedAt
        datetime2 ResolvedAt
        int VideoId FK
        int CommentId FK
        nvarchar ReporterId FK
    }
```

## Decisions worth noting

**Users are never cascade-deleted.** A user is reachable from almost every
table, and SQL Server rejects a schema with more than one cascade path into
the same table. Every foreign key to `AspNetUsers` is therefore `NO ACTION`.
Rows that only make sense together with their video cascade from the video
instead.

**Duplicates are impossible in the database.** `Likes`, `SavedVideos`,
`Follows` and `VideoHashtags` use composite primary keys, so a second like or
a repeated follow fails before any service-layer check runs.

**Enumerations are stored as `int`** with the mapping written next to each
table in `schema.sql`.

**Moderation keeps history.** A video taken down gets `Status = Removed` and
a comment gets `IsRemoved = 1`; neither row is deleted.

**Check constraints** cover a positive duration and file size, the ban on
following oneself, and a report that targets either a video or a comment but
never both.

## Working with migrations

Run from the repository root, with `FunSend.Web` as the start-up project:

```bash
dotnet ef migrations add <Name> --project src/FunSend.Data --startup-project src/FunSend.Web
dotnet ef database update --project src/FunSend.Data --startup-project src/FunSend.Web
```
