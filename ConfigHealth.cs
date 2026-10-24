using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class ConfigHealth
{
    [Function("ConfigHealth")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "config-health")] HttpRequestData req)
    {
        static bool Has(params string[] names) =>
            names.Any(n => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(n)));

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new
        {
            ok = true,
            shopifyToken = Has("SHOPIFY_ACCESS_TOKEN", "ShopifyConfig__ShopifyToken"),
            shopifyShop = Has("SHOPIFY_STORE_URL", "SHOPIFY_SHOP", "ShopifyConfig__ShopifyStore"),
            smtpUser = Has("SMTP_USER", "EmailSettings__FromEmail"),
            smtpPassword = Has("SMTP_PASSWORD", "EmailSettings__Password"),
            at = DateTimeOffset.UtcNow
        });
        return res;
    }
}
