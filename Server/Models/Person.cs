using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppServer.Models;

[Index("Login", Name = "UQ__Persons__5E55825B3FDB5EB9", IsUnique = true)]
public partial class Person
{
    [Key]
    [StringLength(15)]
    public string PersonId { get; set; } = null!;

    [StringLength(100)]
    public string FirstName { get; set; } = null!;

    [StringLength(100)]
    public string SurName { get; set; } = null!;

    [StringLength(100)]
    public string LastName { get; set; } = null!;

    [StringLength(13)]
    [Unicode(false)]
    public string? ContactNumber { get; set; }

    [StringLength(100)]
    public string Login { get; set; } = null!;

    [StringLength(100)]
    public string HashedPassword { get; set; } = null!;

    [InverseProperty("Person")]
    public virtual ICollection<PasswordHistory> PasswordHistories { get; set; } = new List<PasswordHistory>();

    [InverseProperty("Person")]
    public virtual Dispatcher? Dispatcher { get; set; }

    [InverseProperty("Person")]
    public virtual Driver? Driver { get; set; }

    [InverseProperty("Person")]
    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
}
