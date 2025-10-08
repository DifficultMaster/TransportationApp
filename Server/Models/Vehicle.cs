using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppServer.Models;

public partial class Vehicle
{
    [Key]
    [StringLength(8)]
    public string VehicleId { get; set; } = null!;

    [StringLength(5)]
    public string VehicleTypeId { get; set; } = null!;

    [StringLength(5)]
    public string DepotId { get; set; } = null!;

    [StringLength(3)]
    public string IsActive { get; set; } = null!;

    public int BeginOperationYear { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? GalleryUrl { get; set; }

    public string? Notes { get; set; }

    [ForeignKey("DepotId")]
    [InverseProperty("Vehicles")]
    public virtual Depot Depot { get; set; } = null!;

    [InverseProperty("Vehicle")]
    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    [ForeignKey("VehicleTypeId")]
    [InverseProperty("Vehicles")]
    public virtual VehicleType VehicleType { get; set; } = null!;
}
