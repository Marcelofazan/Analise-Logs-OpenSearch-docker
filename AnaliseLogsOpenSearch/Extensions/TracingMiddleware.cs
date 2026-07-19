using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AnaliseLogsOpenSearch.Middlewares
{
    public class TracingMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly ActivitySource APIActivitySource = new("AnaliseLogsOpenSearch.API");

        public TracingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Inicia a atividade forçada para a requisição HTTP atual
            using Activity? activity = APIActivitySource.StartActivity($"HTTP {context.Request.Method} {context.Request.Path}");

            if (activity != null)
            {
                // Vincula o identificador único do ASP.NET Core como o ID pai do rastro
                activity.SetParentId(context.TraceIdentifier);
            }

            // Executa o pipeline (chama o controller correspondente e grava no SQLite)
            await _next(context);

            // Analisa e captura o Status Code assim que a execução do controlador finaliza
            if (activity != null)
            {
                // Se o status retornado for um erro do cliente (4xx) ou servidor (5xx), marca a Atividade como Falha
                if (context.Response.StatusCode >= 400)
                {
                    activity.SetStatus(ActivityStatusCode.Error, $"HTTP Failure {context.Response.StatusCode}");
                }
            }
        }
    }
}