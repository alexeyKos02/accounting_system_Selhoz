using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroInventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    low_stock_threshold_liters = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    auto_open_packages = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    first_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    last_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    must_change_password = table.Column<bool>(type: "boolean", nullable: false),
                    is_system_admin = table.Column<bool>(type: "boolean", nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    display_name = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    old_values = table.Column<string>(type: "jsonb", nullable: true),
                    new_values = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    legal_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    bin_or_inn = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    timezone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_companies", x => x.id);
                    table.ForeignKey(
                        name: "fk_companies_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "company_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_company_memberships", x => x.id);
                    table.ForeignKey(
                        name: "fk_company_memberships_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_company_memberships_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_company_memberships_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "crops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_crops", x => x.id);
                    table.ForeignKey(
                        name: "fk_crops_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fields",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fields", x => x.id);
                    table.ForeignKey(
                        name: "fk_fields_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_type = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    merged_into_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_items_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_items_inventory_items_merged_into_item_id",
                        column: x => x.merged_into_item_id,
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "warehouses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warehouses", x => x.id);
                    table.ForeignKey(
                        name: "fk_warehouses_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "membership_access_scopes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_type = table.Column<int>(type: "integer", nullable: false),
                    scope_entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_membership_access_scopes", x => x.id);
                    table.ForeignKey(
                        name: "fk_membership_access_scopes_company_memberships_membership_id",
                        column: x => x.membership_id,
                        principalTable: "company_memberships",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chemical_crops",
                columns: table => new
                {
                    chemical_id = table.Column<Guid>(type: "uuid", nullable: false),
                    crop_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chemical_crops", x => new { x.chemical_id, x.crop_id });
                    table.ForeignKey(
                        name: "fk_chemical_crops_crops_crop_id",
                        column: x => x.crop_id,
                        principalTable: "crops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_chemical_crops_inventory_items_chemical_id",
                        column: x => x.chemical_id,
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chemical_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: true),
                    manufacturer = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chemical_details", x => x.id);
                    table.ForeignKey(
                        name: "fk_chemical_details_inventory_items_inventory_item_id",
                        column: x => x.inventory_item_id,
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chemical_stock_balances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chemical_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    loose_liters = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    total_liters = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chemical_stock_balances", x => x.id);
                    table.ForeignKey(
                        name: "fk_chemical_stock_balances_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_chemical_stock_balances_inventory_items_chemical_id",
                        column: x => x.chemical_id,
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_chemical_stock_balances_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_movements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chemical_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    movement_type = table.Column<int>(type: "integer", nullable: false),
                    quantity_liters = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    unit_type = table.Column<int>(type: "integer", nullable: true),
                    package_volume_liters = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    packages_quantity = table.Column<int>(type: "integer", nullable: true),
                    crop_id = table.Column<Guid>(type: "uuid", nullable: true),
                    field_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_movements", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_movements_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_movements_crops_crop_id",
                        column: x => x.crop_id,
                        principalTable: "crops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_movements_fields_field_id",
                        column: x => x.field_id,
                        principalTable: "fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_movements_inventory_items_chemical_id",
                        column: x => x.chemical_id,
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_movements_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_movements_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "opened_packages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chemical_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_type = table.Column<int>(type: "integer", nullable: false),
                    initial_liters = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    remaining_liters = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    opened_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_opened_packages", x => x.id);
                    table.ForeignKey(
                        name: "fk_opened_packages_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_opened_packages_inventory_items_chemical_id",
                        column: x => x.chemical_id,
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_opened_packages_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "package_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chemical_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_type = table.Column<int>(type: "integer", nullable: false),
                    package_volume_liters = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_package_groups", x => x.id);
                    table.ForeignKey(
                        name: "fk_package_groups_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_package_groups_inventory_items_chemical_id",
                        column: x => x.chemical_id,
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_package_groups_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_movement_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    movement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<int>(type: "integer", nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unit_type = table.Column<int>(type: "integer", nullable: true),
                    package_volume_liters = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    quantity_liters = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    packages_quantity = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_movement_details", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_movement_details_inventory_movements_movement_id",
                        column: x => x.movement_id,
                        principalTable: "inventory_movements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "app_settings",
                columns: new[] { "id", "auto_open_packages", "low_stock_threshold_liters", "updated_at" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000003"), false, 10m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "id", "created_at", "display_name", "email", "first_name", "is_system", "is_system_admin", "last_name", "must_change_password", "password_hash", "phone", "status", "updated_at" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, "System", true, true, "", false, null, null, 0, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                table: "companies",
                columns: new[] { "id", "address", "bin_or_inn", "country", "created_at", "created_by_user_id", "description", "legal_name", "name", "status", "timezone", "updated_at" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000201"), null, null, "KZ", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000001"), null, null, "Хозяйство по умолчанию", 0, "Asia/Almaty", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                table: "company_memberships",
                columns: new[] { "id", "company_id", "created_at", "created_by_user_id", "role", "status", "updated_at", "user_id" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000202"), new Guid("00000000-0000-0000-0000-000000000201"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000001"), 1, 0, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000001") });

            migrationBuilder.InsertData(
                table: "membership_access_scopes",
                columns: new[] { "id", "created_at", "membership_id", "scope_entity_id", "scope_type" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000203"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000202"), null, 0 });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_action",
                table: "audit_logs",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_created_at",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity_type",
                table: "audit_logs",
                column: "entity_type");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_chemical_crops_crop_id",
                table: "chemical_crops",
                column: "crop_id");

            migrationBuilder.CreateIndex(
                name: "ix_chemical_details_inventory_item_id",
                table: "chemical_details",
                column: "inventory_item_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_chemical_stock_balances_chemical_id_warehouse_id",
                table: "chemical_stock_balances",
                columns: new[] { "chemical_id", "warehouse_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_chemical_stock_balances_company_id",
                table: "chemical_stock_balances",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_chemical_stock_balances_warehouse_id",
                table: "chemical_stock_balances",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "ix_companies_created_by_user_id",
                table: "companies",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_companies_status",
                table: "companies",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_company_memberships_company_id",
                table: "company_memberships",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_company_memberships_created_by_user_id",
                table: "company_memberships",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_company_memberships_user_id_company_id",
                table: "company_memberships",
                columns: new[] { "user_id", "company_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_crops_company_id_name",
                table: "crops",
                columns: new[] { "company_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fields_company_id_number",
                table: "fields",
                columns: new[] { "company_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inventory_items_company_id",
                table: "inventory_items",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_items_item_type",
                table: "inventory_items",
                column: "item_type");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_items_merged_into_item_id",
                table: "inventory_items",
                column: "merged_into_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_items_name",
                table: "inventory_items",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_items_status",
                table: "inventory_items",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movement_details_movement_id",
                table: "inventory_movement_details",
                column: "movement_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_chemical_id_warehouse_id",
                table: "inventory_movements",
                columns: new[] { "chemical_id", "warehouse_id" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_company_id",
                table: "inventory_movements",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_created_by_user_id",
                table: "inventory_movements",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_crop_id",
                table: "inventory_movements",
                column: "crop_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_field_id",
                table: "inventory_movements",
                column: "field_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_is_deleted",
                table: "inventory_movements",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_occurred_at",
                table: "inventory_movements",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_warehouse_id",
                table: "inventory_movements",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "ix_membership_access_scopes_membership_id_scope_type",
                table: "membership_access_scopes",
                columns: new[] { "membership_id", "scope_type" });

            migrationBuilder.CreateIndex(
                name: "ix_opened_packages_chemical_id_warehouse_id",
                table: "opened_packages",
                columns: new[] { "chemical_id", "warehouse_id" });

            migrationBuilder.CreateIndex(
                name: "ix_opened_packages_company_id",
                table: "opened_packages",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_opened_packages_warehouse_id",
                table: "opened_packages",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "ix_package_groups_chemical_id_warehouse_id",
                table: "package_groups",
                columns: new[] { "chemical_id", "warehouse_id" });

            migrationBuilder.CreateIndex(
                name: "ix_package_groups_company_id",
                table: "package_groups",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_package_groups_warehouse_id",
                table: "package_groups",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_warehouses_company_id_number",
                table: "warehouses",
                columns: new[] { "company_id", "number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_settings");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "chemical_crops");

            migrationBuilder.DropTable(
                name: "chemical_details");

            migrationBuilder.DropTable(
                name: "chemical_stock_balances");

            migrationBuilder.DropTable(
                name: "inventory_movement_details");

            migrationBuilder.DropTable(
                name: "membership_access_scopes");

            migrationBuilder.DropTable(
                name: "opened_packages");

            migrationBuilder.DropTable(
                name: "package_groups");

            migrationBuilder.DropTable(
                name: "inventory_movements");

            migrationBuilder.DropTable(
                name: "company_memberships");

            migrationBuilder.DropTable(
                name: "crops");

            migrationBuilder.DropTable(
                name: "fields");

            migrationBuilder.DropTable(
                name: "inventory_items");

            migrationBuilder.DropTable(
                name: "warehouses");

            migrationBuilder.DropTable(
                name: "companies");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
