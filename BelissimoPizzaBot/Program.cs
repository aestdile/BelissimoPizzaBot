using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace OshxonaBot
{
    class Program
    {
        private static TelegramBotClient botClient;
        private static List<User> users = new List<User>();
        private static string jsonFilePath = "users.json";

        static async Task Main(string[] args)
        {
            string botToken = "7518052919:AAHCzFxnzmFVuUmhNIdfjc-i2BL7M05s1fo";
            botClient = new TelegramBotClient(botToken);

            if (File.Exists(jsonFilePath))
            {
                string jsonData = File.ReadAllText(jsonFilePath);
                users = JsonConvert.DeserializeObject<List<User>>(jsonData);
            }

            var me = await botClient.GetMe();
            Console.WriteLine($"Bot ishlashni boshladi: @{me.Username}");

            using var cts = new CancellationTokenSource();

            botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandleErrorAsync,
                cancellationToken: cts.Token
            );

            Console.WriteLine("Botni to'xtatish uchun istalgan tugmani bosing...");
            Console.ReadKey();

            cts.Cancel();
        }

        static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Message is Message message)
                {
                    var chatId = message.Chat.Id;
                    Console.WriteLine($"Qabul qilingan xabar: {message.Text} chatId: {chatId}");

                    var user = users.Find(u => u.Id == chatId.ToString());
                    if (user == null)
                    {
                        user = new User
                        {
                            Id = chatId.ToString(),
                            Username = message.From.Username ?? message.From.FirstName
                        };
                        users.Add(user);
                    }

                    if (message.Text == "/start")
                    {
                        await SendMainMenuAsync(chatId, cancellationToken);
                    }
                    else if (message.Text == "🍽 Menyular")
                    {
                        await SendCategoriesMenuAsync(chatId, cancellationToken);
                    }
                    else if (message.Text == "🛒 Savat")
                    {
                        await SendCartInfoAsync(chatId, cancellationToken);
                    }
                    else if (message.Text == "📞 Aloqa")
                    {
                        await botClient.SendMessage(
                            chatId: chatId,
                            text: "Aloqa uchun telefon: +998 77 267 27 74",
                            cancellationToken: cancellationToken);
                    }
                    else if (message.Text == "⬅️ Ortga")
                    {
                        await SendMainMenuAsync(chatId, cancellationToken);
                    }
                    else if (user.AwaitingPhone)
                    {
                        user.PhoneNumber = message.Text;
                        user.AwaitingPhone = false;
                        user.AwaitingAddress = true;

                        await botClient.SendMessage(
                            chatId: chatId,
                            text: "Manzilni kiriting:",
                            cancellationToken: cancellationToken);
                    }
                    else if (user.AwaitingAddress)
                    {
                        user.DeliveryAddress = message.Text;
                        user.AwaitingAddress = false;

                        await CompleteOrderAsync(chatId, user, cancellationToken);
                    }
                    else if (message.Text.StartsWith("Vegeteriyanlar") ||
                             message.Text.StartsWith("Go'shxo'rlar") ||
                             message.Text.StartsWith("FastFood") ||
                             message.Text.StartsWith("GreenFood") ||
                             message.Text.StartsWith("Salqin ichimliklar") ||
                             message.Text.StartsWith("Desert"))
                    {
                        await SendCategoryItemsAsync(chatId, message.Text, cancellationToken);
                    }
                    else if (int.TryParse(message.Text.Split(' ')[0], out int _))
                    {
                        await AddToCartAsync(chatId, message.Text, user, cancellationToken);
                    }
                    else if (message.Text == "Buyurtma berish")
                    {
                        if (user.Cart.Count > 0)
                        {
                            user.AwaitingPhone = true;
                            await botClient.SendMessage(
                                chatId: chatId,
                                text: "Telefon raqamingizni kiriting:",
                                cancellationToken: cancellationToken);
                        }
                        else
                        {
                            await botClient.SendMessage(
                                chatId: chatId,
                                text: "Savatingiz bo'sh! Avval taomlar tanlang.",
                                cancellationToken: cancellationToken);
                        }
                    }
                    else if (message.Text == "Savatni tozalash")
                    {
                        user.Cart.Clear();
                        await botClient.SendMessage(
                            chatId: chatId,
                            text: "Savat tozalandi!",
                            cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await SendMainMenuAsync(chatId, cancellationToken);
                    }

                    SaveUsersData();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Xato yuz berdi: {exception.Message}");
            }
        }

        static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API xatosi:\n{apiRequestException.ErrorCode}\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        static async Task SendMainMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var replyKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("🍽 Menyular"), new KeyboardButton("🛒 Savat") },
                new[] { new KeyboardButton("📞 Aloqa") }
            })
            {
                ResizeKeyboard = true
            };

            await botClient.SendMessage(
                chatId: chatId,
                text: "Oshxona botga xush kelibsiz! Kerakli menyuni tanlang:",
                replyMarkup: replyKeyboard,
                cancellationToken: cancellationToken);
        }

        static async Task SendCategoriesMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var categoriesKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("Vegeteriyanlar"), new KeyboardButton("Go'shxo'rlar") },
                new[] { new KeyboardButton("FastFood"), new KeyboardButton("GreenFood") },
                new[] { new KeyboardButton("Salqin ichimliklar"), new KeyboardButton("Desert") },
                new[] { new KeyboardButton("⬅️ Ortga") }
            })
            {
                ResizeKeyboard = true
            };

            await botClient.SendMessage(
                chatId: chatId,
                text: "Kategoriyani tanlang:",
                replyMarkup: categoriesKeyboard,
                cancellationToken: cancellationToken);
        }

        static async Task SendCategoryItemsAsync(long chatId, string category, CancellationToken cancellationToken)
        {
            var items = GetCategoryItems(category);

            var backButton = new[] { new KeyboardButton("⬅️ Ortga") };
            var itemButtons = items.Select(item => new[] { new KeyboardButton($"{item.Id}. {item.Name} - {item.Price} so'm") }).ToArray();

            var allButtons = new List<KeyboardButton[]>(itemButtons);
            allButtons.Add(backButton);

            var itemsKeyboard = new ReplyKeyboardMarkup(allButtons) { ResizeKeyboard = true };

            await botClient.SendMessage(
                chatId: chatId,
                text: $"{category} kategoriyasidagi taomlar:",
                replyMarkup: itemsKeyboard,
                cancellationToken: cancellationToken);
        }

        static async Task AddToCartAsync(long chatId, string itemText, User user, CancellationToken cancellationToken)
        {
            try
            {
                var parts = itemText.Split('.');
                if (parts.Length > 1)
                {
                    int itemId = int.Parse(parts[0]);
                    string itemInfo = parts[1].Trim();

                    string[] itemParts = itemInfo.Split('-');
                    string itemName = itemParts[0].Trim();
                    string priceText = itemParts[1].Trim();
                    int price = int.Parse(priceText.Split(' ')[0]);

                    var cartItem = new CartItem
                    {
                        Id = itemId,
                        Name = itemName,
                        Price = price,
                        Quantity = 1
                    };

                    var existingItem = user.Cart.Find(i => i.Id == itemId);
                    if (existingItem != null)
                    {
                        existingItem.Quantity++;
                    }
                    else
                    {
                        user.Cart.Add(cartItem);
                    }

                    await botClient.SendMessage(
                        chatId: chatId,
                        text: $"{itemName} savatga qo'shildi! Davom etish uchun kategoriyadan tanlashingiz yoki savatga o'tishingiz mumkin.",
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Savatga qo'shishda xato: {ex.Message}");
            }
        }

        static async Task SendCartInfoAsync(long chatId, CancellationToken cancellationToken)
        {
            var user = users.Find(u => u.Id == chatId.ToString());

            if (user.Cart.Count == 0)
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Sizning savatingiz bo'sh!",
                    cancellationToken: cancellationToken);
                return;
            }

            int totalPrice = 0;
            string cartText = "🛒 SAVAT:\n\n";

            foreach (var item in user.Cart)
            {
                int itemTotal = item.Price * item.Quantity;
                cartText += $"{item.Name} x{item.Quantity} = {itemTotal} so'm\n";
                totalPrice += itemTotal;
            }

            cartText += $"\nUmumiy narx: {totalPrice} so'm";

            var cartKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("Buyurtma berish") },
                new[] { new KeyboardButton("Savatni tozalash") },
                new[] { new KeyboardButton("⬅️ Ortga") }
            })
            {
                ResizeKeyboard = true
            };

            await botClient.SendMessage(
                chatId: chatId,
                text: cartText,
                replyMarkup: cartKeyboard,
                cancellationToken: cancellationToken);
        }

        static async Task CompleteOrderAsync(long chatId, User user, CancellationToken cancellationToken)
        {
            int totalPrice = 0;
            foreach (var item in user.Cart)
            {
                totalPrice += item.Price * item.Quantity;
            }

            user.Price = totalPrice;
            user.OrderCreatedTime = DateTime.Now;

            string orderDetails = $"Buyurtmangiz qabul qilindi!\n\n" +
                                 $"Telefon: {user.PhoneNumber}\n" +
                                 $"Manzil: {user.DeliveryAddress}\n" +
                                 $"Buyurtma vaqti: {user.OrderCreatedTime}\n\n" +
                                 $"Buyurtma tafsilotlari:\n";

            foreach (var item in user.Cart)
            {
                orderDetails += $"- {item.Name} x{item.Quantity} = {item.Price * item.Quantity} so'm\n";
            }

            orderDetails += $"\nUmumiy narx: {totalPrice} so'm";

            string adminId = "6421409546"; 
            if (!string.IsNullOrEmpty(adminId))
            {
                string adminMessage = $"Yangi buyurtma!\n\n" +
                                     $"Foydalanuvchi: {user.Username}\n" +
                                     $"Telefon: {user.PhoneNumber}\n" +
                                     $"Manzil: {user.DeliveryAddress}\n" +
                                     $"Buyurtma vaqti: {user.OrderCreatedTime}\n\n" +
                                     $"Taomlar:\n";

                foreach (var item in user.Cart)
                {
                    adminMessage += $"- {item.Name} x{item.Quantity} = {item.Price * item.Quantity} so'm\n";
                }

                adminMessage += $"\nUmumiy narx: {totalPrice} so'm";

                await botClient.SendMessage(
                    chatId: long.Parse(adminId),
                    text: adminMessage,
                    cancellationToken: cancellationToken);
            }

            await botClient.SendMessage(
                chatId: chatId,
                text: orderDetails,
                cancellationToken: cancellationToken);

            user.Cart.Clear();
            await SendMainMenuAsync(chatId, cancellationToken);
        }

        static List<MenuItem> GetCategoryItems(string category)
        {
            List<MenuItem> items = new List<MenuItem>();

            switch (category)
            {
                case "Vegeteriyanlar":
                    items.Add(new MenuItem { Id = 1, Name = "Olivye salat", Price = 15000 });
                    items.Add(new MenuItem { Id = 2, Name = "Achik-chuchuk", Price = 12000 });
                    items.Add(new MenuItem { Id = 3, Name = "Vinegret", Price = 14000 });
                    break;
                case "Go'shxo'rlar":
                    items.Add(new MenuItem { Id = 4, Name = "Osh", Price = 35000 });
                    items.Add(new MenuItem { Id = 5, Name = "Mastava", Price = 28000 });
                    items.Add(new MenuItem { Id = 6, Name = "Kabob", Price = 10000 });
                    break;
                case "FastFood":
                    items.Add(new MenuItem { Id = 7, Name = "Lavash", Price = 30000 });
                    items.Add(new MenuItem { Id = 8, Name = "Burger", Price = 28000 });
                    items.Add(new MenuItem { Id = 9, Name = "Hot-dog", Price = 15000 });
                    break;
                case "GreenFood":
                    items.Add(new MenuItem { Id = 10, Name = "Cezar salat", Price = 25000 });
                    items.Add(new MenuItem { Id = 11, Name = "Grechkali salat", Price = 22000 });
                    break;
                case "Salqin ichimliklar":
                    items.Add(new MenuItem { Id = 12, Name = "Coca-Cola", Price = 11000 });
                    items.Add(new MenuItem { Id = 13, Name = "Fanta", Price = 11000 });
                    items.Add(new MenuItem { Id = 14, Name = "Sprite", Price = 11000 });
                    break;
                case "Desert":
                    items.Add(new MenuItem { Id = 15, Name = "Shokolad tort", Price = 15000 });
                    items.Add(new MenuItem { Id = 16, Name = "Meva korzinasi", Price = 30000 });
                    break;
            }

            return items;
        }

        static void SaveUsersData()
        {
            try
            {
                string jsonData = JsonConvert.SerializeObject(users);
                File.WriteAllText(jsonFilePath, jsonData);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ma'lumotlarni saqlashda xatolik: " + ex.Message);
            }
        }
    }

    public class User
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string PhoneNumber { get; set; }
        public string DeliveryAddress { get; set; }
        public int Price { get; set; }
        public DateTime OrderCreatedTime { get; set; }
        public List<CartItem> Cart { get; set; } = new List<CartItem>();
        public bool AwaitingPhone { get; set; }
        public bool AwaitingAddress { get; set; }
        public string FileUrl { get; set; } = "chekini manzili";
    }

    public class CartItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Price { get; set; }
        public int Quantity { get; set; }
    }

    public class MenuItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Price { get; set; }
    }
}