using ACSBounceSuppression.Models;
using ACSBounceSuppression.Services;
using Azure.Messaging.EventGrid;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ACSBounceSuppression.Functions;

public class EmailBounceHandler
{
    private readonly EmailSuppressionService _suppressionService;
    private readonly ILogger<EmailBounceHandler> _logger;
    private readonly BlobServiceClient _blobServiceClient;

    public EmailBounceHandler(
        EmailSuppressionService suppressionService,
        ILogger<EmailBounceHandler> logger,
        BlobServiceClient blobServiceClient)
    {
        _suppressionService = suppressionService;
        _logger = logger;
        _blobServiceClient = blobServiceClient;
    }

    [Function("HandleEmailBounces")]
    public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        _logger.LogInformation($" Ricevuto evento: {eventGridEvent.EventType}");

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var bounceData = JsonSerializer.Deserialize<BounceNotification>(eventGridEvent.Data.ToString(), options);

            // Fix: Use correct property names from BounceNotification
            // Assuming BounceNotification has properties: Type, Recipient, Reason
            // Use the correct property for bounce type, e.g., BounceType or Type
            if (string.IsNullOrEmpty(bounceData.BounceReason))
            {
                _logger.LogWarning($"Email Bounced: {bounceData.RecipientAddress} - Motivo: {bounceData.BounceReason}");

                // 1. Aggiungi alla suppression list
                await _suppressionService.AddEmailToSuppressionListAsync(bounceData.RecipientAddress);

                // 2. Salva evento raw in Storage Account
                var containerClient = _blobServiceClient.GetBlobContainerClient("emailbounces");
                await containerClient.CreateIfNotExistsAsync();

                var blobName = $"bounce-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid()}.json";
                var blobClient = containerClient.GetBlobClient(blobName);
                await blobClient.UploadAsync(BinaryData.FromString(eventGridEvent.Data.ToString()));

                _logger.LogInformation($"Evento salvato in Storage: {blobName}");
            }
            else
            {
                _logger.LogInformation(" Evento ignorato: non è un bounce oppure manca l'indirizzo.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Errore durante la gestione dell’evento: {ex.Message}");
            throw;
        }
    }
}
