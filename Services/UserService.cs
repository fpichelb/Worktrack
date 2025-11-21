using Microsoft.EntityFrameworkCore;
using Worktrack.Data;
using Worktrack.Models;

namespace Worktrack.Services
{
    public class UserService
    {
        private readonly AppDbContext Db;

        public UserService(AppDbContext db)
        {
            Db = db;
        }

        // ---------------------------------------------------------
        // Generate unique 8-char SecretCode
        // ---------------------------------------------------------
        public async Task<string> GenerateUniqueCodeAsync()
        {
            string code;
            do
            {
                code = Guid.NewGuid().ToString("N")[..6].ToUpper();
            }
            while (await Db.Users.AnyAsync(u => u.SecretCode == code));

            return code;
        }

        // ---------------------------------------------------------
        // Add Single User
        // ---------------------------------------------------------
        public async Task<User> AddSingleUserAsync(string name, string? email, bool isAdmin)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new Exception("Name darf nicht leer sein.");

            var code = await GenerateUniqueCodeAsync();

            var user = new User
            {
                Name = name.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim(),
                SecretCode = code,
                Role = isAdmin ? "admin" : "user",
                ShareStats = false
            };

            Db.Users.Add(user);
            await Db.SaveChangesAsync();

            return user;
        }

        // ---------------------------------------------------------
        // Make User Admin
        // ---------------------------------------------------------
        public async Task<bool> SetAdminAsync(int userid, bool admin)
        {
            var user = await Db.Users.FindAsync(userid);
            if (user == null) return false;

            user.Role = admin ? "admin" : "user";
            await Db.SaveChangesAsync();
            return true;
        }
        public async Task<List<User>> GetAllAsync()
        {
            return await Db.Users.OrderBy(u => u.Name).ToListAsync();
        }
        
    }
}
