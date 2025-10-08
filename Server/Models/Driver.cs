using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppServer.Models;

public partial class Driver
{
    [Key]
    [StringLength(15)]
    public string PersonId { get; set; } = null!;

    [StringLength(5)]
    public string DepotId { get; set; } = null!;

    [StringLength(100)]
    public string Position { get; set; } = null!;

    public DateOnly EmploymentDate { get; set; }

    public string? Notes { get; set; }

    [ForeignKey("DepotId")]
    [InverseProperty("Drivers")]
    public virtual Depot Depot { get; set; } = null!;

    [ForeignKey("PersonId")]
    [InverseProperty("Driver")]
    public virtual Person Person { get; set; } = null!;

    [InverseProperty("Driver")]
    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}
