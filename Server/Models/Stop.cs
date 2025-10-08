using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppServer.Models;

public partial class Stop
{
    [Key]
    [StringLength(5)]
    public string StopId { get; set; } = null!;

    [StringLength(100)]
    public string District { get; set; } = null!;

    [StringLength(100)]
    public string Name { get; set; } = null!;

    [StringLength(50)]
    public string Type { get; set; } = null!;

    [StringLength(100)]
    public string Operator { get; set; } = null!;

    [InverseProperty("FirstStop")]
    public virtual ICollection<Route> RouteFirstStops { get; set; } = new List<Route>();

    [InverseProperty("LastStop")]
    public virtual ICollection<Route> RouteLastStops { get; set; } = new List<Route>();
}
