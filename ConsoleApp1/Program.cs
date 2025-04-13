using Microsoft.AspNetCore.SignalR.Client;

var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5020/osHub")
    .WithAutomaticReconnect()
    .Build();

connection.On<string>("NovaOSRecebida", (mensagem) =>
{
    Console.WriteLine($"Nova OS recebida: {mensagem}");
});

await connection.StartAsync();
Console.WriteLine("Conectado ao SignalR. Aguardando mensagens...");

Console.ReadLine();