using ECRS_API.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECRS_API.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ECRSDbContext))]
    [Migration("20260601000000_AddProjectPmdsKey")]
    public partial class AddProjectPmdsKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "專案名稱代碼表主鍵_PMDS",
                table: "專案名稱代碼表",
                type: "int",
                nullable: true,
                comment: "PMDS 專案名稱代碼表主鍵");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "專案名稱代碼表主鍵_PMDS",
                table: "專案名稱代碼表");
        }
    }
}
