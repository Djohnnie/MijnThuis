using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MijnThuis.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Flags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FLAGS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SysId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FLAGS", x => x.Id)
                        .Annotation("SqlServer:Clustered", false);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FLAGS_Name",
                table: "FLAGS",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_FLAGS_SysId",
                table: "FLAGS",
                column: "SysId")
                .Annotation("SqlServer:Clustered", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FLAGS");
        }
    }
}
