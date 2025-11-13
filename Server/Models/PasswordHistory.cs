using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppServer.Models;

public partial class PasswordHistory
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(15)]
    public string PersonId { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string HashedPassword { get; set; } = null!;

    public DateTime DateChanged { get; set; }

    [ForeignKey("PersonId")]
    [InverseProperty("PasswordHistories")]
    public virtual Person Person { get; set; } = null!;
}
