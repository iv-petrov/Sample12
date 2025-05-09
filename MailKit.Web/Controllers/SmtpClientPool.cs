using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Collections.Concurrent;
using System.Timers;
using Timer = System.Timers.Timer;

namespace MailKit.Web.Controllers
{
    /// <summary>
    ///     Пул подключений к SMTP серверу
    /// </summary>
    public sealed class SmtpClientPool : IDisposable
    {
        private readonly AutoResetEvent _wait = new AutoResetEvent(false);
        private readonly Timer _timer = new Timer(TimeSpan.FromSeconds(10));

        private readonly ConcurrentBag<SmtpClient> _clients = new ConcurrentBag<SmtpClient>();

        public SmtpClientPool(int connectionsCount = 1)
        {
            // проверим параметр на корректность
            int n = connectionsCount;
            if (n <= 0) {
                n = 1;
            }
            // создаём пул клиентов
            _clients = new ConcurrentBag<SmtpClient>();
            for (int i = 0; i < n; i++)
                _clients.Add(new SmtpClient());
            // открываем турникет
            _wait.Set();
            // запускаем таймер для поддержания соединений
            _timer.Elapsed += SendNoOp;
            _timer.AutoReset = true;
            _timer.Start();
        }

        /// <summary>
        ///     Отправляет письмо
        /// </summary>
        /// <param name="message">Сообщение с письмом</param>
        /// <param name="token">Токен отмены операции</param>
        public async Task SendAsync(MimeMessage message, CancellationToken token)
        {
            SmtpClient? client;
            // выдёргиваем клиента из пула или ждём у турникета, если свободных нет
            while (_clients.TryTake(out client) is false)
                _wait.WaitOne();

            try
            {
                // если соединение потеряно, то восстанавливаем
                if (client.IsConnected is false)
                    await client.ConnectAsync(token);
                // отправляем письмо
                await client.SendAsync(message, token);
            }
            finally
            {
                // добавляем клиента обратно в пул
                _clients.Add(client);
                // освобождаем турникет
                _wait.Set();
            }
        }

        /// <summary>
        ///     Отправляет команду NoOp для поддержания соединения открытым
        /// </summary>
        private async void SendNoOp(object? sender, ElapsedEventArgs e)
        {
            var returnClients = new List<SmtpClient>();
            // проверяем есть ли свободные клиенты
            if (_clients.TryPeek(out _) is false)
                return; // свободных клиентов нет - уходим

            try
            {
                // пока есть свободные клиенты, выдёргиваем их в отдельный список
                while (_clients.TryTake(out var client))
                    returnClients.Add(client);
                // проверяем всех надёрганых клиентов
                await Task.WhenAll(returnClients.Select(async c => 
                {
                    if (c.IsConnected) // если соединение открыто, то посылаем NoOp для его поддержания
                        await c.NoOpAsync();
                }));
            }
            finally
            {
                // напоследок запихиваем всех обработанных обратно в пул
                foreach (var returnClient in returnClients)
                    _clients.Add(returnClient);
                // и открываем турникет, если что-то было обработано
                if (returnClients.Count > 0)
                    _wait.Set();
            }
        }

        public void Dispose()
        {
            _timer.Elapsed -= SendNoOp;
            _timer.Dispose();

            foreach (var client in _clients)
                client.Dispose();

            _wait.Dispose();
        }
    }
}

public static class SmtpClientExtensions
{
    private const string _server = "10.4.107.10";
    private const string _login = @"innoca\IVPetrov";
    private const string _password = "HM0rVuto64";
    private const int _port = 587;

    /// <summary>
    ///     Выполняет подключение и аутентификацию к SMPT серверу
    /// </summary>
    public static async Task ConnectAsync(this SmtpClient client, CancellationToken cancellationToken)
    {
        client.ServerCertificateValidationCallback = (s, c, h, e) => true;
        await client.ConnectAsync(_server, _port, SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(_login, _password, cancellationToken);
    }
}
