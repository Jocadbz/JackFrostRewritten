using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

class Program
{

    public static class Globals
    {
        public static string CoinName = "Macca"; // Nome da moeda
        public static string BotName = "Jack Frost"; // Nome da moeda, usado em mensagens
    }
    public static async Task Main(string[] args)
    {
        var discord = new DiscordClient(new DiscordConfiguration
        {
            Token = "SEU_TOKEN_AQUI",
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged
        });

        // Handler para toda mensagem recebida
        discord.MessageCreated += async (client, e) =>
        {
            // Evite responder a mensagens de bots (inclusive o próprio)
            if (e.Author.IsBot) return;

            // Faça o que quiser aqui
            //Console.WriteLine($"Mensagem recebida de {e.Author.Username}: {e.Message.Content}");
        };

        // Configuração do CommandsNext
        var commands = discord.UseCommandsNext(new CommandsNextConfiguration
        {
            StringPrefixes = new[] { "d$" }
        });

        // Registro dos módulos de comando
        commands.RegisterCommands<PingModule>();

        await discord.ConnectAsync();
        await Task.Delay(-1);
    }
}

// Módulo de comandos
public class PingModule : BaseCommandModule
{
    [Command("adivinhar")]
    public async Task AdivinharAsync(CommandContext ctx, int valor, int aposta)
    {
        if (aposta < 1 || aposta > 10)
        {
            await ctx.RespondAsync("Aposta deve ser entre 1 e 10.");
            return;
        }
        if (valor <= 0)
        {
            await ctx.RespondAsync("Valor não pode ser menor que 0.");
            return;
        }
        int resultado = new Random().Next(1, 11); // Gera um número aleatório entre 1 e 10
        int dinheiroatual = int.Parse(File.ReadAllText($"profile/{ctx.Member.Id}/coins"));
        if (resultado == valor)
        {
            await ctx.RespondAsync($"Você acertou! O número era {resultado} e você apostou {aposta}.");
            File.WriteAllText($"profile/{ctx.Member.Id}/coins", (dinheiroatual + (valor * 5)).ToString());
        }
        else
        {
            await ctx.RespondAsync($"Você errou! O número era {resultado} e você apostou {aposta}.");
            File.WriteAllText($"profile/{ctx.Member.Id}/coins", (dinheiroatual - valor).ToString());
        }
    }

    [Command("aposta")]
    public async Task ApostaAsync(CommandContext ctx, int aposta, DiscordUser oponente)
    {
        if (aposta <= 0)
        {
            await ctx.RespondAsync("O valor da aposta deve ser maior que zero.");
            return;
        }
        if (ctx.User.Id == oponente.Id)
        {
            await ctx.RespondAsync("Você não pode apostar contra si mesmo.");
            return;
        }
        string userPath = $"profile/{ctx.Member.Id}/coins";
        string oponentePath = $"profile/{oponente.Id}/coins";
        if (int.Parse(File.ReadAllText(userPath)) < aposta)
        {
            await ctx.RespondAsync("Você não tem dinheiro suficiente para essa aposta.");
            return;
        }
        if (int.Parse(File.ReadAllText(oponentePath)) < aposta)
        {
            await ctx.RespondAsync("Seu oponente não tem dinheiro suficiente para essa aposta.");
            return;
        }

        var confirmMsg = await ctx.RespondAsync($"Atenção {oponente.Mention}, o {ctx.User.Mention} quer apostar {aposta} {Program.Globals.CoinName} com você. Reaja com 👍 em 15 segundos para confirmar a aposta.");
        await confirmMsg.CreateReactionAsync(DiscordEmoji.FromUnicode("👍"));

        var interactivity = ctx.Client.GetInteractivity();
        var reactionResult = await interactivity.WaitForReactionAsync(
            x => x.Message == confirmMsg && x.User == oponente && x.Emoji.GetDiscordName() == "👍",
            TimeSpan.FromSeconds(15)
        );

        if (reactionResult.TimedOut)
        {
            await confirmMsg.ModifyAsync("Aposta cancelada: tempo esgotado para confirmação.");
            return;
        }

        // Sorteia o vencedor
        List<DiscordUser> contestants = new List<DiscordUser> { ctx.User, oponente };
        DiscordUser winner = contestants[new Random().Next(contestants.Count)];
        DiscordUser loser = winner.Id == ctx.User.Id ? oponente : ctx.User;

        // Atualiza os saldos
        string winnerPath = $"profile/{winner.Id}/coins";
        string loserPath = $"profile/{loser.Id}/coins";
        int winnerMoney = int.Parse(File.ReadAllText(winnerPath));
        int loserMoney = int.Parse(File.ReadAllText(loserPath));

        File.WriteAllText(winnerPath, (winnerMoney + aposta).ToString());
        File.WriteAllText(loserPath, (loserMoney - aposta).ToString());

        await confirmMsg.ModifyAsync(
            $"Aposta confirmada! {ctx.User.Mention} apostou {aposta} contra {oponente.Mention}.\n" +
            $"O vencedor é {winner.Mention}! {aposta} moedas foram transferidas de {loser.Mention} para {winner.Mention}."
        );
    }

    [Command("avatar")]
    public async Task AvatarAsync(CommandContext ctx, DiscordUser user = null)
    {
        user ??= ctx.User; // Se nenhum usuário for especificado, usa o autor do comando
        var avatarUrl = user.AvatarUrl ?? user.DefaultAvatarUrl;
        var embed = new DiscordEmbedBuilder
        {
            Title = $"Avatar de {user.Username}",
            ImageUrl = avatarUrl,
            Color = DiscordColor.Blurple
        };
        await ctx.RespondAsync(embed: embed.Build());
    }
}