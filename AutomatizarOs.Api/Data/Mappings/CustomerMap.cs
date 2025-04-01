using AutomatizarOs.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutomatizarOs.Api.Data.Mappings;

public class CustomerMap : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.Id)
            .HasColumnName("Id")
            .ValueGeneratedNever();
        
        builder.Property(c => c.Name)
            .HasColumnName("Name")
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(c => c.Street)
            .HasColumnName("Street")
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(c => c.Neighborhood)
            .HasColumnName("Neighborhood")
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(c => c.City)
            .HasColumnName("City")
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(c => c.Number)
            .HasColumnName("Number")
            .IsRequired()
            .HasMaxLength(20);
            
        builder.Property(c => c.ZipCode)
            .HasColumnName("ZipCode")
            .IsRequired()
            .HasMaxLength(10);
            
        builder.Property(c => c.StateCode)
            .HasColumnName("StateCode")
            .IsRequired()
            .HasMaxLength(2);
            
        builder.Property(c => c.Landline)
            .HasColumnName("Landline")
            .HasMaxLength(20);
            
        builder.Property(c => c.Phone)
            .HasColumnName("Phone")
            .IsRequired()
            .HasMaxLength(20);
            
        builder.Property(c => c.Email)
            .HasColumnName("Email")
            .IsRequired()
            .HasMaxLength(100);
    }
}