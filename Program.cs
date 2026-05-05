using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using PedidosApi.Features;
using PedidosApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddFeatureManagement()
    .AddFeatureFilter<PercentageFilter>()
    .AddFeatureFilter<UserIdPercentageFilter>();

builder.Services.AddScoped<DescontoService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Lê o ambiente e a versão do assembly (injetada pelo pipeline)
    var ambiente = builder.Environment.EnvironmentName;
    var versao   = typeof(Program).Assembly
                       .GetName().Version?.ToString() ?? "0.0.0";

    options.SwaggerDoc("v1", new()
    {
        Title       = $"PedidosApi — {ambiente}",
        Version     = versao,
        Description = $"""
            API de pedidos com Feature Flags e Rollout Gradual.

            Ambiente  : {ambiente}
            Versão    : {versao}
            Repositório: Azure Repos / PedidosApi
            """
    });
});

var app = builder.Build();

// Swagger sempre visível (todos os ambientes)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "PedidosApi v1");
    options.DocumentTitle = $"PedidosApi — {app.Environment.EnvironmentName}";
});

app.MapPedidoEndpoints();

app.Run();

public partial class Program { }
