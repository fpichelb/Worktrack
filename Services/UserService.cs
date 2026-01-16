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

        public async Task<List<User>> GetAllAsync(CancellationToken ct =default)
        {
            await using var Db = await _factory.CreateDbContextAsync();
            return await Db.Users.OrderBy(u => u.Name).ToListAsync(ct);
        }
        public async Task<User?> UpdateUser(User user)
        {
            await using var Db = await _factory.CreateDbContextAsync();
            Db.Users.Update(user);
            await Db.SaveChangesAsync();
            return user;
        }

        public List<User> SortUser(List<User> Users, string query, int Elements = 5) {
            return Users
            .OrderByDescending(u => GetMatchScore(query, u.Name))
            .Take(Elements)
            .ToList();
        }

        public async Task<List<User>> GetBestUsers(string query, int Elements = 5, CancellationToken ct=default)
        {
            var Users = await GetAllAsync(ct);
            return Users
            .OrderByDescending(u => GetMatchScore(query, u.Name))
            .Take(Elements)
            .ToList();
        }
        private int Levenshtein(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return b.Length;
            if (string.IsNullOrEmpty(b)) return a.Length;

            int[,] d = new int[a.Length + 1, b.Length + 1];

            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost
                    );
                }
            }

            return d[a.Length, b.Length];
        }

    private double GetMatchScore(string entered, string fullName)
    {
        entered = entered.ToLower();
        fullName = fullName.ToLower();

        double score = 0;

        // 1. String-Distance
        var dist = Levenshtein(entered, fullName);
        score -= dist; // niedrig besser → negativ nutzen

        // 2. Teilstring-Bonus
        if (fullName.Contains(entered)) score += 10;

        // 3. Token-Vergleich
        var tokens = fullName.Split(' ');
        var enteredTokens = entered.Split(' ');

        foreach (var t in enteredTokens)
        {
            foreach (var ft in tokens)
            {
                if (ft.StartsWith(t)) score += 5;
                if (ft == t) score += 10;
            }
        }

        return score;
    }
    }
}
