using System;
using System.Data.Entity;
using System.IO;
using Core.Model;

namespace Core.Persistence
{
	public class Context : DbContext
	{
		public DbSet<Patent> Patents { get; set; }

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			var solutionDataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\data");

			if (!Directory.Exists(solutionDataDirectory))
			{
				Directory.CreateDirectory(solutionDataDirectory);
			}

			AppDomain.CurrentDomain.SetData("DataDirectory", solutionDataDirectory);
			
			Database.SetInitializer(new MigrateDatabaseToLatestVersion<Context, Configuration>());
		}
	}
}
