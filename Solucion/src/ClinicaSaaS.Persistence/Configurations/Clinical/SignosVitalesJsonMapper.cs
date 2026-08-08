using System.Text.Json.Nodes;
using ClinicaSaaS.Domain.Clinical.ValueObjects;

namespace ClinicaSaaS.Persistence.Configurations.Clinical;

/// <summary>
/// SignosVitales es un record con constructor privado y factory estático Crear(...) — por
/// diseño del dominio (Fase 2), no expone un constructor público sin parámetros ni setters,
/// así que System.Text.Json NO puede reconstruirlo automáticamente por reflexión. En vez de
/// agregar un atributo [JsonConstructor] al dominio (fuera de alcance en esta fase: "NO debes
/// modificar el dominio"), este mapper serializa/deserializa a mano leyendo las propiedades
/// públicas y llamando siempre al factory público Crear(...), sin tocar SignosVitales.cs.
/// </summary>
internal static class SignosVitalesJsonMapper
{
    public static string? Serializar(SignosVitales? signosVitales)
    {
        if (signosVitales is null) return null;

        var json = new JsonObject
        {
            ["presionSistolica"] = signosVitales.PresionSistolica,
            ["presionDiastolica"] = signosVitales.PresionDiastolica,
            ["temperaturaCelsius"] = signosVitales.TemperaturaCelsius,
            ["frecuenciaCardiaca"] = signosVitales.FrecuenciaCardiaca,
            ["frecuenciaRespiratoria"] = signosVitales.FrecuenciaRespiratoria,
            ["saturacionOxigeno"] = signosVitales.SaturacionOxigeno,
            ["pesoKg"] = signosVitales.PesoKg,
            ["tallaCm"] = signosVitales.TallaCm
        };

        return json.ToJsonString();
    }

    public static SignosVitales? Deserializar(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        var nodo = JsonNode.Parse(json)!.AsObject();

        var resultado = SignosVitales.Crear(
            presionSistolica: (decimal?)nodo["presionSistolica"],
            presionDiastolica: (decimal?)nodo["presionDiastolica"],
            temperaturaCelsius: (decimal?)nodo["temperaturaCelsius"],
            frecuenciaCardiaca: (int?)nodo["frecuenciaCardiaca"],
            frecuenciaRespiratoria: (int?)nodo["frecuenciaRespiratoria"],
            saturacionOxigeno: (decimal?)nodo["saturacionOxigeno"],
            pesoKg: (decimal?)nodo["pesoKg"],
            tallaCm: (decimal?)nodo["tallaCm"]);

        if (resultado.EsFallido)
            throw new InvalidOperationException(
                $"SignosVitalesJson corrupto en base de datos: {resultado.Error}");

        return resultado.Value;
    }
}
