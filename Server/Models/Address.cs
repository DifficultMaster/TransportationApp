using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppServer.Models;

public partial class Address
{
    [Key]
    [StringLength(5)]
    public string AddressId { get; set; } = null!;

    [StringLength(50)]
    public string City { get; set; } = null!;

    [StringLength(100)]
    public string District { get; set; } = null!;

    [StringLength(100)]
    public string Street { get; set; } = null!;

    [StringLength(5)]
    public string BuildingNo { get; set; } = null!;

    [InverseProperty("Address")]
    public virtual ICollection<Depot> Depots { get; set; } = new List<Depot>();
}
