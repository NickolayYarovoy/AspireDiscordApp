using CommonClasses;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Net.Http.Json;

namespace AspireDiscordApp
{
    public class BankModule : ModuleBase<SocketCommandContext>
    {
        private readonly HttpClient httpClient;

        public BankModule(HttpClient httpClient) : base()
        {
            this.httpClient = httpClient;
        }

        [Command("register")]
        public async Task RegisterUser(string name)
        {
            var response = await httpClient.PostAsync($"api/user", JsonContent.Create(new CreateUserJson
            {
                Id = Context.User.Id,
                Name = name
            }));

            if(response.IsSuccessStatusCode)
            {
                await Context.Channel.SendMessageAsync("Вы успешно зарегистрированы!");
            }
            else if(await response.Content.ReadAsStringAsync() == "Пользователь с данным Id уже существует")
            {
                await Context.Channel.SendMessageAsync("Регистрация не удалась: вы уже зарегистрированы");
            }
        }

        [Command("users")]
        public async Task GetUsersList()
        {
            var response = await httpClient.GetAsync("api/users");

            var users = JsonConvert.DeserializeObject<NameClass[]>(await response.Content.ReadAsStringAsync());

            await Context.Channel.SendMessageAsync(string.Join('\n', users.Select(x => $"{x.Name}: {x.Id}")));
        }

        [Command("new_account")]
        public async Task CreateNewBankAccount(string name)
        {
            var response = await httpClient.PostAsync($"api/account", JsonContent.Create(new CreateAccountJson
            {
                UserId = Context.User.Id,
                Name = name
            }));

            if (response.IsSuccessStatusCode)
            {
                await Context.Channel.SendMessageAsync("Новый аккаунт был создан");
            }
        }

        [Command("accounts")]
        public async Task GetAccountsList()
        {
            ulong id = Context.User.Id;

            var response = await httpClient.GetAsync($"api/user/{id}");

            if (response.IsSuccessStatusCode)
            {
                var user = JsonConvert.DeserializeObject<UserJson>(await response.Content.ReadAsStringAsync());

                await Context.Channel.SendMessageAsync("Аккаунты пользователя:\n" + string.Join('\n', user.AccountsIds.Select(x => $"{x.Name}: {x.Id}")));
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                await Context.Channel.SendMessageAsync("Пользователь не найден");
            }
        }

        [Command("accounts")]
        public async Task GetAccountsList(ulong id)
        {
            var response = await httpClient.GetAsync($"api/user/{id}");

            if (response.IsSuccessStatusCode)
            {
                var user = JsonConvert.DeserializeObject<UserJson>(await response.Content.ReadAsStringAsync());

                await Context.Channel.SendMessageAsync(string.Join('\n', user.AccountsIds.Select(x => $"{x.Name}: {x.Id}")));
            }
            else if(response.StatusCode  == System.Net.HttpStatusCode.NotFound)
            {
                await Context.Channel.SendMessageAsync("Пользователь не найден");
            }
        }

        [Command("account")]
        public async Task GetAccountBalanceList(string str_id)
        {
            if(!Guid.TryParse(str_id, out Guid id))
            { 
                await Context.Channel.SendMessageAsync("Некорректный id");
                return;
            }

            var response = await httpClient.GetAsync($"api/account/{id}");

            if (response.IsSuccessStatusCode)
            {
                var account = JsonConvert.DeserializeObject<BankAccountJson>(await response.Content.ReadAsStringAsync());

                await Context.Channel.SendMessageAsync(account.Balance.ToString());
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                await Context.Channel.SendMessageAsync("Банковский счет не найден");
            }
        }

        [Command("top up")]
        public async Task TopUpAccount(string str_id, ulong value)
        {
            if (!Guid.TryParse(str_id, out Guid id))
            { 
                await Context.Channel.SendMessageAsync("Некорректный id");
                return;
            }

            var response = await httpClient.PutAsync($"api/top_up", JsonContent.Create(new ChangeBalanceJson { AccountId =  id, Value= value}));

            if (response.IsSuccessStatusCode)
            {
                await Context.Channel.SendMessageAsync("Счет успешно пополнен");
            }
        }

        [Command("withdrawn")]
        public async Task WithdrawnAccountMoney(string str_id, ulong value)
        {
            if (!Guid.TryParse(str_id, out Guid id))
            { 
                await Context.Channel.SendMessageAsync("Некорректный id");
                return;
            }

            var response = await httpClient.PutAsync($"api/withdrawn", JsonContent.Create(new ChangeBalanceJson { AccountId = id, Value = value }));

            if (response.IsSuccessStatusCode)
            {
                await Context.Channel.SendMessageAsync("Деньги были успешно сняты со счета");
            }
            else if(await response.Content.ReadAsStringAsync() == "На счету недостаточно средств")
            {
                await Context.Channel.SendMessageAsync("На счету недостаточно средств");
            }
        }

        [Command("remittance")]
        public async Task RemittanceAccountMoney(string str_senderId, string str_recipientId, ulong value)
        {
            if (!Guid.TryParse(str_senderId, out Guid senderId))
            {
                await Context.Channel.SendMessageAsync("Некорректный id отправителя");
                return;
            }

            if (!Guid.TryParse(str_recipientId, out Guid recipientId))
            { 
                await Context.Channel.SendMessageAsync("Некорректный id получателя");
                return;
            }

            var response = await httpClient.PutAsync($"api/remittance", JsonContent.Create(new RemittanceJson
            {
                SenderAccount = senderId,
                RecipientAccount = recipientId,
                Value = value
            }));

            if (response.IsSuccessStatusCode)
            {
                await Context.Channel.SendMessageAsync("Деньги были успешно переведены");
            }
            else if (await response.Content.ReadAsStringAsync() == "На счету недостаточно средств")
            {
                await Context.Channel.SendMessageAsync("На счету недостаточно средств");
            }
        }
    }
}
