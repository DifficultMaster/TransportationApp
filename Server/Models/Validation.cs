using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppServer.Models;

public partial class Validation
{
    [Key]
    public int ValidationId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime DateTime { get; set; }

    [StringLength(15)]
    public string ScheduleId { get; set; } = null!;

    [ForeignKey("ScheduleId")]
    [InverseProperty("Validations")]
    public virtual Schedule Schedule { get; set; } = null!;
}
