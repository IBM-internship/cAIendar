using AiCalendarAssistant.Data.Constants;
using AiCalendarAssistant.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace AiCalendarAssistant.Data.Seeding
{
    public class UserSeeder
    {
        public static IEnumerable<ApplicationUser> SeedUsers()
        {
            List<ApplicationUser> users = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    Id = UserConstants.Id,
                    UserName = UserConstants.UserName,
                    NormalizedUserName = UserConstants.UserName.ToUpper(),
                    Email = UserConstants.Email,
                    NormalizedEmail = UserConstants.Email.ToUpper(),
                    ConcurrencyStamp = UserConstants.ConcurrencyStamp,
                }
            };

            var hasher = new PasswordHasher<ApplicationUser>();

            foreach (var user in users)
            {
                user.PasswordHash = hasher.HashPassword(user, UserConstants.Password);
            }

            return users;
        }
    }
}
