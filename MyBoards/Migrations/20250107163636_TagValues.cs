using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyBoards.Migrations
{
    /// <inheritdoc />
    public partial class TagValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Tag",
                column: "Value",
                value: "Web");

            migrationBuilder.InsertData(
                table: "Tag",
                column: "Value",
                value: "UI");

            migrationBuilder.InsertData(
                table: "Tag",
                column: "Value",
                value: "Desktop");

            migrationBuilder.InsertData(
                table: "Tag",
                column: "Value",
                value: "API");

            migrationBuilder.InsertData(
                table: "Tag",
                column: "Value",
                value: "Service");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Tag",
                keyColumn: "Value",
                keyValue: "Web");

            migrationBuilder.DeleteData(
                table: "Tag",
                keyColumn: "Value",
                keyValue: "UI");

            migrationBuilder.DeleteData(
                table: "Tag",
                keyColumn: "Value",
                keyValue: "Desktop");

            migrationBuilder.DeleteData(
                table: "Tag",
                keyColumn: "Value",
                keyValue: "API");

            migrationBuilder.DeleteData(
                table: "Tag",
                keyColumn: "Value",
                keyValue: "Service");
        }
    }
}
