namespace Identity.Api.Data;

public sealed class UserAccount
{
    public Guid Id { get; set; }

    public string Email { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public string Role { get; set; } = "Player";
}