using Microsoft.AspNetCore.SignalR.Client;

Console.WriteLine("Iniciando cliente SignalR...");

var hubConnection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5020/osHub")
    .WithAutomaticReconnect()
    .Build();

hubConnection.On<string>("NovaOSRecebida", (mensagem) =>
{
    Console.WriteLine($"[Sinal recebido] NovaOSRecebida: {mensagem}");
});

try
{
    await hubConnection.StartAsync();
    Console.WriteLine("Conexão iniciada. Aguardando sinais...");
}
catch (Exception ex)
{
    Console.WriteLine("Erro ao conectar: " + ex.Message);
}

Console.WriteLine("Pressione qualquer tecla para sair...");
Console.ReadKey();

await hubConnection.StopAsync();