using System.Data.Entity;
using Authenticator.Data.Models;

namespace Authenticator.Data
{
	internal class AuthenticatorContext : DbContext
	{
		public AuthenticatorContext() : base("A")
		{
			Database.SetInitializer(new CreateDatabaseIfNotExists<AuthenticatorContext>());
#if DEBUG
			this.Database.Log = s => Debug.WriteLine(s);
			#endif
		}

		public DbSet<Account> Accounts { get; set; }
	}
}