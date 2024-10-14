using CAAMarketing.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CAAMarketing.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CAAMarketing.Data
{
    public class CAAContext : DbContext
    {
        //To give access to IHttpContextAccessor for Audit Data with IAuditable
        private readonly IHttpContextAccessor _httpContextAccessor;

        //Property to hold the UserName value
        public string UserName
        {
            get; private set;
        }

        public CAAContext(DbContextOptions<CAAContext> options, IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
            if (_httpContextAccessor.HttpContext != null)
            {
                //We have a HttpContext, but there might not be anyone Authenticated
                UserName = _httpContextAccessor.HttpContext?.User.Identity.Name;
                UserName ??= "Unknown";
            }
            else
            {
                //No HttpContext so seeding data
                UserName = "Seed Data";
            }
        }

        public DbSet<ItemImages> itemImages { get; set; }

        public DbSet<ItemThumbNail> ItemThumbNails { get; set; }


        public DbSet<Item> Items { get; set; }

        public DbSet<Supplier> Suppliers { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Receiving> Orders { get; set; }

        public DbSet<Event> Events { get; set; }

        public DbSet<ItemEvent> ItemEvents { get; set; }

        public DbSet<Location> Locations { get; set; }


        public DbSet<ItemLocation> ItemLocations { get; set; }


        public DbSet<Inventory> Inventories { get; set; }

        public DbSet<Equipment> Equipments { get; set; }

        public DbSet<Employee> Employees { get; set; }


        public DbSet<Subscription> Subscriptions { get; set; }

        public DbSet<Transfer> Transfers { get; set; }

        public DbSet<InventoryTransfer> InventoryTransfers { get; set; }

        public DbSet<Archive> Archives { get; set; }

        public DbSet<InventoryReportVM> InventoryReports { get; set; }

        public DbSet<ItemReservation> ItemReservations { get; set; }

        public DbSet<EventLog> EventLogs { get; set; }

        public DbSet<MissingItemLog> MissingItemLogs { get; set; }

        public DbSet<MissingTransitItem> MissingTransitItems { get; set; }

        public DbSet<Audit> AuditLogs { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {


            modelBuilder.Entity<ItemReservation>()
        .HasQueryFilter(p => !p.IsDeleted);



            //Many to Many for Play Table to Musician
            modelBuilder.Entity<ItemLocation>()
                .HasKey(p => new { p.LocationID, p.ItemID });


            //Many to Many for Play Table to Musician
            modelBuilder.Entity<ItemEvent>()
                .HasKey(p => new { p.ItemID, p.EventID });

            modelBuilder.Entity<InventoryTransfer>()
            .HasOne(t => t.Item)
            .WithMany(i => i.InventoryTransfers)
            .HasForeignKey(t => t.ItemId);


            modelBuilder.Entity<InventoryTransfer>()
                .HasOne(t => t.FromLocation)
                .WithMany(l => l.InventoryTransfersFrom)
                .HasForeignKey(t => t.FromLocationId);

            modelBuilder.Entity<InventoryTransfer>()
                .HasOne(t => t.ToLocation)
                .WithMany(l => l.InventoryTransfersTo)
                .HasForeignKey(t => t.ToLocationId);

            modelBuilder.Entity<InventoryTransfer>()
                .HasOne(t => t.Transfer)
                .WithMany(l => l.InventoryTransfers)
                .HasForeignKey(t => t.TransferId);

            modelBuilder.Entity<Item>()
            .HasOne(t => t.Employee)
            .WithMany(l => l.Items)
            .HasForeignKey(t => t.EmployeeID);

            modelBuilder.Entity<ItemReservation>()
        .HasOne(i => i.Item)
        .WithMany(ir => ir.ItemReservations)
        .HasForeignKey(ir => ir.ItemId)
        .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MissingItemLog>()
                .HasOne(i => i.Item)
                .WithMany(ir => ir.MissingItemLogs)
                .HasForeignKey(ir => ir.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MissingTransitItem>()
                .HasOne(i => i.Item)
                .WithMany(ir => ir.MissingTransitItems)
                .HasForeignKey(ir => ir.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
            //Add a unique index to the Employee Email
            modelBuilder.Entity<Employee>()
            .HasIndex(a => new {
                a.Email
            })
            .IsUnique();

            //For the InventoryReport ViewModel
            //Note: The Database View name is InventoryReports
            modelBuilder
                .Entity<InventoryReportVM>()
                .ToView(nameof(InventoryReports))
                .HasKey(a => a.ID);


            //Prevents cascade delete from Introment class to musician
            modelBuilder.Entity<ItemLocation>()
                .HasOne<Location>(i => i.Location)
                .WithMany(m => m.ItemLocations)
                .HasForeignKey(m => m.LocationID)
                .OnDelete(DeleteBehavior.Restrict);


            //Prevent Cascade Delete from Location to Inventory
            //so we are prevented from deleting a location with Inventory
            //modelBuilder.Entity<Location>()
            //    .HasMany<Inventory>(i => i.)
            //    .WithOne(l => l.Location)
            //    .HasForeignKey(l => l.LocationID)
            //    .OnDelete(DeleteBehavior.Restrict);
            ////Prevents cascade delete from Introment class to musician
            //modelBuilder.Entity<Musician>()
            //    .HasOne<Instrument>(i => i.Instrument)
            //    .WithMany(m => m.Musicians)
            //    .HasForeignKey(m => m.InstrumentID)
            //    .OnDelete(DeleteBehavior.Restrict);

            ////Cascade Delete for The Play Table
            //modelBuilder.Entity<Play>()
            //    .HasOne(i => i.Instrument)
            //    .WithMany(p => p.Plays)
            //    .HasForeignKey(i => i.InstrumentID)
            //    .OnDelete(DeleteBehavior.Restrict);

            ////Makes a unique key to the SIN of the musician class
            //modelBuilder.Entity<Musician>()
            //    .HasIndex(m => m.SIN)
            //    .IsUnique();
        }

        //for audit trail
        public async Task<int> SaveChangesAudit()
        {
            //In the controlles, call this instead of SaveChangesAsync()
            //to log the changes for auditing
            OnBeforeSavingAudit();
            var result = await base.SaveChangesAsync();
            return result;
        }

        private void OnBeforeSavingAudit()
        {
            ChangeTracker.DetectChanges();
            var auditEntries = new List<AuditEntry>();
            var when = DateTime.Now;//Use as consistent timestamp for all changes
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is Audit || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                //Handle the basic auditing
                if (entry.Entity is IAuditable trackable)
                {
                    switch (entry.State)
                    {
                        case EntityState.Modified:
                            trackable.UpdatedOn = when;
                            trackable.UpdatedBy = UserName;
                            break;

                        case EntityState.Added:
                            trackable.CreatedOn = when;
                            trackable.CreatedBy = UserName;
                            trackable.UpdatedOn = when;
                            trackable.UpdatedBy = UserName;
                            break;
                    }
                }
                var auditEntry = new AuditEntry(entry);

                //Get the name of the Entity
                Type TypeOfEntity = entry.Entity.GetType();
                auditEntry.EntityName = TypeOfEntity.Name;

                //Establish the type of change but, this next line
                //basically "converts" the enum values in EntityState
                //to my own prefered values in the enum MyEntityState
                //For example, EntityState.Deleted becomes "Removed"
                auditEntry.AuditType = ((MyEntityState)entry.State).ToString();

                auditEntry.UserId = UserName;
                auditEntry.UserName = UserName;//See if we can get this from the cookie
                auditEntries.Add(auditEntry);
                foreach (var property in entry.Properties)
                {
                    string propertyName = property.Metadata.Name;
                    MemberInfo theProperty = TypeOfEntity.GetProperty(propertyName);

                    if (property.Metadata.IsPrimaryKey())
                    {
                        //Note: a composite PK will just have multiple entries
                        auditEntry.KeyValues[propertyName] = property.CurrentValue;
                        continue;
                    }
                    else if (property.Metadata.IsForeignKey())
                    {
                        auditEntry.FKeyValues[propertyName] = property.CurrentValue;
                    }
                    else
                    {
                        //this next bit of code gets the Display(Name= value if there is one and uses it instead
                        //of the property name
                        var displayName = theProperty?.GetCustomAttribute(typeof(DisplayAttribute)) as DisplayAttribute;
                        if (displayName != null) { propertyName = displayName.Name; }
                    }

                    //Avoid saving Byte Arrays in the audit file
                    var isByteArray = (theProperty?.ToString().Contains("Byte[]")) == null ? true : theProperty?.ToString().Contains("Byte[]");
                    if ((bool)!isByteArray)
                    {
                        switch (entry.State)
                        {
                            case EntityState.Added:
                                auditEntry.NewValues[propertyName] = property.CurrentValue;
                                break;
                            case EntityState.Deleted:
                                auditEntry.OldValues[propertyName] = property.OriginalValue;
                                break;
                            case EntityState.Modified:
                                if (property.IsModified)
                                {
                                    auditEntry.OldValues[propertyName] = property.OriginalValue;
                                    auditEntry.NewValues[propertyName] = property.CurrentValue;
                                }
                                break;
                        }
                    }

                }
            }
            foreach (var auditEntry in auditEntries)
            {
                AuditLogs.Add(auditEntry.ToAudit(when));
            }
        }

        //The following code is to override both the sync and async SaveChanges methods
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            OnBeforeSaving();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            OnBeforeSaving();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void OnBeforeSaving()
        {
            var entries = ChangeTracker.Entries();
            foreach (var entry in entries)
            {
                if (entry.Entity is IAuditable trackable)
                {
                    var now = DateTime.UtcNow;
                    switch (entry.State)
                    {
                        case EntityState.Modified:
                            trackable.UpdatedOn = now;
                            trackable.UpdatedBy = UserName;
                            break;

                        case EntityState.Added:
                            trackable.CreatedOn = now;
                            trackable.CreatedBy = UserName;
                            trackable.UpdatedOn = now;
                            trackable.UpdatedBy = UserName;
                            break;
                    }
                }
            }
        }

        public DbSet<CAAMarketing.Models.Category> Category { get; set; }

        public DbSet<CAAMarketing.ViewModels.InventoryReportVM> InventoryReportVM { get; set; }

        

        //public override int SaveChanges(bool acceptAllChangesOnSuccess)
        //{
        //    //OnBeforeSaving();
        //    return base.SaveChanges(acceptAllChangesOnSuccess);
        //}

        //public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        //{
        //    //OnBeforeSaving();
        //    return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        //}

    }
}
