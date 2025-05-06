using System.Text.RegularExpressions;
using AutomatizarOs.Core.Enums;
using AutomatizarOs.Core.Handlers;
using AutomatizarOs.Core.Models;
using AutomatizarOs.Web.Components;
using AutomatizarOs.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;

namespace AutomatizarOs.Web.Pages;

public partial class Home : ComponentBase
{
    public string SearchOsNumber { get; set; } = string.Empty;
    
    public List<ServiceOrder> ServiceOrders { get; set; } = new();
    public List<ServiceOrder> OrcamentosPendentes => ServiceOrders
        .Where(s => s.EServiceOrderStatus == EServiceOrderStatus.Entered && s.ERepair == ERepair.Entered)
        .OrderBy(s => s.Id)
        .ToList();
    public List<ServiceOrder> AguardandoResposta => ServiceOrders
        .Where(s => s.EServiceOrderStatus == EServiceOrderStatus.Evaluated && s.ERepair == ERepair.Waiting)
        .OrderBy(s => s.Id)
        .ToList();
    public List<ServiceOrder> AguardandoPeca => ServiceOrders
        .Where(s => s.EServiceOrderStatus == EServiceOrderStatus.Evaluated && s.ERepair == ERepair.Approved)
        .OrderBy(s => s.Id)
        .ToList();
    public List<ServiceOrder> AguardandoColeta => ServiceOrders
        .Where(s => s.EServiceOrderStatus == EServiceOrderStatus.Repaired || 
                    (s.EServiceOrderStatus == EServiceOrderStatus.Evaluated && 
                     (s.ERepair == ERepair.Disapproved || s.EUnrepaired == EUnrepaired.Unrepaired || s.EUnrepaired == EUnrepaired.NoDefectFound)))
        .OrderBy(s => s.Id)
        .ToList();
    public List<ServiceOrder> Entregues => ServiceOrders
        .Where(s => s.EServiceOrderStatus == EServiceOrderStatus.Delivered)
        .OrderBy(s => s.Id)
        .ToList();

        
    
    [Inject] public IServiceOrderHandler ServiceOrderHandler { get; set; } = null!;
    [Inject] public HubConnection HubConnection { get; set; } = null!;
    [Inject] public IDialogService DialogService { get; set; } = null!;
    [Inject] public CustomAuthStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;
    [Inject] public ISnackbar Snackbar { get; set; } = null!;

    
    protected override async Task OnInitializedAsync()
    {
        try
        {
            HubConnection.On<string>("NovaOSRecebida", async (mensagem) =>
            {
                Console.WriteLine($"Web socket Recebido: {mensagem}");
                await AtualizarServiceOrders();
            });

            if (HubConnection.State == HubConnectionState.Disconnected)
            {
                await HubConnection.StartAsync();
                Console.WriteLine("SignalR conectado");
            }
        
            await AtualizarServiceOrders();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro na inicialização: {ex.Message}");
        }
    }
    
    private async Task AtualizarServiceOrders()
    {
        var ordersResult = await ServiceOrderHandler.GetAllServiceOrder();
        ServiceOrders = ordersResult.Data ?? new List<ServiceOrder>();
        StateHasChanged();
    }
    
    private async Task AbrirDialogAtualizar(ServiceOrder ordem)
    {
        var parameters = new DialogParameters
        {
            ["ServiceOrder"] = ordem,
            ["OnQuoteAdded"] = EventCallback.Factory.Create(this, AtualizarServiceOrders)
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Large
        };

        var dialog = await DialogService.ShowAsync<AddOrcamentoDialog>("Atualizar Ordem de Serviço", parameters, options);
        var result = await dialog.Result;
    
        if (result!.Canceled)
        {
            await AtualizarServiceOrders();
        }
    }
    
    private async Task AbrirDialogResposta(ServiceOrder ordem)
    {
        var parameters = new DialogParameters
        {
            ["ServiceOrder"] = ordem,
            ["OnResponseAdded"] = EventCallback.Factory.Create(this, AtualizarServiceOrders)
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Large
        };

        var dialog = await DialogService.ShowAsync<AddRespostaDialog>("Adicionar resposta", parameters, options);
        var result = await dialog.Result;
    
        if (result!.Canceled)
        {
            await AtualizarServiceOrders();
        }
    }
    
    private async Task AbrirDialogConserto(ServiceOrder ordem)
    {
        var parameters = new DialogParameters
        {
            ["ServiceOrder"] = ordem,
            ["OnConsertoAdded"] = EventCallback.Factory.Create(this, AtualizarServiceOrders)
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Large
        };

        var dialog = await DialogService.ShowAsync<AddConsertoDialog>("Adicionar conserto", parameters, options);
        var result = await dialog.Result;
    
        if (result!.Canceled)
        {
            await AtualizarServiceOrders();
        }
    }
    
    private async Task AbrirDialogEntrega(ServiceOrder ordem)
    {
        var parameters = new DialogParameters
        {
            ["ServiceOrder"] = ordem,
            ["OnEntregaAdded"] = EventCallback.Factory.Create(this, AtualizarServiceOrders)
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Large
        };

        var dialog = await DialogService.ShowAsync<AddEntregaDialog>("Adicionar entrega", parameters, options);
        var result = await dialog.Result;
    
        if (result!.Canceled)
        {
            await AtualizarServiceOrders();
        }
    }
    
    private async Task AbrirDialogVer(ServiceOrder ordem)
    {
        var parameters = new DialogParameters
        {
            ["ServiceOrder"] = ordem
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Large
        };

        await DialogService.ShowAsync<AddVerDialog>("Ver OS", parameters, options);
    }
    
    private async Task ConsultarOs(string number)
    {
        try
        {
            var digitsOnly = Regex.Replace(number, "[^0-9]", "");
            
            if (!long.TryParse(digitsOnly, out var id))
            {
                Snackbar.Add("Número da OS inválido. Digite apenas números.", Severity.Error);
                return;
            }
            
            var result = await ServiceOrderHandler.GetCloudServiceOrder(id);
            if (!result.IsSuccess)
            {
                Snackbar.Add($"Erro: {result.Message}", Severity.Error);
                return;
            }

            if (result.Data == null)
            {
                Snackbar.Add("Nenhuma OS localizada.", Severity.Error);
                return;
            }
            
            var serviceOrder = result.Data;
            
            await AbrirDialogVer(serviceOrder);
            
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}