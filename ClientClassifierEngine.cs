using System;
using System.Collections.Generic;
using System.Linq;

public class ClientClassifierEngine
{
    public Dictionary<string, List<string>> Clasificar(List<Order> ordenes)
    {
        var agrupado = ordenes
            .Where(o => !string.IsNullOrEmpty(o.CustomerId))
            .GroupBy(o => o.CustomerId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var resultado = new Dictionary<string, List<string>>();
        DateTime ahora = DateTime.UtcNow;

        foreach (var cliente in agrupado)
        {
            string customerId = cliente.Key;
            var ordenesCliente = cliente.Value.OrderBy(o => o.CreatedAt).ToList();

            var tags = new List<string>();
            int totalOrdenes = ordenesCliente.Count;
            double totalGastado = ordenesCliente.Sum(o => o.Total);
            DateTime primeraCompra = ordenesCliente.First().CreatedAt;
            DateTime ultimaCompra = ordenesCliente.Last().CreatedAt;
            TimeSpan inactividad = ahora - ultimaCompra;
            TimeSpan antiguedad = ahora - primeraCompra;
            var canales = ordenesCliente.Select(o => o.CanalOrigen).Where(c => !string.IsNullOrEmpty(c)).Distinct();

            if (antiguedad.TotalDays >= 365 * 3 && totalOrdenes >= 15)
                tags.Add("La Leal Legendaria");

            if (totalGastado >= 4000000)
                tags.Add("Reina del Impacto");

            if (antiguedad.TotalDays < 60 && totalOrdenes >= 3)
                tags.Add("Estrella Emergente");

            if (inactividad.TotalDays > 90 && totalOrdenes >= 2)
                tags.Add("Alma Reincidente");

            if (antiguedad.TotalDays > 120 && inactividad.TotalDays <= 30)
                tags.Add("Musa del Movimiento");

            if (ordenesCliente.Count == 1 && antiguedad.TotalDays < 15)
                tags.Add("Visionaria del Cambio");

            if (ordenesCliente.Count >= 3)
                tags.Add("Reina del Carrito Perfecto");

            if (canales.Count() >= 2)
                tags.Add("Orquestadora de Experiencias");

            if (tags.Any())
                resultado[customerId] = tags;
        }

        return resultado;
    }
}
