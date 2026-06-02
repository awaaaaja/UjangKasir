using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UjangKasir.Desktop.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260601103000_AddOperationalModules")]
public partial class AddOperationalModules : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("Code", "Suppliers", type: "TEXT", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>("ContactPerson", "Suppliers", type: "TEXT", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<DateTime>("CreatedAt", "Suppliers", type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP");
        migrationBuilder.AddColumn<string>("Email", "Suppliers", type: "TEXT", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>("Notes", "Suppliers", type: "TEXT", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<DateTime>("UpdatedAt", "Suppliers", type: "TEXT", nullable: true);
        migrationBuilder.AddColumn<int>("MemberId", "Sales", type: "INTEGER", nullable: true);

        migrationBuilder.CreateTable(
            name: "Members",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                MemberCode = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Phone = table.Column<string>(type: "TEXT", nullable: false),
                Email = table.Column<string>(type: "TEXT", nullable: false),
                Address = table.Column<string>(type: "TEXT", nullable: false),
                Points = table.Column<int>(type: "INTEGER", nullable: false),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_Members", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Promos",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                Code = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                PromoType = table.Column<string>(type: "TEXT", nullable: false),
                DiscountType = table.Column<string>(type: "TEXT", nullable: false),
                DiscountValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                MinimumPurchase = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_Promos", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Purchases",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                PurchaseNumber = table.Column<string>(type: "TEXT", nullable: false),
                SupplierId = table.Column<int>(type: "INTEGER", nullable: false),
                PurchaseDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                TotalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                PaidAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                PaymentStatus = table.Column<string>(type: "TEXT", nullable: false),
                Notes = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Purchases", x => x.Id);
                table.ForeignKey("FK_Purchases_Suppliers_SupplierId", x => x.SupplierId, "Suppliers", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Expenses",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                ExpenseDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                Category = table.Column<string>(type: "TEXT", nullable: false),
                Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: false),
                PaymentMethod = table.Column<string>(type: "TEXT", nullable: false),
                CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Expenses", x => x.Id);
                table.ForeignKey("FK_Expenses_Users_CreatedByUserId", x => x.CreatedByUserId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "HeldTransactions",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                HoldNumber = table.Column<string>(type: "TEXT", nullable: false),
                CashierUserId = table.Column<int>(type: "INTEGER", nullable: false),
                MemberId = table.Column<int>(type: "INTEGER", nullable: true),
                Notes = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HeldTransactions", x => x.Id);
                table.ForeignKey("FK_HeldTransactions_Members_MemberId", x => x.MemberId, "Members", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_HeldTransactions_Users_CashierUserId", x => x.CashierUserId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PurchaseItems",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                PurchaseId = table.Column<int>(type: "INTEGER", nullable: false),
                ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                UnitCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Subtotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PurchaseItems", x => x.Id);
                table.ForeignKey("FK_PurchaseItems_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_PurchaseItems_Purchases_PurchaseId", x => x.PurchaseId, "Purchases", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "HeldTransactionItems",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                HeldTransactionId = table.Column<int>(type: "INTEGER", nullable: false),
                ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                ProductName = table.Column<string>(type: "TEXT", nullable: false),
                Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                UnitPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Discount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Subtotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HeldTransactionItems", x => x.Id);
                table.ForeignKey("FK_HeldTransactionItems_HeldTransactions_HeldTransactionId", x => x.HeldTransactionId, "HeldTransactions", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_HeldTransactionItems_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_Suppliers_Code", "Suppliers", "Code", unique: true, filter: "Code <> ''");
        migrationBuilder.CreateIndex("IX_Sales_MemberId", "Sales", "MemberId");
        migrationBuilder.CreateIndex("IX_Members_MemberCode", "Members", "MemberCode", unique: true);
        migrationBuilder.CreateIndex("IX_Promos_Code", "Promos", "Code", unique: true);
        migrationBuilder.CreateIndex("IX_Purchases_PurchaseNumber", "Purchases", "PurchaseNumber", unique: true);
        migrationBuilder.CreateIndex("IX_Purchases_SupplierId", "Purchases", "SupplierId");
        migrationBuilder.CreateIndex("IX_Expenses_CreatedByUserId", "Expenses", "CreatedByUserId");
        migrationBuilder.CreateIndex("IX_HeldTransactions_HoldNumber", "HeldTransactions", "HoldNumber", unique: true);
        migrationBuilder.CreateIndex("IX_HeldTransactions_CashierUserId", "HeldTransactions", "CashierUserId");
        migrationBuilder.CreateIndex("IX_HeldTransactions_MemberId", "HeldTransactions", "MemberId");
        migrationBuilder.CreateIndex("IX_PurchaseItems_ProductId", "PurchaseItems", "ProductId");
        migrationBuilder.CreateIndex("IX_PurchaseItems_PurchaseId", "PurchaseItems", "PurchaseId");
        migrationBuilder.CreateIndex("IX_HeldTransactionItems_HeldTransactionId", "HeldTransactionItems", "HeldTransactionId");
        migrationBuilder.CreateIndex("IX_HeldTransactionItems_ProductId", "HeldTransactionItems", "ProductId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("HeldTransactionItems");
        migrationBuilder.DropTable("PurchaseItems");
        migrationBuilder.DropTable("Expenses");
        migrationBuilder.DropTable("Promos");
        migrationBuilder.DropTable("HeldTransactions");
        migrationBuilder.DropTable("Purchases");
        migrationBuilder.DropTable("Members");
        migrationBuilder.DropIndex("IX_Suppliers_Code", "Suppliers");
        migrationBuilder.DropIndex("IX_Sales_MemberId", "Sales");
        migrationBuilder.DropColumn("Code", "Suppliers");
        migrationBuilder.DropColumn("ContactPerson", "Suppliers");
        migrationBuilder.DropColumn("CreatedAt", "Suppliers");
        migrationBuilder.DropColumn("Email", "Suppliers");
        migrationBuilder.DropColumn("Notes", "Suppliers");
        migrationBuilder.DropColumn("UpdatedAt", "Suppliers");
        migrationBuilder.DropColumn("MemberId", "Sales");
    }
}
