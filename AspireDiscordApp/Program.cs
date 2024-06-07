using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace AspireDiscordApp
{
    internal class Program
    {
        private static DiscordSocketClient _client;
        private static CommandService _commands;
        private static CommandHandler _commandHandler;

        static async Task Main(string[] args)
        {
            ServiceCollection services = new ServiceCollection();

            services.AddHttpClient("WebAPI", x=> x.BaseAddress = new Uri("http://localhost:5084"));
            services.AddScoped(x => x.GetService<IHttpClientFactory>().CreateClient("WebAPI"));

            ServiceProvider prov = services.BuildServiceProvider();

            var config = new DiscordSocketConfig
            {
                AlwaysDownloadUsers = false,
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            };


            _client = new DiscordSocketClient(config);

            _client.Log += Log;

            var token = "<Your token here>";

            _commands = new CommandService();

            _commandHandler = new CommandHandler(_client, _commands, prov);
            await _commandHandler.InstallCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
