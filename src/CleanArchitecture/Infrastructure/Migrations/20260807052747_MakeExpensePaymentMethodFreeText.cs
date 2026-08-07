using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeExpensePaymentMethodFreeText : Migration
    {
        // The payment_method column already stores free-form text (varchar(100)).
        // This migration only records the CLR type change (ExpensePaymentMethod enum -> string)
        // so the EF model snapshot stays in sync. No database schema change is required.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
