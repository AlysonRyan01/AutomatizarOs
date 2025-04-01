using AutomatizarOs.Core.Enums;

namespace AutomatizarOs.Core.Models;

public class ServiceOrder
{
    public long Id { get; set; }
    
    public EServiceOrderStatus EServiceOrderStatus { get; set; } = EServiceOrderStatus.Entered;
    
    public EEnterprise EEnterprise { get; set; } = EEnterprise.Particular;

    public EProduct ProductType { get; set; }
    
    public string ProductBrand { get; set; } = String.Empty;
    public string ProductModel { get; set; } = String.Empty;
    public string ProductSerialNumber { get; set; } = String.Empty;
    public string ProductDefect { get; set; } = String.Empty;
    public string? Solution { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    
    public DateTime EntryDate  { get; set; } = DateTime.Now;
    public DateTime? InspectionDate { get; set; }
    public DateTime? RepairDate { get; set; }
    public DateTime? DeliveryDate { get; set; }

    public long CustomerId { get; set; }

    public ERepair ERepair { get; set; } = ERepair.Entered;

    public EUnrepaired EUnrepaired { get; set; }

    public decimal PartCost { get; set; }
    public decimal LaborCost { get; set; }
}