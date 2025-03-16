using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoiceInfo.Migrations
{
    /// <inheritdoc />
    public partial class latestNews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLatestNews",
                table: "Posts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLatestNews",
                table: "Posts");
        }
    }
}
