using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UjangKasir.Desktop.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260601093000_UpdateProductMasterFields")]
public partial class UpdateProductMasterFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Products_Barcode", table: "Products");
        migrationBuilder.DropIndex(name: "IX_Products_Sku", table: "Products");

        migrationBuilder.RenameColumn(name: "Sku", table: "Products", newName: "Code");
        migrationBuilder.RenameColumn(name: "BuyPrice", table: "Products", newName: "PurchasePrice");
        migrationBuilder.RenameColumn(name: "SellPrice", table: "Products", newName: "SellingPrice");
        migrationBuilder.RenameColumn(name: "StockQuantity", table: "Products", newName: "Stock");

        migrationBuilder.AddColumn<DateTime>(
            name: "ExpiredDate",
            table: "Products",
            type: "TEXT",
            nullable: true);

        migrationBuilder.CreateIndex(name: "IX_Products_Code", table: "Products", column: "Code", unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_Products_Barcode",
            table: "Products",
            column: "Barcode",
            unique: true,
            filter: "Barcode <> ''");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Products_Barcode", table: "Products");
        migrationBuilder.DropIndex(name: "IX_Products_Code", table: "Products");

        migrationBuilder.DropColumn(name: "ExpiredDate", table: "Products");

        migrationBuilder.RenameColumn(name: "Code", table: "Products", newName: "Sku");
        migrationBuilder.RenameColumn(name: "PurchasePrice", table: "Products", newName: "BuyPrice");
        migrationBuilder.RenameColumn(name: "SellingPrice", table: "Products", newName: "SellPrice");
        migrationBuilder.RenameColumn(name: "Stock", table: "Products", newName: "StockQuantity");

        migrationBuilder.CreateIndex(name: "IX_Products_Barcode", table: "Products", column: "Barcode", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Products_Sku", table: "Products", column: "Sku", unique: true);
    }
}
