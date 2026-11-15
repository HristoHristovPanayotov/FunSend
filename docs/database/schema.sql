/* ===========================================================================
   FunSend - reference schema (MS SQL Server)

   The database of record is created by Entity Framework Core migrations from
   FunSendDbContext. This script is the readable reference for the diploma
   documentation and a way to inspect the schema without running the
   application; keep it in step with the context when the model changes.

   Identity tables (AspNetUsers, AspNetRoles, AspNetUserRoles,
   AspNetUserClaims, AspNetUserLogins, AspNetUserTokens, AspNetRoleClaims)
   come from ASP.NET Core Identity. Only AspNetUsers is listed here, with the
   FunSend profile columns, because the others are unchanged.
   =========================================================================== */

/* --- Users ---------------------------------------------------------------- */

CREATE TABLE [AspNetUsers]
(
    [Id]                   NVARCHAR(450)  NOT NULL,
    [UserName]             NVARCHAR(256)  NULL,
    [NormalizedUserName]   NVARCHAR(256)  NULL,
    [Email]                NVARCHAR(256)  NULL,
    [NormalizedEmail]      NVARCHAR(256)  NULL,
    [EmailConfirmed]       BIT            NOT NULL,
    [PasswordHash]         NVARCHAR(MAX)  NULL,
    [SecurityStamp]        NVARCHAR(MAX)  NULL,
    [ConcurrencyStamp]     NVARCHAR(MAX)  NULL,
    [PhoneNumber]          NVARCHAR(MAX)  NULL,
    [PhoneNumberConfirmed] BIT            NOT NULL,
    [TwoFactorEnabled]     BIT            NOT NULL,
    [LockoutEnd]           DATETIMEOFFSET NULL,
    [LockoutEnabled]       BIT            NOT NULL,
    [AccessFailedCount]    INT            NOT NULL,
    /* FunSend profile columns */
    [DisplayName]          NVARCHAR(60)   NOT NULL,
    [Bio]                  NVARCHAR(400)  NULL,
    [AvatarFileName]       NVARCHAR(80)   NULL,
    [CreatedAt]            DATETIME2      NOT NULL,
    [IsBlocked]            BIT            NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);

CREATE UNIQUE INDEX [UserNameIndex]
    ON [AspNetUsers] ([NormalizedUserName])
    WHERE [NormalizedUserName] IS NOT NULL;

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
CREATE INDEX [IX_AspNetUsers_DisplayName] ON [AspNetUsers] ([DisplayName]);

/* --- Categories ----------------------------------------------------------- */

CREATE TABLE [Categories]
(
    [Id]   INT          NOT NULL IDENTITY(1, 1),
    [Name] NVARCHAR(50) NOT NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
);

CREATE UNIQUE INDEX [IX_Categories_Name] ON [Categories] ([Name]);

/* --- Videos --------------------------------------------------------------- */
/* Status:     0 = Published, 1 = Removed
   Visibility: 0 = Public,    1 = FollowersOnly, 2 = Private */

CREATE TABLE [Videos]
(
    [Id]              INT            NOT NULL IDENTITY(1, 1),
    [Title]           NVARCHAR(120)  NOT NULL,
    [Description]     NVARCHAR(1000) NULL,
    [StoredFileName]  NVARCHAR(80)   NOT NULL,
    [FileSizeBytes]   BIGINT         NOT NULL,
    [DurationSeconds] INT            NOT NULL,
    [Status]          INT            NOT NULL,
    [Visibility]      INT            NOT NULL,
    [CreatedAt]       DATETIME2      NOT NULL,
    [UserId]          NVARCHAR(450)  NOT NULL,
    [CategoryId]      INT            NOT NULL,
    CONSTRAINT [PK_Videos] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Videos_AspNetUsers_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Videos_Categories_CategoryId]
        FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [CK_Videos_DurationSeconds_Positive] CHECK ([DurationSeconds] > 0),
    CONSTRAINT [CK_Videos_FileSizeBytes_Positive]   CHECK ([FileSizeBytes] > 0)
);

CREATE INDEX [IX_Videos_CreatedAt] ON [Videos] ([CreatedAt]);
CREATE INDEX [IX_Videos_UserId_CreatedAt] ON [Videos] ([UserId], [CreatedAt]);
CREATE INDEX [IX_Videos_Status_Visibility_CreatedAt]
    ON [Videos] ([Status], [Visibility], [CreatedAt]);
CREATE INDEX [IX_Videos_CategoryId] ON [Videos] ([CategoryId]);

/* --- Comments ------------------------------------------------------------- */

CREATE TABLE [Comments]
(
    [Id]        INT           NOT NULL IDENTITY(1, 1),
    [Text]      NVARCHAR(500) NOT NULL,
    [CreatedAt] DATETIME2     NOT NULL,
    [IsRemoved] BIT           NOT NULL,
    [VideoId]   INT           NOT NULL,
    [UserId]    NVARCHAR(450) NOT NULL,
    CONSTRAINT [PK_Comments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Comments_Videos_VideoId]
        FOREIGN KEY ([VideoId]) REFERENCES [Videos] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Comments_AspNetUsers_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_Comments_VideoId_CreatedAt] ON [Comments] ([VideoId], [CreatedAt]);
CREATE INDEX [IX_Comments_UserId] ON [Comments] ([UserId]);

/* --- Likes ---------------------------------------------------------------- */

CREATE TABLE [Likes]
(
    [VideoId]   INT           NOT NULL,
    [UserId]    NVARCHAR(450) NOT NULL,
    [CreatedAt] DATETIME2     NOT NULL,
    CONSTRAINT [PK_Likes] PRIMARY KEY ([VideoId], [UserId]),
    CONSTRAINT [FK_Likes_Videos_VideoId]
        FOREIGN KEY ([VideoId]) REFERENCES [Videos] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Likes_AspNetUsers_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_Likes_UserId] ON [Likes] ([UserId]);

/* --- Saved videos --------------------------------------------------------- */

CREATE TABLE [SavedVideos]
(
    [VideoId]   INT           NOT NULL,
    [UserId]    NVARCHAR(450) NOT NULL,
    [CreatedAt] DATETIME2     NOT NULL,
    CONSTRAINT [PK_SavedVideos] PRIMARY KEY ([VideoId], [UserId]),
    CONSTRAINT [FK_SavedVideos_Videos_VideoId]
        FOREIGN KEY ([VideoId]) REFERENCES [Videos] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SavedVideos_AspNetUsers_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_SavedVideos_UserId_CreatedAt] ON [SavedVideos] ([UserId], [CreatedAt]);

/* --- Follows -------------------------------------------------------------- */

CREATE TABLE [Follows]
(
    [FollowerId]  NVARCHAR(450) NOT NULL,
    [FollowingId] NVARCHAR(450) NOT NULL,
    [CreatedAt]   DATETIME2     NOT NULL,
    CONSTRAINT [PK_Follows] PRIMARY KEY ([FollowerId], [FollowingId]),
    CONSTRAINT [FK_Follows_AspNetUsers_FollowerId]
        FOREIGN KEY ([FollowerId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Follows_AspNetUsers_FollowingId]
        FOREIGN KEY ([FollowingId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [CK_Follows_NoSelfFollow] CHECK ([FollowerId] <> [FollowingId])
);

CREATE INDEX [IX_Follows_FollowingId] ON [Follows] ([FollowingId]);

/* --- Hashtags ------------------------------------------------------------- */

CREATE TABLE [Hashtags]
(
    [Id]   INT          NOT NULL IDENTITY(1, 1),
    [Name] NVARCHAR(40) NOT NULL,
    CONSTRAINT [PK_Hashtags] PRIMARY KEY ([Id])
);

CREATE UNIQUE INDEX [IX_Hashtags_Name] ON [Hashtags] ([Name]);

CREATE TABLE [VideoHashtags]
(
    [VideoId]   INT NOT NULL,
    [HashtagId] INT NOT NULL,
    CONSTRAINT [PK_VideoHashtags] PRIMARY KEY ([VideoId], [HashtagId]),
    CONSTRAINT [FK_VideoHashtags_Videos_VideoId]
        FOREIGN KEY ([VideoId]) REFERENCES [Videos] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_VideoHashtags_Hashtags_HashtagId]
        FOREIGN KEY ([HashtagId]) REFERENCES [Hashtags] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_VideoHashtags_HashtagId] ON [VideoHashtags] ([HashtagId]);

/* --- Views ---------------------------------------------------------------- */
/* UserId is NULL when a guest watched the video. */

CREATE TABLE [VideoViews]
(
    [Id]       BIGINT        NOT NULL IDENTITY(1, 1),
    [ViewedAt] DATETIME2     NOT NULL,
    [VideoId]  INT           NOT NULL,
    [UserId]   NVARCHAR(450) NULL,
    CONSTRAINT [PK_VideoViews] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_VideoViews_Videos_VideoId]
        FOREIGN KEY ([VideoId]) REFERENCES [Videos] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_VideoViews_AspNetUsers_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_VideoViews_VideoId_ViewedAt] ON [VideoViews] ([VideoId], [ViewedAt]);
CREATE INDEX [IX_VideoViews_UserId] ON [VideoViews] ([UserId]);

/* --- Notifications -------------------------------------------------------- */
/* Type: 0 = NewFollower, 1 = VideoLiked, 2 = VideoCommented, 3 = VideoRemoved */

CREATE TABLE [Notifications]
(
    [Id]        INT           NOT NULL IDENTITY(1, 1),
    [Type]      INT           NOT NULL,
    [Message]   NVARCHAR(300) NOT NULL,
    [IsRead]    BIT           NOT NULL,
    [CreatedAt] DATETIME2     NOT NULL,
    [UserId]    NVARCHAR(450) NOT NULL,
    [ActorId]   NVARCHAR(450) NULL,
    [VideoId]   INT           NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Notifications_AspNetUsers_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Notifications_AspNetUsers_ActorId]
        FOREIGN KEY ([ActorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Notifications_Videos_VideoId]
        FOREIGN KEY ([VideoId]) REFERENCES [Videos] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Notifications_UserId_IsRead_CreatedAt]
    ON [Notifications] ([UserId], [IsRead], [CreatedAt]);
CREATE INDEX [IX_Notifications_ActorId] ON [Notifications] ([ActorId]);
CREATE INDEX [IX_Notifications_VideoId] ON [Notifications] ([VideoId]);

/* --- Reports -------------------------------------------------------------- */
/* Status: 0 = Pending, 1 = Accepted, 2 = Rejected */

CREATE TABLE [Reports]
(
    [Id]         INT           NOT NULL IDENTITY(1, 1),
    [Reason]     NVARCHAR(400) NOT NULL,
    [Status]     INT           NOT NULL,
    [CreatedAt]  DATETIME2     NOT NULL,
    [ResolvedAt] DATETIME2     NULL,
    [VideoId]    INT           NULL,
    [CommentId]  INT           NULL,
    [ReporterId] NVARCHAR(450) NOT NULL,
    CONSTRAINT [PK_Reports] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Reports_Videos_VideoId]
        FOREIGN KEY ([VideoId]) REFERENCES [Videos] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Reports_Comments_CommentId]
        FOREIGN KEY ([CommentId]) REFERENCES [Comments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Reports_AspNetUsers_ReporterId]
        FOREIGN KEY ([ReporterId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [CK_Reports_ExactlyOneTarget] CHECK
        (([VideoId] IS NOT NULL AND [CommentId] IS NULL)
         OR ([VideoId] IS NULL AND [CommentId] IS NOT NULL))
);

CREATE INDEX [IX_Reports_Status_CreatedAt] ON [Reports] ([Status], [CreatedAt]);
CREATE INDEX [IX_Reports_VideoId] ON [Reports] ([VideoId]);
CREATE INDEX [IX_Reports_CommentId] ON [Reports] ([CommentId]);
CREATE INDEX [IX_Reports_ReporterId] ON [Reports] ([ReporterId]);
