﻿using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Grupo7_Pizarra_SignalR_Data.Entidades
{
    public partial class PizarraContext : DbContext
    {
        public PizarraContext()
        {
        }

        public PizarraContext(DbContextOptions<PizarraContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Dibujo> Dibujos { get; set; } = null!;
        public virtual DbSet<Sala> Salas { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=JOACO\\SQLEXPRESS;Database=Pizarra;Trusted_Connection=True;Encrypt=False");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Dibujo>(entity =>
            {
                entity.HasKey(e => e.IdDibujo);

                entity.ToTable("Dibujo");

                entity.Property(e => e.Dibujo1).HasColumnName("Dibujo");

                entity.HasOne(d => d.IdSalaNavigation)
                    .WithMany(p => p.Dibujos)
                    .HasForeignKey(d => d.IdSala)
                    .HasConstraintName("FK_Dibujo_Salas");
            });

            modelBuilder.Entity<Sala>(entity =>
            {
                entity.HasKey(e => e.IdSala);

                entity.Property(e => e.NombreSala).HasMaxLength(50);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
