using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTaskModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Technologies_Slug",
                table: "Technologies");

            migrationBuilder.CreateIndex(
                name: "IX_Technologies_UserId_Slug",
                table: "Technologies",
                columns: new[] { "UserId", "Slug" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Technologies_Users_UserId",
                table: "Technologies",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Technologies_Users_UserId",
                table: "Technologies");

            migrationBuilder.DropIndex(
                name: "IX_Technologies_UserId_Slug",
                table: "Technologies");

            migrationBuilder.CreateIndex(
                name: "IX_Technologies_Slug",
                table: "Technologies",
                column: "Slug",
                unique: true);
        }
    }
}
