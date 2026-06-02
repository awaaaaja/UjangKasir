using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UjangKasir.Desktop.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260601083600_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AppSettings",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Key = table.Column<string>(type: "TEXT", nullable: false),
                Value = table.Column<string>(type: "TEXT", nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AppSettings", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Categories",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: false),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Categories", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Permissions",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Code = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Permissions", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Roles",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Roles", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Suppliers",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Phone = table.Column<string>(type: "TEXT", nullable: false),
                Address = table.Column<string>(type: "TEXT", nullable: false),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Suppliers", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Units",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Symbol = table.Column<string>(type: "TEXT", nullable: false),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Units", x => x.Id));

        migrationBuilder.CreateTable(
            name: "RolePermissions",
            columns: table => new
            {
                RoleId = table.Column<int>(type: "INTEGER", nullable: false),
                PermissionId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                table.ForeignKey("FK_RolePermissions_Permissions_PermissionId", x => x.PermissionId, "Permissions", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_RolePermissions_Roles_RoleId", x => x.RoleId, "Roles", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                RoleId = table.Column<int>(type: "INTEGER", nullable: false),
                Username = table.Column<string>(type: "TEXT", nullable: false),
                PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                FullName = table.Column<string>(type: "TEXT", nullable: false),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
                table.ForeignKey("FK_Users_Roles_RoleId", x => x.RoleId, "Roles", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Products",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                UnitId = table.Column<int>(type: "INTEGER", nullable: false),
                SupplierId = table.Column<int>(type: "INTEGER", nullable: true),
                Sku = table.Column<string>(type: "TEXT", nullable: false),
                Barcode = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                BuyPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                SellPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                StockQuantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                MinimumStock = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Products", x => x.Id);
                table.ForeignKey("FK_Products_Categories_CategoryId", x => x.CategoryId, "Categories", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_Products_Suppliers_SupplierId", x => x.SupplierId, "Suppliers", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_Products_Units_UnitId", x => x.UnitId, "Units", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "AuditLogs",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<int>(type: "INTEGER", nullable: true),
                Action = table.Column<string>(type: "TEXT", nullable: false),
                EntityName = table.Column<string>(type: "TEXT", nullable: false),
                EntityId = table.Column<string>(type: "TEXT", nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLogs", x => x.Id);
                table.ForeignKey("FK_AuditLogs_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "CashierShifts",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<int>(type: "INTEGER", nullable: false),
                OpenedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                OpeningCash = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                ClosingCash = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                Status = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CashierShifts", x => x.Id);
                table.ForeignKey("FK_CashierShifts_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "StockMovements",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                UserId = table.Column<int>(type: "INTEGER", nullable: true),
                MovementType = table.Column<string>(type: "TEXT", nullable: false),
                QuantityChange = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                QuantityBefore = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                QuantityAfter = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                ReferenceType = table.Column<string>(type: "TEXT", nullable: false),
                ReferenceId = table.Column<int>(type: "INTEGER", nullable: true),
                Note = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StockMovements", x => x.Id);
                table.ForeignKey("FK_StockMovements_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_StockMovements_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "Sales",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                InvoiceNumber = table.Column<string>(type: "TEXT", nullable: false),
                UserId = table.Column<int>(type: "INTEGER", nullable: false),
                CashierShiftId = table.Column<int>(type: "INTEGER", nullable: true),
                TransactionTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                Subtotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                DiscountTotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                TaxTotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                GrandTotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Status = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Sales", x => x.Id);
                table.ForeignKey("FK_Sales_CashierShifts_CashierShiftId", x => x.CashierShiftId, "CashierShifts", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_Sales_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Payments",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                SaleId = table.Column<int>(type: "INTEGER", nullable: false),
                Method = table.Column<string>(type: "TEXT", nullable: false),
                Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                ChangeAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                PaidAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Payments", x => x.Id);
                table.ForeignKey("FK_Payments_Sales_SaleId", x => x.SaleId, "Sales", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SaleItems",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                SaleId = table.Column<int>(type: "INTEGER", nullable: false),
                ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                ProductNameSnapshot = table.Column<string>(type: "TEXT", nullable: false),
                BarcodeSnapshot = table.Column<string>(type: "TEXT", nullable: false),
                Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                UnitPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                DiscountAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                LineTotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SaleItems", x => x.Id);
                table.ForeignKey("FK_SaleItems_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_SaleItems_Sales_SaleId", x => x.SaleId, "Sales", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_AppSettings_Key", "AppSettings", "Key", unique: true);
        migrationBuilder.CreateIndex("IX_AuditLogs_UserId", "AuditLogs", "UserId");
        migrationBuilder.CreateIndex("IX_CashierShifts_UserId", "CashierShifts", "UserId");
        migrationBuilder.CreateIndex("IX_Categories_Name", "Categories", "Name", unique: true);
        migrationBuilder.CreateIndex("IX_Payments_SaleId", "Payments", "SaleId");
        migrationBuilder.CreateIndex("IX_Permissions_Code", "Permissions", "Code", unique: true);
        migrationBuilder.CreateIndex("IX_Products_Barcode", "Products", "Barcode", unique: true);
        migrationBuilder.CreateIndex("IX_Products_CategoryId", "Products", "CategoryId");
        migrationBuilder.CreateIndex("IX_Products_Sku", "Products", "Sku", unique: true);
        migrationBuilder.CreateIndex("IX_Products_SupplierId", "Products", "SupplierId");
        migrationBuilder.CreateIndex("IX_Products_UnitId", "Products", "UnitId");
        migrationBuilder.CreateIndex("IX_RolePermissions_PermissionId", "RolePermissions", "PermissionId");
        migrationBuilder.CreateIndex("IX_Roles_Name", "Roles", "Name", unique: true);
        migrationBuilder.CreateIndex("IX_SaleItems_ProductId", "SaleItems", "ProductId");
        migrationBuilder.CreateIndex("IX_SaleItems_SaleId", "SaleItems", "SaleId");
        migrationBuilder.CreateIndex("IX_Sales_CashierShiftId", "Sales", "CashierShiftId");
        migrationBuilder.CreateIndex("IX_Sales_InvoiceNumber", "Sales", "InvoiceNumber", unique: true);
        migrationBuilder.CreateIndex("IX_Sales_UserId", "Sales", "UserId");
        migrationBuilder.CreateIndex("IX_StockMovements_ProductId", "StockMovements", "ProductId");
        migrationBuilder.CreateIndex("IX_StockMovements_UserId", "StockMovements", "UserId");
        migrationBuilder.CreateIndex("IX_Units_Symbol", "Units", "Symbol", unique: true);
        migrationBuilder.CreateIndex("IX_Users_RoleId", "Users", "RoleId");
        migrationBuilder.CreateIndex("IX_Users_Username", "Users", "Username", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AppSettings");
        migrationBuilder.DropTable("AuditLogs");
        migrationBuilder.DropTable("Payments");
        migrationBuilder.DropTable("RolePermissions");
        migrationBuilder.DropTable("SaleItems");
        migrationBuilder.DropTable("StockMovements");
        migrationBuilder.DropTable("Permissions");
        migrationBuilder.DropTable("Sales");
        migrationBuilder.DropTable("Products");
        migrationBuilder.DropTable("CashierShifts");
        migrationBuilder.DropTable("Categories");
        migrationBuilder.DropTable("Suppliers");
        migrationBuilder.DropTable("Units");
        migrationBuilder.DropTable("Users");
        migrationBuilder.DropTable("Roles");
    }
}
