using OpenSearch.Client;
using OpenSearch.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;

namespace AnaliseLogsOpenSearch.Extensions;

public static class OpensearchExtension
{
    public static void AddOpensearch(this IServiceCollection services, IConfiguration configuration)
    {
        var node = new Uri(configuration.GetSection("Opensearch")["Url"] ?? "http://localhost:9200");

        var config = new ConnectionSettings(node)
            // Habilita o cliente a incluir metadados adicionais de diagnóstico nas requisições
            .EnableDebugMode()
            // Previne falhas se o mapeamento do JSON tentar parsear as propriedades nulas de rastro
            .ThrowExceptions();

        var client = new OpenSearchClient(config);

        services.AddSingleton(client);
    }
}