using System.ComponentModel.DataAnnotations;

namespace RA_MuniAnnouncements.Api.Models;

public sealed record LoginRequest(
    [Required, StringLength(100)] string Username,
    [Required, StringLength(1024)] string Password);

public sealed record LoginResponse(string Token, DateTimeOffset ExpiresAt);
