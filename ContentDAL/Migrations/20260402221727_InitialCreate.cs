using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ContentDAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "collaborationsnapshots",
                columns: table => new
                {
                    collaborationsnapshotid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    collaborationid = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    avatarurl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    syncedat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_collaborationsnapshots", x => x.collaborationsnapshotid);
                });

            migrationBuilder.CreateTable(
                name: "postauthors",
                columns: table => new
                {
                    postauthorid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    avatarurl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    syncedat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_postauthors", x => x.postauthorid);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    tagid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tags", x => x.tagid);
                });

            migrationBuilder.CreateTable(
                name: "posts",
                columns: table => new
                {
                    postid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    postauthorid = table.Column<int>(type: "integer", nullable: true),
                    collaborationsnapshotid = table.Column<int>(type: "integer", nullable: true),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    externallink = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'Published'"),
                    createdby = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updatedby = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updatedat = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    isdeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValueSql: "false")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_posts", x => x.postid);
                    table.CheckConstraint("ck_post_authororcollaboration", "(postauthorid IS NOT NULL AND collaborationsnapshotid IS NULL) OR (postauthorid IS NULL AND collaborationsnapshotid IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_posts_collaborationsnapshots_collaborationsnapshotid",
                        column: x => x.collaborationsnapshotid,
                        principalTable: "collaborationsnapshots",
                        principalColumn: "collaborationsnapshotid",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_posts_postauthors_postauthorid",
                        column: x => x.postauthorid,
                        principalTable: "postauthors",
                        principalColumn: "postauthorid",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "comments",
                columns: table => new
                {
                    commentid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    postid = table.Column<int>(type: "integer", nullable: false),
                    postauthorid = table.Column<int>(type: "integer", nullable: false),
                    parentcommentid = table.Column<int>(type: "integer", nullable: true),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    createdby = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updatedby = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updatedat = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    isdeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValueSql: "false")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comments", x => x.commentid);
                    table.ForeignKey(
                        name: "fk_comments_comments_parentcommentid",
                        column: x => x.parentcommentid,
                        principalTable: "comments",
                        principalColumn: "commentid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_comments_postauthors_postauthorid",
                        column: x => x.postauthorid,
                        principalTable: "postauthors",
                        principalColumn: "postauthorid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_comments_posts_postid",
                        column: x => x.postid,
                        principalTable: "posts",
                        principalColumn: "postid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "likes",
                columns: table => new
                {
                    likeid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    postid = table.Column<int>(type: "integer", nullable: false),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_likes", x => x.likeid);
                    table.ForeignKey(
                        name: "fk_likes_posts_postid",
                        column: x => x.postid,
                        principalTable: "posts",
                        principalColumn: "postid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "postmedia",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    postid = table.Column<int>(type: "integer", nullable: false),
                    objectname = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    bucket = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    contenttype = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_postmedia", x => x.id);
                    table.ForeignKey(
                        name: "fk_postmedia_posts_postid",
                        column: x => x.postid,
                        principalTable: "posts",
                        principalColumn: "postid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "posttags",
                columns: table => new
                {
                    postid = table.Column<int>(type: "integer", nullable: false),
                    tagid = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_posttags", x => new { x.postid, x.tagid });
                    table.ForeignKey(
                        name: "fk_posttags_posts_postid",
                        column: x => x.postid,
                        principalTable: "posts",
                        principalColumn: "postid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_posttags_tags_tagid",
                        column: x => x.tagid,
                        principalTable: "tags",
                        principalColumn: "tagid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "postviews",
                columns: table => new
                {
                    postviewid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    postid = table.Column<int>(type: "integer", nullable: false),
                    userid = table.Column<int>(type: "integer", nullable: true),
                    viewedat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_postviews", x => x.postviewid);
                    table.ForeignKey(
                        name: "fk_postviews_posts_postid",
                        column: x => x.postid,
                        principalTable: "posts",
                        principalColumn: "postid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "savedposts",
                columns: table => new
                {
                    savedpostid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    postid = table.Column<int>(type: "integer", nullable: false),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    savedat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_savedposts", x => x.savedpostid);
                    table.ForeignKey(
                        name: "fk_savedposts_posts_postid",
                        column: x => x.postid,
                        principalTable: "posts",
                        principalColumn: "postid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_collaborationsnapshots_collaborationid",
                table: "collaborationsnapshots",
                column: "collaborationid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_comments_parentcommentid",
                table: "comments",
                column: "parentcommentid");

            migrationBuilder.CreateIndex(
                name: "ix_comments_postauthorid",
                table: "comments",
                column: "postauthorid");

            migrationBuilder.CreateIndex(
                name: "ix_comments_postid",
                table: "comments",
                column: "postid");

            migrationBuilder.CreateIndex(
                name: "ix_likes_postid_userid",
                table: "likes",
                columns: new[] { "postid", "userid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_postauthors_userid",
                table: "postauthors",
                column: "userid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_postmedia_postid",
                table: "postmedia",
                column: "postid");

            migrationBuilder.CreateIndex(
                name: "ix_posts_collaborationsnapshotid",
                table: "posts",
                column: "collaborationsnapshotid");

            migrationBuilder.CreateIndex(
                name: "ix_posts_postauthorid",
                table: "posts",
                column: "postauthorid");

            migrationBuilder.CreateIndex(
                name: "ix_posttags_tagid",
                table: "posttags",
                column: "tagid");

            migrationBuilder.CreateIndex(
                name: "ix_postviews_postid",
                table: "postviews",
                column: "postid");

            migrationBuilder.CreateIndex(
                name: "ix_savedposts_postid_userid",
                table: "savedposts",
                columns: new[] { "postid", "userid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tags_name",
                table: "tags",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "comments");

            migrationBuilder.DropTable(
                name: "likes");

            migrationBuilder.DropTable(
                name: "postmedia");

            migrationBuilder.DropTable(
                name: "posttags");

            migrationBuilder.DropTable(
                name: "postviews");

            migrationBuilder.DropTable(
                name: "savedposts");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "posts");

            migrationBuilder.DropTable(
                name: "collaborationsnapshots");

            migrationBuilder.DropTable(
                name: "postauthors");
        }
    }
}
