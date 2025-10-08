using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppServer.Models;

public partial class VehicleType
{
    [Key]
    [StringLength(5)]
    public string VehicleTypeId { get; set; } = null!;

    [StringLength(9)]
    public string Type { get; set; } = null!;

    [StringLength(100)]
    public string ManufactureCountry { get; set; } = null!;

    public int ModelYear { get; set; }

    [StringLength(50)]
    public string Make { get; set; } = null!;

    [StringLength(100)]
    public string Model { get; set; } = null!;

    [StringLength(5)]
    public string EngineId { get; set; } = null!;

    public int Capacity { get; set; }

    [ForeignKey("EngineId")]
    [InverseProperty("VehicleTypes")]
    public virtual Engine Engine { get; set; } = null!;

    [InverseProperty("VehicleType")]
    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
