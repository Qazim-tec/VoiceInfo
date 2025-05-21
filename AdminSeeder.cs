using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using VoiceInfo.Models;

namespace VoiceInfo.Data
{
    public static class AdminSeeder
    {
        public static async Task SeedAdminAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

            // Check if an admin user already exists
            var adminUser = await userManager.FindByEmailAsync("admin@voiceinfo.com");
            if (adminUser == null)
            {
                // Create the admin user
                var admin = new User
                {
                    FirstName = "Admin",
                    LastName = "User",
                    Email = "admin@voiceinfo.com",
                    UserName = "admin@voiceinfo.com",
                    ProfilePicture = "", // Set a default value
                    IsEmailVerified = true // Ensure admin can log in without OTP
                };

                // Set a default password for the admin
                var result = await userManager.CreateAsync(admin, "Admin@1234");
                if (result.Succeeded)
                {
                    // Assign the Admin role to the user
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
                else
                {
                    // Log errors if the user creation fails
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"Error: {error.Description}");
                    }
                    throw new Exception("Failed to create admin user: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}