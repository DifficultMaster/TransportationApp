using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppServer.Models;

public partial class Route
{
    [Key]
    [StringLength(4)]
    public string RouteId { get; set; } = null!;

    [StringLength(5)]
    public string FirstStopId { get; set; } = null!;

    [StringLength(5)]
    public string LastStopId { get; set; } = null!;

    public int NumberOfStops { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal Length { get; set; }

    [StringLength(9)]
    public string Type { get; set; } = null!;

    [Column(TypeName = "money")]
    public decimal Price { get; set; }

    public int Capacity { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? MapUrl { get; set; }

    public string? Notes { get; set; }

    [ForeignKey("FirstStopId")]
    [InverseProperty("RouteFirstStops")]
    public virtual Stop FirstStop { get; set; } = null!;

    [ForeignKey("LastStopId")]
    [InverseProperty("RouteLastStops")]
    public virtual Stop LastStop { get; set; } = null!;

    [InverseProperty("Route")]
    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}
