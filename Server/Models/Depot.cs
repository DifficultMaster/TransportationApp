using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppServer.Models;

public partial class Depot
{
    [Key]
    [StringLength(5)]
    public string DepotId { get; set; } = null!;

    [StringLength(100)]
    public string Name { get; set; } = null!;

    [StringLength(7)]
    public string Type { get; set; } = null!;

    [StringLength(100)]
    public string Operator { get; set; } = null!;

    [StringLength(5)]
    public string AddressId { get; set; } = null!;

    [StringLength(13)]
    [Unicode(false)]
    public string? ContactNumber { get; set; }

    [ForeignKey("AddressId")]
    [InverseProperty("Depots")]
    public virtual Address Address { get; set; } = null!;

    [InverseProperty("Depot")]
    public virtual ICollection<Dispatcher> Dispatchers { get; set; } = new List<Dispatcher>();

    [InverseProperty("Depot")]
    public virtual ICollection<Driver> Drivers { get; set; } = new List<Driver>();

    [InverseProperty("Depot")]
    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
