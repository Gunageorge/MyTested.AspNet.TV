﻿namespace Catstagram.Server.Data
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Infrastructure.Services;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using Models;
    using Models.Base;

    public class CatstagramDbContext : IdentityDbContext<User>
    {
        private readonly ICurrentUserService currentUser;

        public CatstagramDbContext(
            DbContextOptions<CatstagramDbContext> options, 
            ICurrentUserService currentUser)
            : base(options) 
            => this.currentUser = currentUser;

        public DbSet<Cat> Cats { get; set; }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            this.ApplyAuditInformation();

            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
        {
            this.ApplyAuditInformation();

            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder
                .Entity<Cat>()
                .HasQueryFilter(c => !c.IsDeleted)
                .HasOne(c => c.User)
                .WithMany(u => u.Cats)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(builder);
        }

        private void ApplyAuditInformation() 
            => this.ChangeTracker
                .Entries()
                .ToList()
                .ForEach(entry =>
                {
                    var userName = this.currentUser.GetUserName();

                    if (entry.Entity is IDeletableEntity deletableEntity)
                    {
                        if (entry.State == EntityState.Deleted)
                        {
                            deletableEntity.DeletedOn = DateTime.UtcNow;
                            deletableEntity.DeletedBy = userName;
                            deletableEntity.IsDeleted = true;

                            entry.State = EntityState.Modified;

                            return;
                        }
                    }
                    
                    if (entry.Entity is IEntity entity)
                    {
                        if (entry.State == EntityState.Added)
                        {
                            entity.CreatedOn = DateTime.UtcNow;
                            entity.CreatedBy = userName;
                        }
                        else if (entry.State == EntityState.Modified)
                        {
                            entity.ModifiedOn = DateTime.UtcNow;
                            entity.ModifiedBy = userName;
                        }
                    }
                });
    }
}