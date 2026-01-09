using Microsoft.EntityFrameworkCore;
using Worktrack.Data;
using Worktrack.Models;

namespace Worktrack.Services
{
    public class UserService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

    public UserService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

        // ---------------------------------------------------------
        // Generate unique 8-char SecretCode
        // ---------------------------------------------------------
        public async Task<string> GenerateUniqueCodeAsync()
        {
            await using var Db = await _factory.CreateDbContextAsync();
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
            await using var Db = await _factory.CreateDbContextAsync();
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
            await using var Db = await _factory.CreateDbContextAsync();
            var user = await Db.Users.FindAsync(userid);
            if (user == null) return false;

            user.Role = admin ? "admin" : "user";
            await Db.SaveChangesAsync();
            return true;
        }
        public async Task<User?> GetUserById(int Id)
        {
            await using var Db = await _factory.CreateDbContextAsync();
            return await Db.Users.FirstOrDefaultAsync(u => u.Id == Id);
        }
        public async Task<User?> ValidateSecretCodeAsync(string secretCode)
        {
            await using var Db = await _factory.CreateDbContextAsync();
            return await Db.Users.FirstOrDefaultAsync(u =>
            u.SecretCode.Trim() == secretCode.Trim().ToUpper());
        }
        public async Task<User?> GetUserByStringId(String Id)
        {
            await using var Db = await _factory.CreateDbContextAsync();
            return await Db.Users.FirstOrDefaultAsync(u => u.Id.ToString() == Id);
        }

        public async Task<List<User>> GetAllAsync()
        {
            await using var Db = await _factory.CreateDbContextAsync();
            return await Db.Users.OrderBy(u => u.Name).ToListAsync();
        }
        public async Task<User?> UpdateUser(User user)
        {
            await using var Db = await _factory.CreateDbContextAsync();
            Db.Users.Update(user);
            await Db.SaveChangesAsync();
            return user;
        }
    }
}
