using Microsoft.FeatureManagement;
using PedidosApi.Features;

namespace PedidosApi.Endpoints;

public static class PedidoEndpoints
{
    public static void MapPedidoEndpoints(this WebApplication app)
    {
        // -------------------------------------------------------
        // Endpoint existente — não mudou nada com o TBD
        // -------------------------------------------------------
        app.MapGet("/pedidos", () =>
            Results.Ok(new[]
            {
                new { Id = 1, Valor = 100m, ClientePremium = false },
                new { Id = 2, Valor = 250m, ClientePremium = true  },
                new { Id = 3, Valor = 80m,  ClientePremium = false }
            }))
            .WithName("ListarPedidos")
            .WithTags("Pedidos")
            .WithSummary("Lista todos os pedidos");

        app.MapPost("/pedidos/calcular-desconto",
            async (PedidoDto dto, DescontoService descontoService) =>
            {
                var desconto = await descontoService.CalcularAsync(
                    dto.Valor,
                    dto.ClientePremium);

                return Results.Ok(new
                {
                    dto.Valor,
                    dto.ClientePremium,
                    Desconto = desconto,
                    Total = dto.Valor - desconto,
                    FeatureAtiva = desconto > 0
                });
            })
            .WithName("CalcularDesconto")
            .WithTags("Pedidos")
            .WithSummary("Calcula desconto para um pedido (controlado por feature flag)");

        app.MapGet("/feature-flags",
            async (IFeatureManager featureManager) =>
            {
                return Results.Ok(new
                {
                    DescontoPremium = await featureManager
                        .IsEnabledAsync(FeatureFlags.DescontoPremium),
                    NovoCalculoFrete = await featureManager
                        .IsEnabledAsync(FeatureFlags.NovoCalculoFrete)
                });
            })
            .WithName("StatusFlags")
            .WithTags("Feature Flags")
            .WithSummary("Retorna o status atual de todas as feature flags");

        app.MapGet("/info", (IWebHostEnvironment env) =>
        {
            var versao = typeof(Program).Assembly
                            .GetName().Version?.ToString() ?? "0.0.0";
            return Results.Ok(new
            {
                Ambiente    = env.EnvironmentName,
                Versao      = versao,
                Application = "PedidosApi",
                Timestamp   = DateTime.UtcNow
            });
        })
        .WithName("Info")
        .WithTags("Sistema");
       
    }
}

// -------------------------------------------------------
// DTO da requisição
// -------------------------------------------------------
record PedidoDto(decimal Valor, bool ClientePremium);
