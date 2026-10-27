using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MetaTestInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MetaWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PhoneNumberId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SenderWaId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    State = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetaWebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MetaWebhookEvents_MessageId",
                table: "MetaWebhookEvents",
                column: "MessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MetaWebhookEvents_State_ReceivedAt",
                table: "MetaWebhookEvents",
                columns: new[] { "State", "ReceivedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MetaWebhookEvents");
        }
    }
}
