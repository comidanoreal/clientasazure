using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class ShopifyService
{
    private readonly string accessToken = Environment.GetEnvironmentVariable("SHOPIFY_ACCESS_TOKEN");
    private readonly string shopUrl = Environment.GetEnvironmentVariable("SHOPIFY_SHOP_URL");

    private readonly HttpClient client;

    public ShopifyService()
    {
        client = new HttpClient();
        client.BaseAddress = new Uri($"{shopUrl}/admin/api/2025-01/graphql.json");
        client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> LanzarOperacionBulkYEsperar()
    {
        string bulkQuery = @"
        mutation {
          bulkOperationRunQuery(
            query: """
            {
              orders(query: \"created_at:>=2020-01-01\") {
                edges {
                  node {
                    id
                    createdAt
                    sourceName
                    totalPriceSet { shopMoney { amount } }
                    customer { id }
                  }
                }
              }
            }
            """
          ) {
            bulkOperation { id status }
            userErrors { field message }
          }
        }";

        var request = new StringContent(JsonConvert.SerializeObject(new { query = bulkQuery }), Encoding.UTF8, "application/json");
        await client.PostAsync("", request);
        return await EsperarYObtenerUrlDeResultado();
    }

    private async Task<string> EsperarYObtenerUrlDeResultado()
    {
        string statusQuery = @"{ currentBulkOperation { id status errorCode url objectCount } }";
        for (int i = 0; i < 30; i++)
        {
            var request = new StringContent(JsonConvert.SerializeObject(new { query = statusQuery }), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("", request);
            var result = await response.Content.ReadAsStringAsync();
            var data = JObject.Parse(result)["data"]["currentBulkOperation"];

            if (data?["status"]?.ToString() == "COMPLETED")
                return data["url"]?.ToString();

            await Task.Delay(TimeSpan.FromSeconds(10));
        }
        throw new Exception("La operación bulk no se completó a tiempo.");
    }

    public async Task<List<Order>> DescargarOrdenesDesdeJsonl(string url)
    {
        var ordenes = new List<Order>();
        using var download = await new HttpClient().GetStreamAsync(url);
        using var reader = new StreamReader(download);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            var j = JObject.Parse(line);
            var order = new Order
            {
                OrderId = j["id"]?.ToString(),
                CreatedAt = DateTime.Parse(j["createdAt"].ToString()),
                CustomerId = j["customer"]?["id"]?.ToString(),
                Total = double.Parse(j["totalPriceSet"]["shopMoney"]["amount"].ToString()),
                CanalOrigen = j["sourceName"]?.ToString()
            };
            ordenes.Add(order);
        }

        return ordenes;
    }

    public async Task ActualizarTags(Dictionary<string, List<string>> clientaTags, ILogger log)
    {
        foreach (var kvp in clientaTags)
        {
            var mutation = new
            {
                query = @"
                mutation tagsAdd($id: ID!, $tags: [String!]!) {
                  tagsAdd(id: $id, tags: $tags) {
                    userErrors { field message }
                  }
                }",
                variables = new { id = kvp.Key, tags = kvp.Value }
            };

            var body = new StringContent(JsonConvert.SerializeObject(mutation), Encoding.UTF8, "application/json");
            await client.PostAsync("", body);
            log.LogInformation($"✅ Tags aplicados a {kvp.Key}: {string.Join(", ", kvp.Value)}");
        }
    }
}
