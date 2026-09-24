using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using HFL.Core.Models;
using HFL.Server.Data;

namespace HFL.Server.Services.Telegram
{
    public class TelegramBotService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _config;
        private readonly ILogger<TelegramBotService> _logger;
        private ITelegramBotClient? _botClient;
        private readonly Dictionary<long, string> _userStates = new();

        public TelegramBotService(IServiceProvider serviceProvider, IConfiguration config, ILogger<TelegramBotService> logger)
        {
            _serviceProvider = serviceProvider;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string? botToken = _config["BOT_TOKEN"] ?? Environment.GetEnvironmentVariable("BOT_TOKEN");
            if (string.IsNullOrWhiteSpace(botToken) || botToken == "YOUR_BOT_TOKEN_HERE")
            {
                _logger.LogWarning("BOT_TOKEN not provided. Telegram Bot service is suspended until configured.");
                return;
            }

            _botClient = new TelegramBotClient(botToken);

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
            };

            _logger.LogInformation("Enterprise Telegram Bot Service starting...");

            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: stoppingToken
            );

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            try
            {
                if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
                {
                    await HandleCallbackAsync(bot, update.CallbackQuery, ct);
                    return;
                }

                if (update.Type == UpdateType.Message && update.Message != null)
                {
                    await HandleMessageAsync(bot, update.Message, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Telegram update");
            }
        }

        private async Task HandleMessageAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
        {
            long chatId = message.Chat.Id;
            string text = message.Text?.Trim() ?? "";

            if (!IsAdmin(chatId))
            {
                await bot.SendTextMessageAsync(chatId, "⛔ У вас нет доступа к управлению HFL Razbloker Enterprise.", cancellationToken: ct);
                return;
            }

            if (_userStates.TryGetValue(chatId, out var state))
            {
                if (state == "WAITING_CUSTOM_DAYS")
                {
                    _userStates.Remove(chatId);
                    if (int.TryParse(text, out int days) && days > 0)
                    {
                        await GenerateAndSendKeyAsync(bot, chatId, days, "Custom", ct);
                    }
                    else
                    {
                        await bot.SendTextMessageAsync(chatId, "❌ Неверное число дней. Введите целое число.", cancellationToken: ct);
                    }
                    return;
                }
                else if (state.StartsWith("WAITING_DOMAIN_"))
                {
                    _userStates.Remove(chatId);
                    await AddDnsRecordAsync(bot, chatId, text, ct);
                    return;
                }
                else if (state.StartsWith("WAITING_3XUI_"))
                {
                    _userStates.Remove(chatId);
                    await SetVlessUriAsync(bot, chatId, text, ct);
                    return;
                }
            }

            if (text == "/start" || text == "/menu")
            {
                await SendMainMenuAsync(bot, chatId, ct);
            }
        }

        private async Task HandleCallbackAsync(ITelegramBotClient bot, CallbackQuery callback, CancellationToken ct)
        {
            long chatId = callback.Message!.Chat.Id;
            int messageId = callback.Message.MessageId;
            string data = callback.Data ?? "";

            await bot.AnswerCallbackQueryAsync(callback.Id, cancellationToken: ct);

            if (!IsAdmin(chatId)) return;

            if (data == "menu_main")
            {
                await EditToMainMenuAsync(bot, chatId, messageId, ct);
            }
            else if (data == "menu_gen_key")
            {
                var kb = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("⚡ 7 дней", "gen_7"), InlineKeyboardButton.WithCallbackData("⚡ 30 дней", "gen_30") },
                    new[] { InlineKeyboardButton.WithCallbackData("⚡ 90 дней", "gen_90"), InlineKeyboardButton.WithCallbackData("⚡ 365 дней", "gen_365") },
                    new[] { InlineKeyboardButton.WithCallbackData("♾️ Бессрочный", "gen_-1"), InlineKeyboardButton.WithCallbackData("✏️ Свой срок", "gen_custom") },
                    new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "menu_main") }
                });

                await bot.EditMessageTextAsync(chatId, messageId, "🔑 <b>Выберите срок действия ключа:</b>", parseMode: ParseMode.Html, replyMarkup: kb, cancellationToken: ct);
            }
            else if (data.StartsWith("gen_"))
            {
                string durationStr = data.Substring(4);
                if (durationStr == "custom")
                {
                    _userStates[chatId] = "WAITING_CUSTOM_DAYS";
                    await bot.SendTextMessageAsync(chatId, "✏️ Введите количество дней для лицензии (например: <code>45</code>):", parseMode: ParseMode.Html, cancellationToken: ct);
                }
                else if (int.TryParse(durationStr, out int days))
                {
                    await GenerateAndSendKeyAsync(bot, chatId, days, $"План {days}д", ct);
                }
            }
            else if (data == "menu_list_keys")
            {
                await ShowKeysListAsync(bot, chatId, messageId, 0, ct);
            }
            else if (data.StartsWith("keys_page_"))
            {
                int page = int.Parse(data.Substring(10));
                await ShowKeysListAsync(bot, chatId, messageId, page, ct);
            }
            else if (data.StartsWith("key_info_"))
            {
                int keyId = int.Parse(data.Substring(9));
                await ShowKeyDetailsAsync(bot, chatId, messageId, keyId, ct);
            }
            else if (data.StartsWith("key_resethwid_"))
            {
                int keyId = int.Parse(data.Substring(14));
                await ResetKeyHwidAsync(bot, chatId, messageId, keyId, ct);
            }
            else if (data.StartsWith("key_toggle_"))
            {
                int keyId = int.Parse(data.Substring(11));
                await ToggleKeyStatusAsync(bot, chatId, messageId, keyId, ct);
            }
            else if (data.StartsWith("menu_dns"))
            {
                await ShowDnsMenuAsync(bot, chatId, messageId, ct);
            }
            else if (data == "dns_add")
            {
                _userStates[chatId] = "WAITING_DOMAIN_ADD";
                await bot.SendTextMessageAsync(chatId, "🌐 Введите запись в формате:\n<code>domain [ip]</code>\n\nПример:\n<code>*.local 127.0.0.1</code>\nили\n<code>panel.internal</code>", parseMode: ParseMode.Html, cancellationToken: ct);
            }
            else if (data == "menu_stats")
            {
                await ShowStatsAsync(bot, chatId, messageId, ct);
            }
            else if (data == "menu_settings")
            {
                await ShowSettingsMenuAsync(bot, chatId, messageId, ct);
            }
            else if (data == "settings_set_3xui")
            {
                _userStates[chatId] = "WAITING_3XUI_INPUT";
                await bot.SendTextMessageAsync(chatId, "🔗 Вставьте VLESS Reality / Shadowsocks строку конфигурации от 3X-UI:", cancellationToken: ct);
            }
        }

        private async Task SendMainMenuAsync(ITelegramBotClient bot, long chatId, CancellationToken ct)
        {
            var kb = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("🔑 Создать ключ", "menu_gen_key"), InlineKeyboardButton.WithCallbackData("📋 Список ключей", "menu_list_keys") },
                new[] { InlineKeyboardButton.WithCallbackData("🌐 DNS & Локальные домены", "menu_dns"), InlineKeyboardButton.WithCallbackData("📊 Статистика", "menu_stats") },
                new[] { InlineKeyboardButton.WithCallbackData("⚙️ Настройки (3X-UI / DoH)", "menu_settings") }
            });

            string welcome = "🛡️ <b>HFL Razbloker Enterprise Center</b>\n\n" +
                             "Добро пожаловать в панель управления unblocker-системой и лицензиями.";

            await bot.SendTextMessageAsync(chatId, welcome, parseMode: ParseMode.Html, replyMarkup: kb, cancellationToken: ct);
        }

        private async Task EditToMainMenuAsync(ITelegramBotClient bot, long chatId, int messageId, CancellationToken ct)
        {
            var kb = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("🔑 Создать ключ", "menu_gen_key"), InlineKeyboardButton.WithCallbackData("📋 Список ключей", "menu_list_keys") },
                new[] { InlineKeyboardButton.WithCallbackData("🌐 DNS & Локальные домены", "menu_dns"), InlineKeyboardButton.WithCallbackData("📊 Статистика", "menu_stats") },
                new[] { InlineKeyboardButton.WithCallbackData("⚙️ Настройки (3X-UI / DoH)", "menu_settings") }
            });

            await bot.EditMessageTextAsync(chatId, messageId, "🛡️ <b>HFL Razbloker Enterprise Center</b>\n\nВыберите раздел для управления:", parseMode: ParseMode.Html, replyMarkup: kb, cancellationToken: ct);
        }

        private async Task GenerateAndSendKeyAsync(ITelegramBotClient bot, long chatId, int days, string note, CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            string key = $"HFL-{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper()}-{Guid.NewGuid().ToString("N").Substring(4, 4).ToUpper()}-{Guid.NewGuid().ToString("N").Substring(8, 4).ToUpper()}";
            
            var lic = new LicenseInfo
            {
                Key = key,
                DurationDays = days,
                Note = note,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            db.Licenses.Add(lic);
            await db.SaveChangesAsync(ct);

            string durationText = days == -1 ? "♾️ Бессрочный (Lifetime)" : $"{days} дней";

            string msg = $"✅ <b>Новый ключ успешно создан!</b>\n\n" +
                         $"🔑 Ключ: <code>{key}</code> (нажмите, чтобы скопировать)\n" +
                         $"⏳ Срок: <b>{durationText}</b>\n" +
                         $"📝 Заметка: <i>{note}</i>\n\n" +
                         $"<i>Ключ активируется автоматически при первом вводе в клиенте и привяжется к HWID ПК.</i>";

            var kb = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("➕ Создать еще", "menu_gen_key"), InlineKeyboardButton.WithCallbackData("🏠 В меню", "menu_main") }
            });

            await bot.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html, replyMarkup: kb, cancellationToken: ct);
        }

        private async Task ShowKeysListAsync(ITelegramBotClient bot, long chatId, int messageId, int page, CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            int pageSize = 6;
            var totalCount = await db.Licenses.CountAsync(ct);
            var keys = await db.Licenses.OrderByDescending(l => l.Id).Skip(page * pageSize).Take(pageSize).ToListAsync(ct);

            var rows = new List<InlineKeyboardButton[]>();

            foreach (var k in keys)
            {
                string statusIcon = !k.IsActive ? "🔴" : (k.ActivatedAt == null ? "🟡" : "🟢");
                string shortKey = k.Key.Length > 12 ? k.Key.Substring(0, 12) + "..." : k.Key;
                string label = $"{statusIcon} {shortKey} ({k.DurationDays}д)";
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData(label, $"key_info_{k.Id}") });
            }

            var navRow = new List<InlineKeyboardButton>();
            if (page > 0)
                navRow.Add(InlineKeyboardButton.WithCallbackData("⬅️", $"keys_page_{page - 1}"));
            if ((page + 1) * pageSize < totalCount)
                navRow.Add(InlineKeyboardButton.WithCallbackData("➡️", $"keys_page_{page + 1}"));

            if (navRow.Count > 0)
                rows.Add(navRow.ToArray());

            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🏠 Главное меню", "menu_main") });

            string text = $"📋 <b>Список лицензий (Всего: {totalCount})</b>\n" +
                          $"🟢 - Активен на ПК | 🟡 - Не активирован | 🔴 - Заблокирован\n\n" +
                          $"<i>Нажмите на ключ для подробной информации и управления:</i>";

            await bot.EditMessageTextAsync(chatId, messageId, text, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(rows), cancellationToken: ct);
        }

        private async Task ShowKeyDetailsAsync(ITelegramBotClient bot, long chatId, int messageId, int keyId, CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var k = await db.Licenses.FirstOrDefaultAsync(l => l.Id == keyId, ct);
            if (k == null)
            {
                await ShowKeysListAsync(bot, chatId, messageId, 0, ct);
                return;
            }

            string statusText = !k.IsActive ? "🔴 Заблокирован" : (k.ActivatedAt == null ? "🟡 Ожидает активации" : "🟢 Активен");
            string activated = k.ActivatedAt?.ToString("dd.MM.yyyy HH:mm") ?? "—";
            string expires = k.ExpiresAt?.ToString("dd.MM.yyyy HH:mm") ?? (k.DurationDays == -1 ? "Бессрочно" : "—");
            string hwid = string.IsNullOrEmpty(k.Hwid) ? "Не привязан" : $"<code>{k.Hwid}</code>";

            string text = $"🔑 <b>Лицензия:</b> <code>{k.Key}</code>\n\n" +
                          $"📊 Статус: <b>{statusText}</b>\n" +
                          $"💻 HWID: {hwid}\n" +
                          $"📅 Создан: <b>{k.CreatedAt:dd.MM.yyyy}</b>\n" +
                          $"⚡ Активирован: <b>{activated}</b>\n" +
                          $"⏳ Истекает: <b>{expires}</b>\n" +
                          $"📱 Версия клиента: <code>{k.AppVersion ?? "—"}</code>";

            var rows = new List<InlineKeyboardButton[]>();

            if (!string.IsNullOrEmpty(k.Hwid))
            {
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🔄 Сбросить HWID привязку", $"key_resethwid_{k.Id}") });
            }

            string toggleText = k.IsActive ? "🚫 Заблокировать ключ" : "✅ Разблокировать ключ";
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(toggleText, $"key_toggle_{k.Id}") });
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ К списку ключей", "menu_list_keys"), InlineKeyboardButton.WithCallbackData("🏠 Меню", "menu_main") });

            await bot.EditMessageTextAsync(chatId, messageId, text, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(rows), cancellationToken: ct);
        }

        private async Task ResetKeyHwidAsync(ITelegramBotClient bot, long chatId, int messageId, int keyId, CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var k = await db.Licenses.FirstOrDefaultAsync(l => l.Id == keyId, ct);
            if (k != null)
            {
                k.Hwid = null;
                await db.SaveChangesAsync(ct);
            }

            await ShowKeyDetailsAsync(bot, chatId, messageId, keyId, ct);
        }

        private async Task ToggleKeyStatusAsync(ITelegramBotClient bot, long chatId, int messageId, int keyId, CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var k = await db.Licenses.FirstOrDefaultAsync(l => l.Id == keyId, ct);
            if (k != null)
            {
                k.IsActive = !k.IsActive;
                await db.SaveChangesAsync(ct);
            }

            await ShowKeyDetailsAsync(bot, chatId, messageId, keyId, ct);
        }

        private async Task ShowDnsMenuAsync(ITelegramBotClient bot, long chatId, int messageId, CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var records = await db.DnsRecords.ToListAsync(ct);
            var settings = await db.Settings.FirstOrDefaultAsync(ct) ?? new ServerConfigEntity();

            var sb = new StringBuilder();
            sb.AppendLine("🌐 <b>DNS & Локальные домены:</b>\n");
            sb.AppendLine($"📍 Серверный IP: <code>{settings.ServerPublicIp}</code>");
            sb.AppendLine($"⚡ Upstream DoH: <code>{settings.DohUpstream}</code>\n");
            sb.AppendLine("<b>Активные внутренние записи:</b>");

            if (records.Count == 0)
            {
                sb.AppendLine("<i>Нет добавленных записей.</i>");
            }
            else
            {
                foreach (var r in records)
                {
                    string target = string.IsNullOrWhiteSpace(r.IpAddress) ? settings.ServerPublicIp : r.IpAddress;
                    sb.AppendLine($"• <code>{r.Domain}</code> ➡️ <code>{target}</code>");
                }
            }

            var kb = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("➕ Добавить домен (.local / .internal)", "dns_add") },
                new[] { InlineKeyboardButton.WithCallbackData("🏠 Главное меню", "menu_main") }
            });

            await bot.EditMessageTextAsync(chatId, messageId, sb.ToString(), parseMode: ParseMode.Html, replyMarkup: kb, cancellationToken: ct);
        }

        private async Task AddDnsRecordAsync(ITelegramBotClient bot, long chatId, string input, CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            string domain = parts[0].Trim();
            string ip = parts.Length > 1 ? parts[1].Trim() : "";

            var existing = await db.DnsRecords.FirstOrDefaultAsync(r => r.Domain == domain, ct);
            if (existing != null)
            {
                existing.IpAddress = ip;
                existing.IsEnabled = true;
            }
            else
            {
                db.DnsRecords.Add(new DnsRecord { Domain = domain, IpAddress = ip, IsEnabled = true });
            }

            await db.SaveChangesAsync(ct);
            await bot.SendTextMessageAsync(chatId, $"✅ Запись для <code>{domain}</code> успешно сохранена!", parseMode: ParseMode.Html, cancellationToken: ct);
            await SendMainMenuAsync(bot, chatId, ct);
        }

        private async Task ShowStatsAsync(ITelegramBotClient bot, long chatId, int messageId, CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var total = await db.Licenses.CountAsync(ct);
            var active = await db.Licenses.CountAsync(l => l.IsActive, ct);
            var bound = await db.Licenses.CountAsync(l => !string.IsNullOrEmpty(l.Hwid), ct);

            string stats = $"📊 <b>Enterprise Статистика HFL:</b>\n\n" +
                           $"🔑 Всего сгенерировано ключей: <b>{total}</b>\n" +
                           $"🟢 Активных лицензий: <b>{active}</b>\n" +
                           $"💻 Привязанных устройств: <b>{bound}</b>\n" +
                           $"🛡️ Защита DoH / Zapret: <b>Активна (100% Uptime)</b>";

            var kb = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("🔄 Обновить", "menu_stats"), InlineKeyboardButton.WithCallbackData("🏠 Меню", "menu_main") }
            });

            await bot.EditMessageTextAsync(chatId, messageId, stats, parseMode: ParseMode.Html, replyMarkup: kb, cancellationToken: ct);
        }

        private async Task ShowSettingsMenuAsync(ITelegramBotClient bot, long chatId, int messageId, CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var s = await db.Settings.FirstOrDefaultAsync(ct) ?? new ServerConfigEntity();

            string vlessInfo = string.IsNullOrWhiteSpace(s.XuiVlessUri) ? "<i>Не настроен</i>" : "<code>Настроен (VLESS Reality)</code>";

            string text = $"⚙️ <b>Настройки сервера HFL:</b>\n\n" +
                          $"🌐 3X-UI VLESS: {vlessInfo}\n" +
                          $"🔒 Upstream DoH: <code>{s.DohUpstream}</code>\n" +
                          $"📍 IP сервера: <code>{s.ServerPublicIp}</code>";

            var kb = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("🔗 Указать VLESS Reality от 3X-UI", "settings_set_3xui") },
                new[] { InlineKeyboardButton.WithCallbackData("🏠 Главное меню", "menu_main") }
            });

            await bot.EditMessageTextAsync(chatId, messageId, text, parseMode: ParseMode.Html, replyMarkup: kb, cancellationToken: ct);
        }

        private async Task SetVlessUriAsync(ITelegramBotClient bot, long chatId, string uri, CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var s = await db.Settings.FirstOrDefaultAsync(ct) ?? new ServerConfigEntity();

            s.XuiVlessUri = uri.Trim();
            if (s.Id == 0) db.Settings.Add(s);
            await db.SaveChangesAsync(ct);

            await bot.SendTextMessageAsync(chatId, "✅ VLESS Reality конфигурация успешно обновлена!", cancellationToken: ct);
            await SendMainMenuAsync(bot, chatId, ct);
        }

        private bool IsAdmin(long chatId)
        {
            string? adminIdStr = _config["ADMIN_TELEGRAM_ID"] ?? Environment.GetEnvironmentVariable("ADMIN_TELEGRAM_ID");
            if (string.IsNullOrWhiteSpace(adminIdStr)) return true; // Default allow if not restricted yet
            return adminIdStr.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => id.Trim() == chatId.ToString());
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken ct)
        {
            _logger.LogError(exception, "Telegram Polling Error");
            return Task.CompletedTask;
        }
    }
}
