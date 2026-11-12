using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BonusTest.Domain.Entities;

namespace BonusTest.Infrastructure.Persistence.Configurations;

public class BonusConfiguration : IEntityTypeConfiguration<Bonus>
{
    public void Configure(EntityTypeBuilder<Bonus> builder)
    {
        builder.ToTable("Bonus");
        builder.HasKey(x => x.BonusId);
        builder.Property(x => x.BonusId).UseIdentityColumn();
        builder.Property(x => x.DepartmentCode).HasColumnType("char(5)").IsRequired();
        builder.Property(x => x.EmployeeNumber).IsRequired();
        builder.Property(x => x.Period).HasColumnType("date").IsRequired();
        builder.Property(x => x.FixedAmount).HasPrecision(16, 2);
        builder.Property(x => x.SalaryPercentage).HasPrecision(7, 4);
        builder.Property(x => x.ReasonCode).HasColumnType("char(6)").IsRequired();
        builder.Property(x => x.GrossAmount).HasPrecision(16, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(16, 2);
        builder.Property(x => x.NetAmount).HasPrecision(16, 2);
        builder.Property(x => x.BonusStateCode).HasColumnType("char(4)").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");
        builder.Property(x => x.ModifiedBy).HasMaxLength(100).IsUnicode(false);
        builder.Property(x => x.ModifiedAt).HasColumnType("datetime2(3)");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => new { x.DepartmentCode, x.EmployeeNumber })
            .HasPrincipalKey(x => new { x.DepartmentCode, x.EmployeeNumber })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.BonusType)
            .WithMany()
            .HasForeignKey(x => x.BonusTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.BonusReason)
            .WithMany()
            .HasForeignKey(x => x.ReasonCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.BonusState)
            .WithMany()
            .HasForeignKey(x => x.BonusStateCode)
            .HasPrincipalKey(x => x.StateCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
