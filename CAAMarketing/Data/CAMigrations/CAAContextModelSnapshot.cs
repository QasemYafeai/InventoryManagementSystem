﻿// <auto-generated />
using System;
using CAAMarketing.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace CAAMarketing.Data.CAMigrations
{
    [DbContext(typeof(CAAContext))]
    partial class CAAContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.14");

            modelBuilder.Entity("CAAMarketing.Models.Archive", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("CreatedBy")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("CreatedOn")
                        .HasColumnType("TEXT");

                    b.Property<string>("EmployeeNameUser")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<int>("ItemID")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("BLOB");

                    b.Property<string>("UpdatedBy")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("UpdatedOn")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ItemID");

                    b.ToTable("Archives");
                });

            modelBuilder.Entity("CAAMarketing.Models.Category", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("CreatedBy")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("CreatedOn")
                        .HasColumnType("TEXT");

                    b.Property<string>("EmployeeNameUser")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsLowInventory")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LowCategoryThreshold")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("BLOB");

                    b.Property<string>("UpdatedBy")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("UpdatedOn")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Category");
                });

            modelBuilder.Entity("CAAMarketing.Models.Employee", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Active")
                        .HasColumnType("INTEGER");

                    b.Property<string>("CreatedBy")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("CreatedOn")
                        .HasColumnType("TEXT");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("EmployeeNameUser")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Phone")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("BLOB");

                    b.Property<string>("UpdatedBy")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("UpdatedOn")
                        .HasColumnType("TEXT");

                    b.HasKey("ID");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.ToTable("Employees");
                });

            modelBuilder.Entity("CAAMarketing.Models.Equipment", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("CreatedBy")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("CreatedOn")
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.Property<string>("EmployeeNameUser")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("BLOB");

                    b.Property<string>("UpdatedBy")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("UpdatedOn")
                        .HasColumnType("TEXT");

                    b.HasKey("ID");

                    b.ToTable("Equipments");
                });

            modelBuilder.Entity("CAAMarketing.Models.Event", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("CreatedBy")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("CreatedOn")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Date")
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.Property<string>("EmployeeNameUser")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<int>("LocationID")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("BLOB");

                    b.Property<string>("UpdatedBy")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("UpdatedOn")
                        .HasColumnType("TEXT");

                    b.HasKey("ID");

                    b.HasIndex("LocationID");

                    b.ToTable("Events");
                });

            modelBuilder.Entity("CAAMarketing.Models.EventLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("EventName")
                        .HasColumnType("TEXT");

                    b.Property<string>("ItemName")
                        .HasColumnType("TEXT");

                    b.Property<int?>("ItemReservationId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LogDate")
                        .HasColumnType("TEXT");

                    b.Property<int>("Quantity")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ItemReservationId");

                    b.ToTable("EventLogs");
                });

            modelBuilder.Entity("CAAMarketing.Models.Inventory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("Cost")
                        .HasColumnType("TEXT");

                    b.Property<string>("CreatedBy")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("CreatedOn")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DismissNotification")
                        .HasColumnType("TEXT");

                    b.Property<string>("EmployeeNameUser")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsLowInventory")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ItemID")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LocationID")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LowInventoryThreshold")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Quantity")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("BLOB");

                    b.Property<string>("UpdatedBy")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("UpdatedOn")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ItemID");

                    b.HasIndex("LocationID");

                    b.ToTable("Inventories");
                });

            modelBuilder.Entity("CAAMarketing.Models.InventoryTransfer", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("CreatedBy")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("CreatedOn")
                        .HasColumnType("TEXT");

                    b.Property<string>("EmployeeNameUser")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<int>("FromLocationId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ItemId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Quantity")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("BLOB");

                    b.Property<int>("ToLocationId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("TransferDate")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("UpdatedBy")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("UpdatedOn")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("FromLocationId");

                    b.HasIndex("ItemId");

                    b.HasIndex("ToLocationId");

                    b.ToTable("InventoryTransfers");
                });

            modelBuilder.Entity("CAAMarketing.Models.Item", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Archived")
                        .HasColumnType("INTEGER");

                    b.Property<int>("CategoryID")
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("Cost")
                        .HasColumnType("TEXT");

                    b.Property<string>("CreatedBy")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("CreatedOn")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DateReceived")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.Property<int>("EmployeeID")
                        .HasColumnType("INTEGER");

                    b.Property<string>("EmployeeNameUser")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<bool>("ItemInvCreated")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("TEXT");

                    b.Property<string>("Notes")
                        .HasMaxLength(4000)
                        .HasColumnType("TEXT");

                    b.Property<int>("Quantity")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("BLOB");

                    b.Property<int>("SupplierID")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UPC")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.Property<string>("UpdatedBy")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("UpdatedOn")
                        .HasColumnType("TEXT");

                    b.HasKey("ID");

                    b.HasIndex("CategoryID");

                    b.HasIndex("EmployeeID");

                    b.HasIndex("SupplierID");

                    b.ToTable("Items");
                });

            modelBuilder.Entity("CAAMarketing.Models.ItemEvent", b =>
                {
                    b.Property<int>("ItemID")
                        .HasColumnType("INTEGER");

                    b.Property<int>("EventID")
                        .HasColumnType("INTEGER");

                    b.HasKey("ItemID", "EventID");

                    b.HasIndex("EventID");

                    b.ToTable("ItemEvents");
                });

            modelBuilder.Entity("CAAMarketing.Models.ItemImages", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("Content")
                        .HasColumnType("BLOB");

                    b.Property<int>("ItemID")
                        .HasColumnType("INTEGER");

                    b.Property<string>("MimeType")
                        .HasMaxLength(255)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ItemID")
                        .IsUnique();

                    b.ToTable("itemImages");
                });

            modelBuilder.Entity("CAAMarketing.Models.ItemLocation", b =>
                {
                    b.Property<int>("LocationID")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ItemID")
                        .HasColumnType("INTEGER");

                    b.HasKey("LocationID", "ItemID");

                    b.HasIndex("ItemID");

                    b.ToTable("ItemLocations");
                });

            modelBuilder.Entity("CAAMarketing.Models.ItemReservation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("EventId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ItemId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("LogBackInDate")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("LoggedOutDate")
                        .HasColumnType("TEXT");

                    b.Property<int>("Quantity")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("ReservedDate")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("ReturnDate")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("EventId");

                    b.HasIndex("ItemId");

                    b.ToTable("ItemReservations");
                });

            modelBuilder.Entity("CAAMarketing.Models.ItemThumbNail", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("Content")
                        .HasColumnType("BLOB");

                    b.Property<int>("ItemID")
                        .HasColumnType("INTEGER");

                    b.Property<string>("MimeType")
                        .HasMaxLength(255)
                        .HasColumnType("TEXT");

                    b.HasKey("ID");

                    b.HasIndex("ItemID")
                        .IsUnique();

                    b.ToTable("ItemThumbNails");
                });

            modelBuilder.Entity("CAAMarketing.Models.Location", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("TEXT");

                    b.Property<string>("CreatedBy")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("CreatedOn")
                        .HasColumnType("TEXT");

                    b.Property<string>("EmployeeNameUser")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("TEXT");

                    b.Property<string>("Phone")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("BLOB");

                    b.Property<string>("UpdatedBy")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("UpdatedOn")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Locations");
                });

            modelBuilder.Entity("CAAMarketing.Models.Receiving", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("Cost")
                        .HasColumnType("TEXT");

                    b.Property<string>("CreatedBy")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("CreatedOn")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DateMade")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DeliveryDate")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("EmployeeNameUser")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<int>("ItemID")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LocationID")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Quantity")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("BLOB");

                    b.Property<string>("UpdatedBy")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("UpdatedOn")
                        .HasColumnType("TEXT");

                    b.HasKey("ID");

                    b.HasIndex("ItemID");

                    b.HasIndex("LocationID");

                    b.ToTable("Orders");
                });

            modelBuilder.Entity("CAAMarketing.Models.Subscription", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("CreatedBy")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("CreatedOn")
                        .HasColumnType("TEXT");

                    b.Property<int>("EmployeeID")
                        .HasColumnType("INTEGER");

                    b.Property<string>("EmployeeNameUser")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<string>("PushAuth")
                        .HasMaxLength(512)
                        .HasColumnType("TEXT");

                    b.Property<string>("PushEndpoint")
                        .HasMaxLength(512)
                        .HasColumnType("TEXT");

                    b.Property<string>("PushP256DH")
                        .HasMaxLength(512)
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("BLOB");

                    b.Property<string>("UpdatedBy")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("UpdatedOn")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("EmployeeID");

                    b.ToTable("Subscriptions");
                });

            modelBuilder.Entity("CAAMarketing.Models.Supplier", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.Property<string>("CreatedBy")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("CreatedOn")
                        .HasColumnType("TEXT");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(70)
                        .HasColumnType("TEXT");

                    b.Property<string>("EmployeeNameUser")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("TEXT");

                    b.Property<string>("Phone")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("BLOB");

                    b.Property<string>("UpdatedBy")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("UpdatedOn")
                        .HasColumnType("TEXT");

                    b.HasKey("ID");

                    b.ToTable("Suppliers");
                });

            modelBuilder.Entity("CAAMarketing.ViewModels.InventoryReportVM", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Category")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Cost")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("DateReceived")
                        .HasColumnType("TEXT");

                    b.Property<string>("ItemName")
                        .HasColumnType("TEXT");

                    b.Property<string>("Location")
                        .HasColumnType("TEXT");

                    b.Property<int>("LocationID")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Notes")
                        .HasColumnType("TEXT");

                    b.Property<int>("Quantity")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Supplier")
                        .HasColumnType("TEXT");

                    b.Property<string>("UPC")
                        .HasColumnType("TEXT");

                    b.HasKey("ID");

                    b.ToView("InventoryReports");
                });

            modelBuilder.Entity("CAAMarketing.Models.Archive", b =>
                {
                    b.HasOne("CAAMarketing.Models.Item", "Item")
                        .WithMany()
                        .HasForeignKey("ItemID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Item");
                });

            modelBuilder.Entity("CAAMarketing.Models.Event", b =>
                {
                    b.HasOne("CAAMarketing.Models.Location", "Location")
                        .WithMany()
                        .HasForeignKey("LocationID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Location");
                });

            modelBuilder.Entity("CAAMarketing.Models.EventLog", b =>
                {
                    b.HasOne("CAAMarketing.Models.ItemReservation", "ItemReservation")
                        .WithMany("EventLogs")
                        .HasForeignKey("ItemReservationId");

                    b.Navigation("ItemReservation");
                });

            modelBuilder.Entity("CAAMarketing.Models.Inventory", b =>
                {
                    b.HasOne("CAAMarketing.Models.Item", "Item")
                        .WithMany("Inventories")
                        .HasForeignKey("ItemID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("CAAMarketing.Models.Location", "Location")
                        .WithMany("Inventories")
                        .HasForeignKey("LocationID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Item");

                    b.Navigation("Location");
                });

            modelBuilder.Entity("CAAMarketing.Models.InventoryTransfer", b =>
                {
                    b.HasOne("CAAMarketing.Models.Location", "FromLocation")
                        .WithMany("InventoryTransfersFrom")
                        .HasForeignKey("FromLocationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("CAAMarketing.Models.Item", "Item")
                        .WithMany("InventoryTransfers")
                        .HasForeignKey("ItemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("CAAMarketing.Models.Location", "ToLocation")
                        .WithMany("InventoryTransfersTo")
                        .HasForeignKey("ToLocationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("FromLocation");

                    b.Navigation("Item");

                    b.Navigation("ToLocation");
                });

            modelBuilder.Entity("CAAMarketing.Models.Item", b =>
                {
                    b.HasOne("CAAMarketing.Models.Category", "Category")
                        .WithMany("Items")
                        .HasForeignKey("CategoryID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("CAAMarketing.Models.Employee", "Employee")
                        .WithMany("Items")
                        .HasForeignKey("EmployeeID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("CAAMarketing.Models.Supplier", "Supplier")
                        .WithMany()
                        .HasForeignKey("SupplierID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Category");

                    b.Navigation("Employee");

                    b.Navigation("Supplier");
                });

            modelBuilder.Entity("CAAMarketing.Models.ItemEvent", b =>
                {
                    b.HasOne("CAAMarketing.Models.Event", "Event")
                        .WithMany()
                        .HasForeignKey("EventID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("CAAMarketing.Models.Item", "Item")
                        .WithMany()
                        .HasForeignKey("ItemID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Event");

                    b.Navigation("Item");
                });

            modelBuilder.Entity("CAAMarketing.Models.ItemImages", b =>
                {
                    b.HasOne("CAAMarketing.Models.Item", "Item")
                        .WithOne("ItemImages")
                        .HasForeignKey("CAAMarketing.Models.ItemImages", "ItemID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Item");
                });

            modelBuilder.Entity("CAAMarketing.Models.ItemLocation", b =>
                {
                    b.HasOne("CAAMarketing.Models.Item", "Item")
                        .WithMany("ItemLocations")
                        .HasForeignKey("ItemID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("CAAMarketing.Models.Location", "Location")
                        .WithMany("ItemLocations")
                        .HasForeignKey("LocationID")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Item");

                    b.Navigation("Location");
                });

            modelBuilder.Entity("CAAMarketing.Models.ItemReservation", b =>
                {
                    b.HasOne("CAAMarketing.Models.Event", "Event")
                        .WithMany()
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("CAAMarketing.Models.Item", "Item")
                        .WithMany("ItemReservations")
                        .HasForeignKey("ItemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Event");

                    b.Navigation("Item");
                });

            modelBuilder.Entity("CAAMarketing.Models.ItemThumbNail", b =>
                {
                    b.HasOne("CAAMarketing.Models.Item", "Item")
                        .WithOne("ItemThumbNail")
                        .HasForeignKey("CAAMarketing.Models.ItemThumbNail", "ItemID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Item");
                });

            modelBuilder.Entity("CAAMarketing.Models.Receiving", b =>
                {
                    b.HasOne("CAAMarketing.Models.Item", "Item")
                        .WithMany("Orders")
                        .HasForeignKey("ItemID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("CAAMarketing.Models.Location", "Location")
                        .WithMany()
                        .HasForeignKey("LocationID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Item");

                    b.Navigation("Location");
                });

            modelBuilder.Entity("CAAMarketing.Models.Subscription", b =>
                {
                    b.HasOne("CAAMarketing.Models.Employee", "Employee")
                        .WithMany("Subscriptions")
                        .HasForeignKey("EmployeeID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Employee");
                });

            modelBuilder.Entity("CAAMarketing.Models.Category", b =>
                {
                    b.Navigation("Items");
                });

            modelBuilder.Entity("CAAMarketing.Models.Employee", b =>
                {
                    b.Navigation("Items");

                    b.Navigation("Subscriptions");
                });

            modelBuilder.Entity("CAAMarketing.Models.Item", b =>
                {
                    b.Navigation("Inventories");

                    b.Navigation("InventoryTransfers");

                    b.Navigation("ItemImages");

                    b.Navigation("ItemLocations");

                    b.Navigation("ItemReservations");

                    b.Navigation("ItemThumbNail");

                    b.Navigation("Orders");
                });

            modelBuilder.Entity("CAAMarketing.Models.ItemReservation", b =>
                {
                    b.Navigation("EventLogs");
                });

            modelBuilder.Entity("CAAMarketing.Models.Location", b =>
                {
                    b.Navigation("Inventories");

                    b.Navigation("InventoryTransfersFrom");

                    b.Navigation("InventoryTransfersTo");

                    b.Navigation("ItemLocations");
                });
#pragma warning restore 612, 618
        }
    }
}
