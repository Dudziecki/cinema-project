using Cinema.Contracts;

namespace Cinema.Blazor.Services;

/// <summary>Токен хранится в scoped-сессии Blazor Server, а не в localStorage.</summary>
public sealed class AuthSession
{
    public string? AccessToken { get; private set; }
    public string? Username { get; private set; }
    public string? Role { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken);
    public event Action? Changed;

    public void SignIn(LoginResponse response)
    {
        AccessToken = response.AccessToken;
        Username = response.Username;
        Role = response.Role;
        Changed?.Invoke();
    }

    public void SignOut()
    {
        AccessToken = null;
        Username = null;
        Role = null;
        Changed?.Invoke();
    }
}
