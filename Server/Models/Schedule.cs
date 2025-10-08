using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppServer.Models;

[Table("Schedule")]
public partial class Schedule
{
    [Key]
    [StringLength(15)]
    public string ScheduleId { get; set; } = null!;

    [StringLength(4)]
    public string RouteId { get; set; } = null!;

    [StringLength(8)]
    public string VehicleId { get; set; } = null!;

    [StringLength(15)]
    public string DriverId { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime StartDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime EndDate { get; set; }

    [ForeignKey("DriverId")]
    [InverseProperty("Schedules")]
    public virtual Driver Driver { get; set; } = null!;

    [ForeignKey("RouteId")]
    [InverseProperty("Schedules")]
    public virtual Route Route { get; set; } = null!;

    [InverseProperty("Schedule")]
    public virtual ICollection<Validation> Validations { get; set; } = new List<Validation>();

    [ForeignKey("VehicleId")]
    [InverseProperty("Schedules")]
    public virtual Vehicle Vehicle { get; set; } = null!;
}
