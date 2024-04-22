using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AiKey.Api.Controllers;

[ApiController]
[Route("/")]
public class AiKeyController : ControllerBase
{
    private readonly TokenCredential tokenCredential;
    private readonly HttpClient httpClient;

    public AiKeyController([FromKeyedServices(Constants.NamedServices.AzureAiServiceCredential)] TokenCredential tokenCredential, HttpClient httpClient)
    {
        this.tokenCredential = tokenCredential;
        this.httpClient = httpClient;
    }

    [Route("BearerToken")]
    [HttpGet]
    public async Task<IActionResult> GetBearerToken()
    {
        string token;

        try
        {
            AccessToken accessToken = await tokenCredential.GetTokenAsync(new TokenRequestContext(new[] { "https://cognitiveservices.azure.com/.default" }), CancellationToken.None);
            token = accessToken.Token;

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new Exception("Token can't be empty");
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to get token", ex);
        }

        return Ok($"Token:\n{token}");
    }

    [Route("AiCompletion")]
    [HttpGet]
    public async Task<IActionResult> GetAiCompletion2(string input, string? bearerToken = default, CancellationToken ctx = default)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            AccessToken accessToken = await tokenCredential.GetTokenAsync(new TokenRequestContext(["https://cognitiveservices.azure.com/.default"]), ctx);
            bearerToken = accessToken.Token;
        }

        string? deployment = Environment.GetEnvironmentVariable("DEPLOYMENT");
        string? endpoint = Environment.GetEnvironmentVariable("ENDPOINT");
        string? apiVersion = Environment.GetEnvironmentVariable("API_VERSION");

        if (string.IsNullOrWhiteSpace(deployment) || string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiVersion))
        {
            throw new InvalidOperationException("Deployment, Endpoint, and ApiVersion must be provided.");
        }

        var uri = new Uri($"{endpoint}/openai/deployments/{deployment}/completions?api-version={apiVersion}", UriKind.Absolute);
        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);

        httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        httpRequestMessage.Content = new StringContent(JsonSerializer.Serialize(new
        {
            prompt = $"Wrong answers only.\nI say: {input}\nYou say: ",
            max_tokens = 100,
            temperature = 0.1
        }), Encoding.UTF8, "application/json");

        using HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, ctx);
        httpResponseMessage.EnsureSuccessStatusCode();

        string jsonResult = await httpResponseMessage.Content.ReadAsStringAsync(ctx);
        return Ok(JsonSerializer.Serialize(JsonDocument.Parse(jsonResult), new JsonSerializerOptions { WriteIndented = true }));
    }
}
