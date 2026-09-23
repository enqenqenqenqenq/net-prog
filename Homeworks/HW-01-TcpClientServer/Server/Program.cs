using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server;

internal class Program
{
    // Порт для прослушивания входящих подключений
    private const int Port = 8888;

    // Предопределенный список слов для команды !слово
    private static readonly string[] WordsList =
    [
        "компьютер",
        "программирование",
        "сеть",
        "сокет",
        "сервер",
        "клиент",
        "протокол",
        "пакет",
        "маршрутизатор",
        "соединение"
    ];

    private static readonly Random Randomizer = new();

    static async Task Main(string[] args)
    {
        // Настройка кодировки UTF-8 для корректного отображения кириллицы
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        // Создаем TCP-слушатель на всех доступных интерфейсах (0.0.0.0 / 127.0.0.1)
        var listener = new TcpListener(IPAddress.Any, Port);

        try
        {
            listener.Start();
            Console.WriteLine("Сервер запущен.");
            Console.WriteLine($"[СЕРВЕР ЗАПУЩЕН НА ПОРТУ {Port}]");
            Console.WriteLine("Ожидание подключения...");

            // Бесконечный цикл ожидания клиентов
            while (true)
            {
                // Ожидаем подключения очередного клиента
                TcpClient client = await listener.AcceptTcpClientAsync();

                // Обрабатываем каждого клиента в отдельной асинхронной задаче
                _ = HandleClientAsync(client);
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"[ОШИБКА СОКЕТА]: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[КРИТИЧЕСКАЯ ОШИБКА]: {ex.Message}");
        }
        finally
        {
            listener.Stop();
            Console.WriteLine("Сервер остановлен.");
        }
    }

    private static async Task HandleClientAsync(TcpClient client)
    {
        string clientEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "127.0.0.1";
        Console.WriteLine($"Клиент подключён: {clientEndPoint}");
        Console.WriteLine($"[ПОДКЛЮЧЕН КЛИЕНТ {clientEndPoint}]");

        try
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
            {
                string? message;

                // Читаем строки из сетевого потока до тех пор, пока клиент передает данные
                while ((message = await reader.ReadLineAsync()) != null)
                {
                    message = message.Trim(' ', '\t', '\r', '\n', '\uFEFF');
                    if (string.IsNullOrEmpty(message))
                    {
                        continue;
                    }

                    Console.WriteLine($"Получено: {message}");

                    // Обработка команды завершения работы клиента
                    if (message.Equals("!выход", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"Клиент {clientEndPoint} запросил отключение (!выход).");
                        break;
                    }

                    // Проверяем, является ли сообщение специальной командой
                    if (message.StartsWith('!'))
                    {
                        string response = ProcessCommand(message);
                        await writer.WriteLineAsync(response);
                        Console.WriteLine($"Отправлено: {response}");
                    }
                    else
                    {
                        // Обычное сообщение: сервер возвращает его клиенту без изменений (эхо)
                        await writer.WriteLineAsync(message);
                        Console.WriteLine($"Отправлено: {message}");
                    }
                }
            }
        }
        catch (IOException ex) when (ex.InnerException is SocketException)
        {
            Console.WriteLine($"[РАЗРЫВ СОЕДИНЕНИЯ]: Клиент {clientEndPoint} неожиданно закрыл связь.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ОШИБКА ОБРАБОТКИ]: {ex.Message}");
        }
        finally
        {
            Console.WriteLine($"Клиент отключён: {clientEndPoint}");
            Console.WriteLine("Ожидание подключения...");
        }
    }

    private static string ProcessCommand(string command)
    {
        // Приводим к нижнему регистру для регистронезависимого сравнения
        string lowerCommand = command.ToLowerInvariant();

        switch (lowerCommand)
        {
            case "!время":
                Console.WriteLine("Выполнена команда: !время");
                return $"Текущее время: {DateTime.Now:HH:mm:ss}";

            case "!дата":
                Console.WriteLine("Выполнена команда: !дата");
                return $"Сегодня: {DateTime.Now:dd.MM.yyyy}";

            case "!слово":
                string randomWord = WordsList[Randomizer.Next(WordsList.Length)];
                Console.WriteLine("Выполнена команда: !слово");
                return $"Случайное слово: {randomWord}";

            default:
                Console.WriteLine($"Неизвестная команда: {command}");
                return $"Неизвестная команда: {command}";
        }
    }
}
