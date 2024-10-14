using CAAMarketing.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CAAMarketing.Data
{
    public static class ApplicationDbInitializer
    {
        public static async void Seed(IApplicationBuilder applicationBuilder)
        {
            ApplicationDbContext context = applicationBuilder.ApplicationServices.CreateScope()
                .ServiceProvider.GetRequiredService<ApplicationDbContext>();
            try
            {
                ////Delete the database if you need to apply a new Migration
                //context.Database.EnsureDeleted();
                //Create the database if it does not exist and apply the Migration
                context.Database.Migrate();

                // Create Roles
                var RoleManager = applicationBuilder.ApplicationServices.CreateScope()
                    .ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                string[] roleNames = { "Admin", "Supervisor", "Employee" };
                IdentityResult roleResult;
                foreach (var roleName in roleNames)
                {
                    var roleExist = await RoleManager.RoleExistsAsync(roleName);
                    if (!roleExist)
                    {
                        roleResult = await RoleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }
                // Create Users
                var userManager = applicationBuilder.ApplicationServices.CreateScope()
                    .ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

                // Create an instance of ApplicationDbContext for adding InitialPassword entries
                var dbContext = applicationBuilder.ApplicationServices.CreateScope()
                    .ServiceProvider.GetRequiredService<ApplicationDbContext>();

                if (userManager.FindByEmailAsync("mhachem12@outlook.com").Result == null)
                {
                    IdentityUser user = new IdentityUser
                    {
                        UserName = "mhachem12@outlook.com",
                        Email = "mhachem12@outlook.com"
                    };

                    string initialPassword = "Admins@123";
                    IdentityResult result = userManager.CreateAsync(user, initialPassword).Result;

                    if (result.Succeeded)
                    {
                        userManager.AddToRoleAsync(user, "Admin").Wait();

                        // Add the initial password to the InitialPasswords table
                        dbContext.InitialPasswords.Add(new InitialPassword { UserId = user.Id, Password = initialPassword });
                        await dbContext.SaveChangesAsync();
                    }
                }
                if (userManager.FindByEmailAsync("moayedcaaproject@outlook.com").Result == null)
                {
                    IdentityUser user = new IdentityUser
                    {
                        UserName = "moayedcaaproject@outlook.com",
                        Email = "moayedcaaproject@outlook.com"
                    };

                    string initialPassword = "Admins@1234";
                    IdentityResult result = userManager.CreateAsync(user, initialPassword).Result;

                    if (result.Succeeded)
                    {
                        userManager.AddToRoleAsync(user, "Admin").Wait();

                        // Add the initial password to the InitialPasswords table
                        dbContext.InitialPasswords.Add(new InitialPassword { UserId = user.Id, Password = initialPassword });
                        await dbContext.SaveChangesAsync();
                    }
                }
                if (userManager.FindByEmailAsync("super@caaniagara.ca").Result == null)
                {
                    IdentityUser user = new IdentityUser
                    {
                        UserName = "super@caaniagara.ca",
                        Email = "super@caaniagara.ca"
                    };

                    string initialPassword = "Supers@123";
                    IdentityResult result = userManager.CreateAsync(user, initialPassword).Result;
                    if (result.Succeeded)
                    {
                        userManager.AddToRoleAsync(user, "Supervisor").Wait();

                        // Add the initial password to the InitialPasswords table
                        dbContext.InitialPasswords.Add(new InitialPassword { UserId = user.Id, Password = initialPassword });
                        await dbContext.SaveChangesAsync();
                    }
                }
                if (userManager.FindByEmailAsync("employee@caaniagara.ca").Result == null)
                {
                    IdentityUser user = new IdentityUser
                    {
                        UserName = "employee@caaniagara.ca",
                        Email = "employee@caaniagara.ca"
                    };

                    string initialPassword = "Emp@123";
                    IdentityResult result = userManager.CreateAsync(user, initialPassword).Result;
                    if (result.Succeeded)
                    {
                        userManager.AddToRoleAsync(user, "Employee").Wait();

                        // Add the initial password to the InitialPasswords table
                        dbContext.InitialPasswords.Add(new InitialPassword { UserId = user.Id, Password = initialPassword });
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.GetBaseException().Message);
            }
        }
    }

}
