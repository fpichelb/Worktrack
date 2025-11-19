using Microsoft.EntityFrameworkCore;
using Worktrack.Data;
using Worktrack.Models;

namespace Worktrack.Services
{
    public class UserService
    {
        private readonly AppDbContext _db;

        public UserService(AppDbContext db)
        {
            _db = db;
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
            while (await _db.Users.AnyAsync(u => u.SecretCode == code));

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

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return user;
        }

        // ---------------------------------------------------------
        // Make User Admin
        // ---------------------------------------------------------
        public async Task<bool> SetAdminAsync(int id, bool admin)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return false;

            user.Role = admin ? "admin" : "user";
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _db.Users.OrderBy(u => u.Name).ToListAsync();
        }
    }
}
