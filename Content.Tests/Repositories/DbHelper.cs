using Dapper;
using Microsoft.Data.Sqlite;

namespace Content.Tests.Repositories;

internal static class DbHelper
{
    public static SqliteConnection OpenDb()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();

        conn.Execute(@"
            CREATE TABLE PostAuthors (
                PostAuthorId INTEGER PRIMARY KEY,
                UserId       INTEGER NOT NULL,
                UserName     TEXT    NOT NULL,
                AvatarUrl    TEXT,
                SyncedAt     TEXT    NOT NULL
            )");

        conn.Execute(@"
            CREATE TABLE CollaborationSnapshots (
                CollaborationSnapshotId INTEGER PRIMARY KEY,
                CollaborationId         INTEGER NOT NULL,
                Name                    TEXT    NOT NULL,
                AvatarUrl               TEXT,
                SyncedAt                TEXT    NOT NULL,
                ExternalId              TEXT
            )");

        conn.Execute(@"
            CREATE TABLE Posts (
                PostId                  INTEGER PRIMARY KEY,
                PostAuthorId            INTEGER,
                CollaborationSnapshotId INTEGER,
                Title                   TEXT    NOT NULL,
                Description             TEXT,
                ExternalLink            TEXT,
                Status                  TEXT    NOT NULL DEFAULT 'Published',
                CreatedAt               TEXT    NOT NULL,
                CreatedBy               TEXT,
                UpdatedAt               TEXT,
                UpdatedBy               TEXT,
                IsDeleted               INTEGER NOT NULL DEFAULT 0
            )");

        conn.Execute(@"
            CREATE TABLE Tags (
                TagId INTEGER PRIMARY KEY,
                Name  TEXT NOT NULL
            )");

        conn.Execute(@"
            CREATE TABLE PostTags (
                PostId INTEGER NOT NULL,
                TagId  INTEGER NOT NULL,
                PRIMARY KEY (PostId, TagId)
            )");

        conn.Execute(@"
            CREATE TABLE Comments (
                CommentId       INTEGER PRIMARY KEY,
                PostId          INTEGER NOT NULL,
                PostAuthorId    INTEGER NOT NULL,
                ParentCommentId INTEGER,
                Body            TEXT    NOT NULL,
                CreatedAt       TEXT    NOT NULL,
                CreatedBy       TEXT,
                UpdatedAt       TEXT,
                UpdatedBy       TEXT,
                IsDeleted       INTEGER NOT NULL DEFAULT 0
            )");

        conn.Execute(@"
            CREATE TABLE Likes (
                LikeId    INTEGER PRIMARY KEY,
                PostId    INTEGER NOT NULL,
                UserId    INTEGER NOT NULL,
                CreatedAt TEXT    NOT NULL
            )");

        conn.Execute(@"
            CREATE TABLE PostViews (
                PostViewId INTEGER PRIMARY KEY,
                PostId     INTEGER NOT NULL,
                UserId     INTEGER,
                ViewedAt   TEXT    NOT NULL
            )");

        conn.Execute(@"
            CREATE TABLE SavedPosts (
                SavedPostId INTEGER PRIMARY KEY,
                PostId      INTEGER NOT NULL,
                UserId      INTEGER NOT NULL,
                SavedAt     TEXT    NOT NULL
            )");

        conn.Execute(@"
            CREATE TABLE PostMedia (
                Id          INTEGER PRIMARY KEY,
                PostId      INTEGER NOT NULL,
                ObjectName  TEXT    NOT NULL,
                Bucket      TEXT    NOT NULL,
                ContentType TEXT    NOT NULL
            )");

        return conn;
    }
}
