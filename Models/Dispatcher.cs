using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppServer.Models;

public partial class Dispatcher
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
    [InverseProperty("Dispatchers")]
    public virtual Depot Depot { get; set; } = null!;

    [ForeignKey("PersonId")]
    [InverseProperty("Dispatcher")]
    public virtual Person Person { get; set; } = null!;
}
