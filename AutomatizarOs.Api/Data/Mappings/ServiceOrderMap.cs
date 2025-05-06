using AutomatizarOs.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutomatizarOs.Api.Data.Mappings;

public class ServiceOrderMap : IEntityTypeConfiguration<ServiceOrder>
{
    public void Configure(EntityTypeBuilder<ServiceOrder> builder)
    {
        builder.ToTable("ServiceOrders");
        
        builder.HasKey(so => so.Id);

        builder.Property(so => so.Id)
            .HasColumnName("Id")
            .ValueGeneratedNever();

        builder.Property(so => so.EServiceOrderStatus)
            .HasColumnName("ServiceOrderStatus")
            .HasColumnType("INT");
            
        builder.Property(so => so.EEnterprise)
            .HasColumnName("Enterprise")
            .HasColumnType("INT");
            
        builder.Property(so => so.ProductType)
            .HasColumnName("ProductType")
            .HasColumnType("INT");
            
        builder.Property(so => so.ERepair)
            .HasColumnName("RepairStatus")
            .HasColumnType("INT");
            
        builder.Property(so => so.EUnrepaired)
            .HasColumnName("UnrepairedStatus")
            .HasColumnType("INT");
            
        builder.Property(so => so.ProductBrand)
            .HasColumnName("ProductBrand")
            .HasMaxLength(50);
            
        builder.Property(so => so.ProductModel)
            .HasColumnName("ProductModel")
            .HasMaxLength(50);
            
        builder.Property(so => so.ProductSerialNumber)
            .HasColumnName("ProductSerialNumber")
            .HasMaxLength(50);
            
        builder.Property(so => so.ProductDefect)
            .HasColumnName("ProductDefect")
            .HasColumnType("text");
            
        builder.Property(so => so.Solution)
            .HasColumnName("Solution")
            .HasColumnType("text");
            
        builder.Property(so => so.Amount)
            .HasColumnName("Amount")
            .HasColumnType("DECIMAL(10,2)");
        
        builder.Property(so => so.EntryDate)
            .HasColumnName("EntryDate")
            .HasColumnType("datetime");
            
        builder.Property(so => so.InspectionDate)
            .HasColumnName("InspectionDate")
            .HasColumnType("datetime");
            
        builder.Property(so => so.RepairDate)
            .HasColumnName("RepairDate")
            .HasColumnType("datetime");
            
        builder.Property(so => so.DeliveryDate)
            .HasColumnName("DeliveryDate")
            .HasColumnType("datetime");
        
        builder.Property(so => so.PartCost)
            .HasColumnName("PartCost")
            .HasColumnType("DECIMAL(10,2)");
            
        builder.Property(so => so.LaborCost)
            .HasColumnName("LaborCost")
            .HasColumnType("DECIMAL(10,2)");
        
        builder.Property(so => so.CustomerId)
            .HasColumnName("CustomerId");
            
        builder.HasOne(so => so.Customer)
            .WithMany(c => c.ServiceOrders)
            .HasForeignKey(so => so.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Property(so => so.Location)
            .HasColumnName("Location")
            .HasMaxLength(50);
    }
}