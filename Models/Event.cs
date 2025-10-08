using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppServer.Models;

public partial class Event
{
    [Key]
    public int EventId { get; set; }

    [StringLength(15)]
    public string PersonId { get; set; } = null!;

    [StringLength(20)]
    public string TableName { get; set; } = null!;

    [StringLength(20)]
    public string ActionType { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime DateTime { get; set; }

    public string Query { get; set; } = null!;

    [ForeignKey("PersonId")]
    [InverseProperty("Events")]
    public virtual Person Person { get; set; } = null!;
}
