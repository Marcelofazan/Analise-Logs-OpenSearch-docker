using AnaliseLogsOpenSearch.Extensions;
using AnaliseLogsOpenSearch.Middlewares;
using AnaliseLogsOpenSearch.Services;
using Microsoft.OpenApi;
using NLog;
using NLog.Web;
using System.Diagnostics;

// 1. Escuta diretamente a fonte que criamos no Middleware para o TraceId
ActivitySource.AddActivityListener(new ActivityListener
{
    ShouldListenTo = (source) => source.Name == "AnaliseLogsOpenSearch.API",
    Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
    ActivityStarted = activity => { },
    ActivityStopped = activity => { }
});

// Inicializa o motor do NLog carregando o arquivo de configuração
var logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // 2. CORREÇÃO DE CORS: Permite que o OpenSearch Dashboards local consulte a API sem bloqueios
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AnaliseLogsCORS", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // 3. CONFIGURAÇÃO DE LOGS: Substitui o provedor padrão pelo NLog de forma limpa
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Adiciona os serviços de controle e o serializador customizado
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.Converters.Add(new DateTimeLocalConverter());
        });

    // 4. SERVIÇOS CRÍTICOS: Adiciona o serviço de autorização obrigatório para o UseAuthorization
    builder.Services.AddAuthorization();

    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.MapType<DateTime>(() =>
        {
            // Obtém o fuso horário oficial de Brasília
            TimeZoneInfo brazilZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            DateTime brazilTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brazilZone);

            return new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Format = "date-time",
                // CORREÇÃO .NET 10: Usa o nó JSON nativo em vez de OpenApiString
                Example = System.Text.Json.Nodes.JsonValue.Create(brazilTime.ToString("yyyy-MM-ddTHH:mm:ss.fff"))
            };
        });
    });


    builder.Services.AddScoped<IProdutoService, ProdutoService>();
    builder.Services.AddOpensearch(builder.Configuration);

    var app = builder.Build();

    // 6. PIPELINE DE MIDDLEWARES
    app.UseCors("AnaliseLogsCORS");
    app.UseMiddleware<TracingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception exception)
{
    logger.Error(exception, "Falha fatal durante a inicialização da aplicação.");
    throw;
}
finally
{
    // Garante que o buffer de logs do OpenSearch descarregue antes de fechar a API
    LogManager.Shutdown();
}

// Classe auxiliar de conversão do formato JSON
public class DateTimeLocalConverter : System.Text.Json.Serialization.JsonConverter<DateTime>
{
    public override DateTime Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        return DateTime.Parse(reader.GetString()!);
    }

    public override void Write(System.Text.Json.Utf8JsonWriter writer, DateTime value, System.Text.Json.JsonSerializerOptions options)
    {
        // Força a escrita no formato ISO local com milissegundos
        writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
    }
}