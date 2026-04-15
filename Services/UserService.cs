using Microsoft.EntityFrameworkCore;
using Worktrack.Data;
using Worktrack.Models;
using System.Text.RegularExpressions;

namespace Worktrack.Services
{
    public class UserService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly SecretCodeMailer _mailer;
        private static readonly HashSet<string> GenericMailDomains =
        [
            "gmail",
            "gmx",
            "web",
            "hotmail",
            "outlook",
            "icloud",
            "yahoo",
            "live",
            "aol",
            "mail"
        ];

        public UserService(IDbContextFactory<AppDbContext> factory, SecretCodeMailer mailer)
        {
            _factory = factory;
            _mailer = mailer;
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

        public async Task<(bool ok, string? error, string? newCode)> RegenerateSecretCodeAsync(int userId, CancellationToken ct = default)
        {
            if (userId <= 0)
                return (false, "Benutzer nicht gefunden.", null);

            await using var db = await _factory.CreateDbContextAsync(ct);
            var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
            if (user is null)
                return (false, "Benutzer nicht gefunden.", null);

            user.SecretCode = await GenerateUniqueCodeAsync();
            await db.SaveChangesAsync(ct);

            return (true, null, user.SecretCode);
        }

        public async Task<CodeRequestResult> RequestSecretCodeAsync(string enteredName, string email, CancellationToken ct = default)
        {
            var normalizedName = NormalizeLoose(enteredName);
            var normalizedEmail = NormalizeEmail(email);

            if (string.IsNullOrWhiteSpace(normalizedName) || string.IsNullOrWhiteSpace(normalizedEmail))
            {
                return CodeRequestResult.Invalid("Bitte Namen und E-Mail vollständig eingeben.");
            }

            if (!IsValidEmail(email))
            {
                return CodeRequestResult.Invalid("Die E-Mail-Adresse sieht nicht gültig aus.");
            }

            await using var db = await _factory.CreateDbContextAsync();
            var users = await db.Users.OrderBy(u => u.Name).ToListAsync(ct);

            var emailMatches = users
                .Where(u => NormalizeEmail(u.Email) == normalizedEmail)
                .ToList();

            if (emailMatches.Count > 1)
            {
                return CodeRequestResult.ManualReview("Diese E-Mail-Adresse ist mehrfach vergeben. Bitte an den Administrator wenden.");
            }

            if (emailMatches.Count == 1)
            {
                var existingUser = emailMatches[0];
                await _mailer.SendSecretCodeAsync(existingUser, ct);
                return CodeRequestResult.Success(
                    existingUser,
                    "Der Secret Code wurde an die bereits hinterlegte E-Mail-Adresse gesendet.",
                    false);
            }

            var candidates = users
                .Select(u => new
                {
                    User = u,
                    NameScore = GetNameSimilarityScore(enteredName, u.Name),
                    EmailScore = GetEmailNameScore(email, u.Name)
                })
                .Where(x => x.NameScore >= 75 && x.EmailScore >= 40)
                .OrderByDescending(x => x.NameScore + x.EmailScore)
                .ToList();

            if (candidates.Count == 0)
            {
                return CodeRequestResult.ManualReview("Name und E-Mail passen nicht plausibel zusammen. Bitte an den Administrator wenden.");
            }

            var best = candidates[0];
            var second = candidates.Skip(1).FirstOrDefault();
            if (second is not null && (best.NameScore + best.EmailScore) - (second.NameScore + second.EmailScore) < 15)
            {
                return CodeRequestResult.ManualReview("Die Zuordnung ist nicht eindeutig. Bitte an den Administrator wenden.");
            }

            if (!string.IsNullOrWhiteSpace(best.User.Email))
            {
                return CodeRequestResult.ManualReview("Für diesen Account ist bereits eine andere E-Mail-Adresse hinterlegt. Bitte an den Administrator wenden.");
            }

            best.User.Email = email.Trim();
            await db.SaveChangesAsync(ct);
            await _mailer.SendSecretCodeAsync(best.User, ct);

            return CodeRequestResult.Success(
                best.User,
                $"Die E-Mail-Adresse wurde beim Account von {best.User.Name} hinterlegt und der Secret Code wurde versendet.",
                true);
        }

        public List<User> SortUser(List<User> Users, string query, int Elements = 5) {
            return Elements == 0 ? [.. Users.OrderByDescending(u => GetMatchScore(query, u.Name))] :
            [.. Users.OrderByDescending(u => GetMatchScore(query, u.Name)).Take(Elements)];
        }

        public async Task<List<User>> GetBestUsers(string query, int Elements = 5, CancellationToken ct=default)
        {
            var Users = await GetAllAsync(ct);

            return (Elements == 0) ? Users
            .OrderByDescending(u => GetMatchScore(query, u.Name))
            .ToList()
            :Users.OrderByDescending(u => GetMatchScore(query, u.Name))
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

    private static bool IsValidEmail(string email)
        => System.Net.Mail.MailAddress.TryCreate(email?.Trim(), out _);

    private static string NormalizeEmail(string? email)
        => string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant();

    private static string NormalizeLoose(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var lettersOnly = Regex.Replace(value.Trim().ToLowerInvariant(), @"[^a-z0-9]+", " ");
        return Regex.Replace(lettersOnly, @"\s+", " ").Trim();
    }

    private static List<string> Tokenize(string? value)
        => NormalizeLoose(value)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    private static int GetNameSimilarityScore(string enteredName, string accountName)
    {
        var enteredTokens = Tokenize(enteredName);
        var accountTokens = Tokenize(accountName);

        if (enteredTokens.Count == 0 || accountTokens.Count == 0) return 0;

        var exactMatches = 0;
        var prefixMatches = 0;

        foreach (var entered in enteredTokens)
        {
            foreach (var account in accountTokens)
            {
                if (entered == account)
                {
                    exactMatches++;
                    break;
                }

                if (entered.Length >= 3 && account.StartsWith(entered))
                {
                    prefixMatches++;
                    break;
                }

                if (account.Length >= 3 && entered.StartsWith(account))
                {
                    prefixMatches++;
                    break;
                }
            }
        }

        var fullDistance = LevenshteinStatic(NormalizeLoose(enteredName), NormalizeLoose(accountName));
        var maxLen = Math.Max(NormalizeLoose(enteredName).Length, NormalizeLoose(accountName).Length);
        var distanceScore = maxLen == 0 ? 0 : Math.Max(0, 40 - (fullDistance * 40 / maxLen));

        return (exactMatches * 30) + (prefixMatches * 18) + distanceScore;
    }

    private static int GetEmailNameScore(string email, string name)
    {
        var address = NormalizeEmail(email);
        var atIndex = address.IndexOf('@');
        if (atIndex <= 0) return 0;

        var localPart = address[..atIndex];
        var domain = address[(atIndex + 1)..];
        var domainMain = domain.Split('.')[0];

        var emailTokens = Tokenize(localPart);
        var nameTokens = Tokenize(name);
        if (nameTokens.Count == 0) return 0;

        var score = 0;

        foreach (var nameToken in nameTokens)
        {
            foreach (var emailToken in emailTokens)
            {
                if (emailToken == nameToken)
                {
                    score += 40;
                    break;
                }

                if (emailToken.Length >= 3 && nameToken.StartsWith(emailToken))
                {
                    score += 28;
                    break;
                }

                if (emailToken.Length == 1 && nameToken.StartsWith(emailToken))
                {
                    score += 14;
                    break;
                }
            }
        }

        if (!GenericMailDomains.Contains(domainMain))
        {
            foreach (var nameToken in nameTokens)
            {
                if (domainMain == nameToken)
                {
                    score += 25;
                    break;
                }

                if (domainMain.Length >= 4 && nameToken.StartsWith(domainMain))
                {
                    score += 18;
                    break;
                }
            }
        }

        return score;
    }

    private static int LevenshteinStatic(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b.Length;
        if (string.IsNullOrEmpty(b)) return a.Length;

        var d = new int[a.Length + 1, b.Length + 1];

        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) d[0, j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[a.Length, b.Length];
    }
    }
}

public class CodeRequestResult
{
    public bool IsSuccess { get; init; }
    public bool AddedEmailToAccount { get; init; }
    public bool RequiresManualReview { get; init; }
    public string Message { get; init; } = string.Empty;
    public User? User { get; init; }

    public static CodeRequestResult Success(User user, string message, bool addedEmailToAccount)
        => new()
        {
            IsSuccess = true,
            Message = message,
            User = user,
            AddedEmailToAccount = addedEmailToAccount
        };

    public static CodeRequestResult ManualReview(string message)
        => new()
        {
            RequiresManualReview = true,
            Message = message
        };

    public static CodeRequestResult Invalid(string message)
        => new()
        {
            Message = message
        };
}
