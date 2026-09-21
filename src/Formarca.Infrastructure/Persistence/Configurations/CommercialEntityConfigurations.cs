using Formarca.Domain.Brands;
using Formarca.Domain.Contracts;
using Formarca.Domain.Invoices;
using Formarca.Domain.Processes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcessEntity = Formarca.Domain.Processes.Process;

namespace Formarca.Infrastructure.Persistence.Configurations;

public sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("Brands");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ServicesDescription).HasMaxLength(4000).IsRequired();
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.Name });
        builder.Ignore(x => x.DomainEvents);

        builder.OwnsMany(x => x.InpiClasses, owned =>
        {
            owned.ToTable("BrandNiceClasses");
            owned.WithOwner().HasForeignKey("BrandId");
            owned.Property<Guid>("Id");
            owned.HasKey("Id");
            owned.Property(x => x.Number).IsRequired();
            owned.HasIndex("BrandId", "Number").IsUnique();
        });
        builder.Navigation(x => x.InpiClasses)
            .HasField("_inpiClasses")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("Contracts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CompanyDocument).HasMaxLength(32).IsRequired();
        builder.Property(x => x.RepresentativeDocument).HasMaxLength(32).IsRequired();
        builder.Property(x => x.BrandName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Classes).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.TemplateKey).HasMaxLength(64).IsRequired();
        builder.Property(x => x.BodyMarkdown).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Street).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Number).HasMaxLength(20).IsRequired();
        builder.Property(x => x.City).HasMaxLength(100).IsRequired();
        builder.Property(x => x.State).HasMaxLength(2).IsRequired();
        builder.Property(x => x.ZipCode).HasMaxLength(8).IsRequired();
        builder.Property(x => x.MinimumTerm).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ClientSignaturePng).HasColumnType("varbinary(max)");
        builder.Property(x => x.AutentiqueDocumentId).HasMaxLength(128);
        builder.Property(x => x.AutentiqueSignerPublicId).HasMaxLength(64);
        builder.Property(x => x.AutentiqueSignUrl).HasMaxLength(2048);
        builder.Property(x => x.AutentiqueStatus).HasMaxLength(32);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.BrandId);
        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.AutentiqueDocumentId)
            .IsUnique()
            .HasFilter("[AutentiqueDocumentId] IS NOT NULL AND [IsDeleted] = 0");
        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.CanDownload);
        builder.Ignore(x => x.CanPreview);
        builder.Ignore(x => x.HasCompleteIssueFields);
        builder.Ignore(x => x.PersistedFileKind);
    }
}

public sealed class ContractFileConfiguration : IEntityTypeConfiguration<ContractFile>
{
    public void Configure(EntityTypeBuilder<ContractFile> builder)
    {
        builder.ToTable("ContractFiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Content).HasColumnType("varbinary(max)").IsRequired();
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.ContractId);
        builder.HasIndex(x => new { x.ContractId, x.Kind })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class InvoiceSettingsConfiguration : IEntityTypeConfiguration<InvoiceSettings>
{
    public void Configure(EntityTypeBuilder<InvoiceSettings> builder)
    {
        builder.ToTable("InvoiceSettings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.InterestRatesJson).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.PixDiscount).HasPrecision(9, 4);
        builder.Property(x => x.GeneralDiscount).HasPrecision(9, 4);
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ContractAmount).HasPrecision(18, 2);
        builder.Property(x => x.Discount).HasPrecision(9, 4);
        builder.Property(x => x.ExpectedAmount).HasPrecision(18, 2);
        builder.Property(x => x.ReceivedAmount).HasPrecision(18, 2);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.ContractId)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class ProcessConfiguration : IEntityTypeConfiguration<ProcessEntity>
{
    public void Configure(EntityTypeBuilder<ProcessEntity> builder)
    {
        builder.ToTable("Processes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProcessNumber).HasMaxLength(64);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.BrandId);
        builder.HasIndex(x => x.TrademarkStatusId);
        builder.HasIndex(x => x.ContractId)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.HasOne<TrademarkStatus>()
            .WithMany()
            .HasForeignKey(x => x.TrademarkStatusId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class TrademarkStatusConfiguration : IEntityTypeConfiguration<TrademarkStatus>
{
    public void Configure(EntityTypeBuilder<TrademarkStatus> builder)
    {
        builder.ToTable("TrademarkStatuses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(TrademarkStatus.CodeMaxLength).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(TrademarkStatus.TitleMaxLength).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(TrademarkStatus.DescriptionMaxLength).IsRequired();
        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.SortOrder);
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class ProcessStatusChangeConfiguration : IEntityTypeConfiguration<ProcessStatusChange>
{
    public void Configure(EntityTypeBuilder<ProcessStatusChange> builder)
    {
        builder.ToTable("ProcessStatusChanges");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Note).HasMaxLength(ProcessStatusChange.NoteMaxLength);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.ProcessId);
        builder.HasIndex(x => x.ToStatusId);
        builder.HasOne<ProcessEntity>()
            .WithMany()
            .HasForeignKey(x => x.ProcessId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TrademarkStatus>()
            .WithMany()
            .HasForeignKey(x => x.ToStatusId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TrademarkStatus>()
            .WithMany()
            .HasForeignKey(x => x.FromStatusId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(x => x.DomainEvents);
    }
}
