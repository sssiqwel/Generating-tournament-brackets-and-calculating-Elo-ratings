namespace Tournaments.Web;

public sealed class PlayerSession
{
    public string? AccessToken { get; set; }
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
}