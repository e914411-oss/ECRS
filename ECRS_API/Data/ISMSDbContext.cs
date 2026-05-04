using System;
using System.Collections.Generic;
using ECRS_API.Models.ISMS;
using Microsoft.EntityFrameworkCore;

namespace ECRS_API.Data;

public partial class ISMSDbContext : DbContext
{
    public ISMSDbContext(DbContextOptions<ISMSDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ISMS_查詢登入資訊> ISMS_查詢登入資訊s { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ISMS_查詢登入資訊>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("ISMS_查詢登入資訊");

            entity.Property(e => e.CAType).HasMaxLength(20);
            entity.Property(e => e.Mail).HasMaxLength(100);
            entity.Property(e => e.Token).HasMaxLength(100);
            entity.Property(e => e.Uid).HasMaxLength(20);
            entity.Property(e => e.人員別).HasMaxLength(50);
            entity.Property(e => e.來源ip).HasMaxLength(30);
            entity.Property(e => e.區別名稱).HasMaxLength(100);
            entity.Property(e => e.名稱).HasMaxLength(50);
            entity.Property(e => e.單位oid).HasMaxLength(500);
            entity.Property(e => e.單位名稱).HasMaxLength(100);
            entity.Property(e => e.安全等級).HasMaxLength(100);
            entity.Property(e => e.帳號).HasMaxLength(60);
            entity.Property(e => e.手機).HasMaxLength(20);
            entity.Property(e => e.登入時間).HasColumnType("datetime");
            entity.Property(e => e.登出時間).HasColumnType("datetime");
            entity.Property(e => e.系統代碼).HasMaxLength(50);
            entity.Property(e => e.綁定憑證).HasMaxLength(20);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
