using System.Net.Sockets;
using System.Text;

namespace Client;

internal class Program
{
    private const string ServerHost = "127.0.0.1";
    private const int ServerPort = 8888;

    static async Task Main(string[] args)
    {
        // Настройка кодировки UTF-8 для корректного отображения кириллицы
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        Console.WriteLine("Клиентское приложение");
        Console.WriteLine("Подключение к серверу...");

        var client = new TcpClient();

        try
        {
            // Подключаемся к серверу по IP-адресу и порту
            await client.ConnectAsync(ServerHost, ServerPort);
            Console.WriteLine("Подключение установлено!");
            Console.WriteLine();
            Console.WriteLine("Введите сообщение или команду:");

            using (client)
            using (NetworkStream stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
            {
                while (true)
                {
                    Console.Write("> ");
                    string? input = Console.ReadLine();

                    // Если ввод завершен (Ctrl+Z / конец потока)
                    if (input == null)
                    {
                        break;
                    }

                    input = input.Trim(' ', '\t', '\r', '\n', '\uFEFF');
                    if (string.IsNullOrEmpty(input))
                    {
                        continue;
                    }

                    // Проверка на команду завершения работы
                    if (input.Equals("!выход", StringComparison.OrdinalIgnoreCase))
                    {
                        // Отправляем команду завершения на сервер для корректного закрытия
                        await writer.WriteLineAsync("!выход");
                        Console.WriteLine("Отключение от сервера...");
                        break;
                    }

                    // Отправка введенного сообщения серверу
                    await writer.WriteLineAsync(input);

                    // Получение ответа от сервера
                    string? response = await reader.ReadLineAsync();
                    if (response == null)
                    {
                        Console.WriteLine("Сервер разорвал соединение.");
                        break;
                    }

                    // Вывод ответа сервера в консоль
                    Console.WriteLine($"Ответ сервера: {response}");
                }
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"[ОШИБКА ПОДКЛЮЧЕНИЯ]: Не удалось связаться с сервером ({ServerHost}:{ServerPort}).");
            Console.WriteLine($"Подробности: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ОШИБКА]: {ex.Message}");
        }
        finally
        {
            client.Close();
            Console.WriteLine("Работа программы завершена.");
        }
    }
}
