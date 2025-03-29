using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

public class ClientBulkClassifier
{
    private readonly ShopifyService _shopify;
    private readonly ClientClassifierEngine _classifier;
    private readonly ILogger _logger;

    public ClientBulkClassifier(ShopifyService shopify, ClientClassifierEngine classifier, ILoggerFactory loggerFactory)
    {
        _shopify = shopify;
        _classifier = classifier;
        _logger = loggerFactory.CreateLogger<ClientBulkClassifier>();
    }

    [Function("ClientBulkClassifier")]
    public async Task Run([TimerTrigger("0 0 3 * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("🚀 Clasificación iniciada...");

        string url = await _shopify.LanzarOperacionBulkYEsperar();
        var ordenes = await _shopify.DescargarOrdenesDesdeJsonl(url);
        var clasificadas = _classifier.Clasificar(ordenes);

        var modoSimulacion = Environment.GetEnvironmentVariable("SIMULACION") == "true";
        if (modoSimulacion)
        {
            _logger.LogInformation("🧪 Modo simulación activado. No se aplican tags en Shopify.");
        }
        else
        {
            await _shopify.ActualizarTags(clasificadas, _logger);
        }

        _logger.LogInformation("✅ Clasificación terminada.");
    }
}
