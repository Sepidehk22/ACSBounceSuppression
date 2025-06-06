using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ACSBounceSuppression.Services;
public class EmailSuppressionService
{
    private readonly ILogger<EmailSuppressionService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _acsEndpoint;
    private readonly TokenCredential _credential;

    public EmailSuppressionService(IConfiguration configuration, ILogger<EmailSuppressionService> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();

        _acsEndpoint = configuration["ACSEndpoint"] ?? throw new InvalidOperationException("ACSEndpoint not set in configuration");
        _credential = new DefaultAzureCredential(); // uses Managed Identity in Azure
    }

    public async Task AddEmailToSuppressionListAsync(string emailAddress)
    {
        try
        {
            _logger.LogInformation($"Adding {emailAddress} to ACS suppression list...");

            // Get token to call ACS Email Suppression API
            var tokenRequestContext = new TokenRequestContext(new[] { "https://communication.azure.com/.default" });
            var token = await _credential.GetTokenAsync(tokenRequestContext, CancellationToken.None);

            // Build request
            var url = $"{_acsEndpoint}/emailsuppressions?api-version=2023-03-31";
            var request = new HttpRequestMessage(HttpMethod.Post, url);

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            request.Content = new StringContent(JsonSerializer.Serialize(new
            {
                recipientAddress = emailAddress,
                reason = "BounceDetected"
            }), Encoding.UTF8, "application/json");

            // Send
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"✅ {emailAddress} added to suppression list");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"❌ Failed to suppress {emailAddress}: {response.StatusCode} - {error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error adding {emailAddress} to suppression list");
            throw;
        }
    }
}

