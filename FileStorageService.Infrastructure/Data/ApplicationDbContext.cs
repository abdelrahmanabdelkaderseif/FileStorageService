using System;
using Microsoft.EntityFrameworkCore;
using FileStorageService.Core.Models;
using System.Text.Json;

namespace FileStorageService.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<FileMetadata> Files { get; set; }
        public DbSet<FilePermission> FilePermissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
                entity.Property(e => e.FullName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
                
                // Convert string array to JSON for storage
                entity.Property(e => e.Roles)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, _jsonOptions),
                        v => JsonSerializer.Deserialize<string[]>(v, _jsonOptions));

                // Unique index on email
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // FileMetadata configuration
            modelBuilder.Entity<FileMetadata>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileName).HasMaxLength(256).IsRequired();
                entity.Property(e => e.ContentType).HasMaxLength(100);
                entity.Property(e => e.Path).HasMaxLength(1000).IsRequired();
                entity.Property(e => e.UploadedBy).HasMaxLength(256).IsRequired();

                // Store Dictionary as JSON
                entity.Property(e => e.CustomMetadata)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, _jsonOptions),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, _jsonOptions));
            });

            // FilePermission configuration
            modelBuilder.Entity<FilePermission>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Relationships
                entity.HasOne(e => e.User)
                    .WithMany(u => u.FilePermissions)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.File)
                    .WithMany(f => f.Permissions)
                    .HasForeignKey(e => e.FileId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Unique constraint on UserId and FileId combination
                entity.HasIndex(e => new { e.UserId, e.FileId }).IsUnique();
            });

            // Seed admin user
            var adminId = Guid.NewGuid();
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = adminId,
                FullName = "System Administrator",
                Email = "admin@system.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Roles = new[] { UserRoles.Admin }
            });
        }
    }
} 