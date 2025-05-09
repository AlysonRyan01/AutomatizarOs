using AutomatizarOs.Core.Enums;
using Dapper.Contrib.Extensions;

namespace AutomatizarOs.Core.Models;

[Table("os")]
public class ServiceOrder
{
    [Key]
    public long Id { get; set; }

    public EServiceOrderStatus EServiceOrderStatus { get; set; } = EServiceOrderStatus.Entered;

    public EEnterprise EEnterprise { get; set; } = EEnterprise.Particular;

    public EProduct ProductType { get; set; }

    public string ProductBrand { get; set; } = string.Empty;
    public string ProductModel { get; set; } = string.Empty;
    public string ProductSerialNumber { get; set; } = string.Empty;
    public string ProductDefect { get; set; } = string.Empty;
    public string? Solution { get; set; }
    public decimal? Amount { get; set; }

    public DateTime EntryDate { get; set; }
    public DateTime? InspectionDate { get; set; }
    public DateTime? RepairDate { get; set; }
    public DateTime? DeliveryDate { get; set; }

    public long CustomerId { get; set; }

    public ERepair ERepair { get; set; } = ERepair.Entered;

    public EUnrepaired EUnrepaired { get; set; }

    public decimal PartCost { get; set; }
    public decimal LaborCost { get; set; }

    public Customer Customer { get; set; } = null!;

    public string Location { get; set; } = string.Empty;
}