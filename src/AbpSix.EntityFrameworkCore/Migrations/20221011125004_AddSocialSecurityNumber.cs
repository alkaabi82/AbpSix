using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AbpSix.Migrations
{
    public partial class AddSocialSecurityNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SocialSecurityNumber",
                table: "AbpUsers",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SocialSecurityNumber",
                table: "AbpUsers");
        }
    }
}
