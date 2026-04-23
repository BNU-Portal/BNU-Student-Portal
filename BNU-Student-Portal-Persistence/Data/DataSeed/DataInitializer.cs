using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace BNU_Student_Portal_Persistence.Data.DataSeed
{
    public class DataInitializer(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<DataInitializer> logger) : IDataInitializer
    {
        public async Task Initialize()
        {
            // Implementation for data initialization

            string[] roles = ["SuperAdmin", "Admin", "Student", "Professor", "TeachingAssistant"];



            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            try
            {
                if (!userManager.Users.Any())
                {
                    var Saif = new AppUser
                    {
                        Email = "saif@gmail.com",
                        UserName = "Saif",
                        DateOfBirth = new DateOnly(year: 2004, month: 5, day: 26),
                        NationalId = "30405260101654",
                        Name = "Saif Eldeen Ahmed Lotfy",
                        Nationality = "Egyptian",
                        Gender = Gender.Male
                    };
                    var Bassel = new AppUser
                    {
                        Email = "bassel@gmail.com",
                        UserName = "bassel",
                        DateOfBirth = new DateOnly(year: 2004, month: 3, day: 15),
                        NationalId = "Test",
                        Name = "Bassel Alaa Nour",
                        Nationality = "Egyptian",
                        Gender = Gender.Male

                    };

                    await userManager.CreateAsync(Saif, "Pa$$w0rd");
                    await userManager.CreateAsync(Bassel, "Pa$$w0rd");

                    await userManager.AddToRoleAsync(Saif, "SuperAdmin");
                    await userManager.AddToRoleAsync(Bassel, "SuperAdmin");

                }


            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"An error occurred while seeding identity data. \n Message : {ex.Message}");
            }




        }
    }
}
