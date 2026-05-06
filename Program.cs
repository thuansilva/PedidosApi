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
    var ambiente = builder.Environment.EnvironmentName;
    // Depois — lê InformationalVersion (preserva 1.3.0, 1.3.0-beta.1, etc.)
    var versaoCompleta = typeof(Program).Assembly
                    .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion ?? "0.0.0";
                    
    // Remove o hash do commit (+abc123...)
    var versao = versaoCompleta.Split('+')[0];

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
