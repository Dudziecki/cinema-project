using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cinema.Contracts;

namespace Cinema.Blazor.Services;

public sealed class CinemaApiClient(HttpClient http, AuthSession session)
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        using var response = await http.PostAsJsonAsync("/api/auth/login", request, cancellationToken: ct);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct))!;
    }

    public Task<List<MovieDto>> GetMoviesAsync(CancellationToken ct = default) =>
        SendAsync<List<MovieDto>>(HttpMethod.Get, "/api/movies", cancellationToken: ct);

    public Task<List<GenreDto>> GetGenresAsync(CancellationToken ct = default) =>
        SendAsync<List<GenreDto>>(HttpMethod.Get, "/api/genres", cancellationToken: ct);

    public Task<MovieDto> CreateMovieAsync(MovieUpsertDto dto, CancellationToken ct = default) =>
        SendAsync<MovieDto>(HttpMethod.Post, "/api/movies", dto, ct);

    public async Task UpdateMovieAsync(int id, MovieUpsertDto dto, CancellationToken ct = default) =>
        await SendNoContentAsync(HttpMethod.Put, $"/api/movies/{id}", dto, ct);

    public async Task DeleteMovieAsync(int id, CancellationToken ct = default) =>
        await SendNoContentAsync(HttpMethod.Delete, $"/api/movies/{id}", null, ct);

    public Task<List<ScreeningDto>> GetScreeningsAsync(CancellationToken ct = default) =>
        SendAsync<List<ScreeningDto>>(HttpMethod.Get, "/api/screenings", cancellationToken: ct);

    public Task<TicketDto> SellTicketAsync(TicketCreateDto dto, CancellationToken ct = default) =>
        SendAsync<TicketDto>(HttpMethod.Post, "/api/tickets", dto, ct);

    private async Task<T> SendAsync<T>(HttpMethod method, string url, object? body = null, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(method, url, body);
        using var response = await http.SendAsync(request, cancellationToken);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken))!;
    }

    private async Task SendNoContentAsync(HttpMethod method, string url, object? body, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(method, url, body);
        using var response = await http.SendAsync(request, cancellationToken);
        await EnsureSuccess(response);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url, object? body)
    {
        var request = new HttpRequestMessage(method, url);
        if (body is not null) request.Content = JsonContent.Create(body);
        if (session.IsAuthenticated)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return request;
    }

    private static async Task EnsureSuccess(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync();
        var message = string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body;
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new InvalidOperationException("Неверный логин или пароль либо токен истёк.");
        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new InvalidOperationException("Недостаточно прав для выполнения операции.");
        throw new InvalidOperationException(message ?? "Ошибка HTTP-запроса.");
    }
}
