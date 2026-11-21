using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentDAL.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearchToPosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE posts ADD COLUMN IF NOT EXISTS search_vector tsvector;
                CREATE INDEX IF NOT EXISTS ix_posts_search_vector ON posts USING gin(search_vector);
                
                CREATE OR REPLACE FUNCTION posts_search_vector_trigger() RETURNS trigger AS $$
                BEGIN
                  NEW.search_vector :=
                    setweight(to_tsvector('english', coalesce(NEW.title, '')), 'A') ||
                    setweight(to_tsvector('english', coalesce(NEW.description, '')), 'B');
                  RETURN NEW;
                END
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER tsvectorupdate BEFORE INSERT OR UPDATE
                ON posts FOR EACH ROW EXECUTE FUNCTION posts_search_vector_trigger();
                
                UPDATE posts SET search_vector = 
                    setweight(to_tsvector('english', coalesce(title, '')), 'A') ||
                    setweight(to_tsvector('english', coalesce(description, '')), 'B');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS tsvectorupdate ON posts;
                DROP FUNCTION IF EXISTS posts_search_vector_trigger();
                DROP INDEX IF EXISTS ix_posts_search_vector;
                ALTER TABLE posts DROP COLUMN IF EXISTS search_vector;
            ");
        }
    }
}
