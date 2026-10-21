using System.Text;
using Microsoft.AspNetCore.Identity;
using RA_MuniAnnouncements.Api.DataAccess;
using RA_MuniAnnouncements.Api.Models;

namespace RA_MuniAnnouncements.Api.DevTools;

public static class UserProvisioner
{
    public static async Task CreateAsync(IServiceProvider services, string[] args)
    {
        if (args.Length != 3 || string.IsNullOrWhiteSpace(args[1]) || args[1].Length > 100 ||
            args[2].Length != 9 || !args[2].All(char.IsAsciiDigit))
            throw new ArgumentException("Usage: --create-user <username> <9-digit idNumber>. Password is read privately from the console.");
        Console.Write("Password (at least 12 characters): ");
        var password = new StringBuilder();
        if (Console.IsInputRedirected) password.Append(Console.ReadLine());
        else
        {
            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Enter) break;
                if (key.Key == ConsoleKey.Backspace && password.Length > 0) password.Length--;
                else if (!char.IsControl(key.KeyChar)) password.Append(key.KeyChar);
            }
            Console.WriteLine();
        }
        if (password.Length is < 12 or > 1024) throw new ArgumentException("Password must contain 12–1024 characters.");
        var user = new User { Username = args[1].Trim(), NormalizedUsername = User.NormalizeUsername(args[1]), IdNumber = args[2] };
        user.PasswordHash = services.GetRequiredService<IPasswordHasher<User>>().HashPassword(user, password.ToString());
        password.Clear();
        await services.GetRequiredService<IUserRepository>().CreateAsync(user, CancellationToken.None);
        Console.WriteLine("User created.");
    }
}
