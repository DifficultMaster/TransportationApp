using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppServer.Models;

public partial class Engine
{
    [Key]
    [StringLength(5)]
    public string EngineId { get; set; } = null!;

    [StringLength(50)]
    public string Make { get; set; } = null!;

    [StringLength(100)]
    public string Model { get; set; } = null!;

    [StringLength(15)]
    public string Propulsion { get; set; } = null!;

    [Column(TypeName = "money")]
    public decimal CostOfOperationPer100km { get; set; }

    [InverseProperty("Engine")]
    public virtual ICollection<VehicleType> VehicleTypes { get; set; } = new List<VehicleType>();
}
