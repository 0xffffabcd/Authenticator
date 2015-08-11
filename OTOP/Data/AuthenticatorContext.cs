using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using Authenticator.Data.Models;

namespace Authenticator.Data
{
    internal class AuthenticatorContext : DbContext
    {
        public AuthenticatorContext() : base("localSqlite")
        {
            Database.SetInitializer(new CreateDatabaseIfNotExists<AuthenticatorContext>());
            #if DEBUG
            this.Database.Log = s => Debug.WriteLine(s);
            #endif
        }

        public DbSet<Account> Accounts { get; set; }

        
    }
}