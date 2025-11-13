using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppServer.Models;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<Depot> Depots { get; set; }

    public virtual DbSet<Dispatcher> Dispatchers { get; set; }

    public virtual DbSet<Driver> Drivers { get; set; }

    public virtual DbSet<Engine> Engines { get; set; }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<Person> Persons { get; set; }

    public virtual DbSet<Route> Routes { get; set; }

    public virtual DbSet<Schedule> Schedules { get; set; }

    public virtual DbSet<Stop> Stops { get; set; }

    public virtual DbSet<Validation> Validations { get; set; }

    public virtual DbSet<Vehicle> Vehicles { get; set; }

    public virtual DbSet<VehicleType> VehicleTypes { get; set; }

    public virtual DbSet<PasswordHistory> PasswordHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.AddressId).HasName("PK__Addresse__091C2AFB58B1011B");

            entity.Property(e => e.BuildingNo).HasDefaultValue("1");
            entity.Property(e => e.City).HasDefaultValue("?");
            entity.Property(e => e.District).HasDefaultValue("?");
            entity.Property(e => e.Street).HasDefaultValue("?");           
        });

        modelBuilder.Entity<Depot>(entity =>
        {
            entity.HasKey(e => e.DepotId).HasName("PK__Depots__42C1C217052087E5");

            entity.Property(e => e.Name).HasDefaultValue("?");
            entity.Property(e => e.Operator).HasDefaultValue("?");
            entity.Property(e => e.Type).HasDefaultValue("???????");

            entity.HasOne(d => d.Address).WithMany(p => p.Depots)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Depots__AddressI__48CFD27E");
        });

        modelBuilder.Entity<Dispatcher>(entity =>
        {
            entity.HasKey(e => e.PersonId).HasName("PK__Dispatch__AA2FFBE5D6D207CC");

            entity.Property(e => e.EmploymentDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Position).HasDefaultValue("?");

            entity.HasOne(d => d.Depot).WithMany(p => p.Dispatchers)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Dispatche__Depot__00200768");

            entity.HasOne(d => d.Person).WithOne(p => p.Dispatcher)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Dispatche__Perso__7F2BE32F");
        });

        modelBuilder.Entity<Driver>(entity =>
        {
            entity.HasKey(e => e.PersonId).HasName("PK__Drivers__AA2FFBE5611DF48E");

            entity.Property(e => e.EmploymentDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Position).HasDefaultValue("?");

            entity.HasOne(d => d.Depot).WithMany(p => p.Drivers)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Drivers__DepotId__7A672E12");

            entity.HasOne(d => d.Person).WithOne(p => p.Driver)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Drivers__PersonI__797309D9");
        });

        modelBuilder.Entity<Engine>(entity =>
        {
            entity.HasKey(e => e.EngineId).HasName("PK__Engines__7BBCE904600AB53F");

            entity.Property(e => e.CostOfOperationPer100km).HasDefaultValue(1m);
            entity.Property(e => e.Make).HasDefaultValue("?");
            entity.Property(e => e.Model).HasDefaultValue("?");
            entity.Property(e => e.Propulsion).HasDefaultValue("??????");
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.EventId).HasName("PK__Events__7944C8107E6BE843");

            entity.Property(e => e.DateTime).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Person).WithMany(p => p.Events)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Events__PersonId__0E6E26BF");
        });

        modelBuilder.Entity<Person>(entity =>
        {
            entity.HasKey(e => e.PersonId).HasName("PK__Persons__AA2FFBE547DCB67F");

            entity.Property(e => e.FirstName).HasDefaultValue("?");
            entity.Property(e => e.LastName).HasDefaultValue("?");
            entity.Property(e => e.SurName).HasDefaultValue("?");
        });

        modelBuilder.Entity<PasswordHistory>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(d => d.Person)
                .WithMany(p => p.PasswordHistories)
                .HasForeignKey(d => d.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Route>(entity =>
        {
            entity.HasKey(e => e.RouteId).HasName("PK__Routes__80979B4D5A15FE99");

            entity.Property(e => e.Length).HasDefaultValue(1m);
            entity.Property(e => e.NumberOfStops).HasDefaultValue(2);
            entity.Property(e => e.Type).HasDefaultValue("??????");

            entity.HasOne(d => d.FirstStop).WithMany(p => p.RouteFirstStops)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Routes__FirstSto__6E01572D");

            entity.HasOne(d => d.LastStop).WithMany(p => p.RouteLastStops)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Routes__LastStop__6EF57B66");
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PK__Schedule__9C8A5B4976CB6A79");

            entity.Property(e => e.EndDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.StartDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Driver).WithMany(p => p.Schedules)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Schedule__Driver__06CD04F7");

            entity.HasOne(d => d.Route).WithMany(p => p.Schedules)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Schedule__RouteI__04E4BC85");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.Schedules)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Schedule__Vehicl__05D8E0BE");
        });

        modelBuilder.Entity<Stop>(entity =>
        {
            entity.HasKey(e => e.StopId).HasName("PK__Stops__EB6A38F4C58937EF");

            entity.Property(e => e.District).HasDefaultValue("?");
            entity.Property(e => e.Name).HasDefaultValue("?");
            entity.Property(e => e.Operator).HasDefaultValue("?");
            entity.Property(e => e.Type).HasDefaultValue("?");
        });

        modelBuilder.Entity<Validation>(entity =>
        {
            entity.HasKey(e => e.ValidationId).HasName("PK__Validati__FA0B508521D2DF9D");

            entity.Property(e => e.DateTime).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Schedule).WithMany(p => p.Validations)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Validatio__Sched__0A9D95DB");
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.VehicleId).HasName("PK__Vehicles__476B5492E397FD9A");

            entity.Property(e => e.BeginOperationYear).HasDefaultValueSql("(datepart(year,getdate()))");
            entity.Property(e => e.IsActive).HasDefaultValue("??");

            entity.HasOne(d => d.Depot).WithMany(p => p.Vehicles)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Vehicles__DepotI__5BE2A6F2");

            entity.HasOne(d => d.VehicleType).WithMany(p => p.Vehicles)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Vehicles__Vehicl__5AEE82B9");
        });

        modelBuilder.Entity<VehicleType>(entity =>
        {
            entity.HasKey(e => e.VehicleTypeId).HasName("PK__VehicleT__9F449643D83C0789");

            entity.Property(e => e.Capacity).HasDefaultValue(1);
            entity.Property(e => e.Make).HasDefaultValue("?");
            entity.Property(e => e.ManufactureCountry).HasDefaultValue("???????");
            entity.Property(e => e.Model).HasDefaultValue("?");
            entity.Property(e => e.ModelYear).HasDefaultValueSql("(datepart(year,getdate()))");
            entity.Property(e => e.Type).HasDefaultValue("???????");

            entity.HasOne(d => d.Engine).WithMany(p => p.VehicleTypes)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__VehicleTy__Engin__5441852A");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
