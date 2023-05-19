using CrucibleBlogMVC.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CrucibleBlogMVC.Data
{
    public static class DataUtility
    {

        private const string? _adminRole = "Admin";
        private const string? _moderatorRole = "Moderator";
        public static string GetConnectionString(IConfiguration configuration)
        {
            string? connectionString = configuration.GetConnectionString("DefaultConnection");
            string? databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

            return string.IsNullOrEmpty(databaseUrl) ? connectionString! : BuildConnectionString(databaseUrl);

        }
        private static string BuildConnectionString(string databaseUrl)
        {
            var databaseUri = new Uri(databaseUrl);
            var userInfo = databaseUri.UserInfo.Split(':');
            var builder = new NpgsqlConnectionStringBuilder()
            {
                Host = databaseUri.Host,
                Port = databaseUri.Port,
                Username = userInfo[0],
                Password = userInfo[1],
                Database = databaseUri.LocalPath.TrimStart('/'),
                SslMode = SslMode.Require,
                TrustServerCertificate = true
            };
            return builder.ToString();
        }

        public static async Task ManageDataAsync(IServiceProvider svcProvider)
        {
            // Obtaining the necessary services based on the IServiceProvider parameter
            var dbContextSvc = svcProvider.GetRequiredService<ApplicationDbContext>();
            var userManagerSvc = svcProvider.GetRequiredService<UserManager<BlogUser>>();
            var roleManagerSvc = svcProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var configurationSvc = svcProvider.GetRequiredService<IConfiguration>();

            // Align the database by checking the Migrations
            await dbContextSvc.Database.MigrateAsync();

            // Seed Application Roles
            await SeedRolesAsync(roleManagerSvc);

            // Seed Demo User(s)
            await SeedBlogUsersAsync(userManagerSvc, configurationSvc);
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            if (!await roleManager.RoleExistsAsync(_adminRole!))
            {
                await roleManager.CreateAsync(new IdentityRole(_adminRole!));
            }

            if (!await roleManager.RoleExistsAsync(_moderatorRole!))
            {
                await roleManager.CreateAsync(new IdentityRole(_moderatorRole!));
            }
        }

        // Demo Users Seed Method
        private static async Task SeedBlogUsersAsync(UserManager<BlogUser> userManagerSvc, IConfiguration configuration)
        {
            string? adminEmail = configuration["AdminLoginEmail"] ?? Environment.GetEnvironmentVariable("AdminLoginEmail");
            string? adminPassword = configuration["AdminLoginPassword"] ?? Environment.GetEnvironmentVariable("AdminLoginPassword");
            
            string? moderatorEmail = configuration["ModeratorLoginEmail"] ?? Environment.GetEnvironmentVariable("ModeratorLoginEmail");
            string? moderatorPassword = configuration["ModeratorLoginPassword"] ?? Environment.GetEnvironmentVariable("ModeratorLoginPassword");
            
            try
            {
                // Seed Admin
                BlogUser? adminUser = new BlogUser()
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Jamal",
                    LastName = "Gibson",
                    EmailConfirmed = true,
                };


                BlogUser? adUser = await userManagerSvc.FindByEmailAsync(adminUser.Email!);

                if (adUser == null)
                {
                    await userManagerSvc.CreateAsync(adminUser, adminPassword!);
                    await userManagerSvc.AddToRoleAsync(adminUser, _adminRole!);
                }
                

                // Seed Moderator
                BlogUser? moderatorUser = new BlogUser()
                {
                    UserName = moderatorEmail,
                    Email = moderatorEmail,
                    FirstName = "Jamal",
                    LastName = "Abdi",
                    EmailConfirmed = true,
                };


                BlogUser? modUser = await userManagerSvc.FindByEmailAsync(moderatorUser.Email!);

                if (modUser == null)
                {
                    await userManagerSvc.CreateAsync(moderatorUser, moderatorPassword!);
                    await userManagerSvc.AddToRoleAsync(moderatorUser, _moderatorRole!);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("*************  ERROR  *************");
                Console.WriteLine("Error Seeding Demo Login User.");
                Console.WriteLine(ex.Message);
                Console.WriteLine("***********************************");
                throw;
            }
        }
    }
}
