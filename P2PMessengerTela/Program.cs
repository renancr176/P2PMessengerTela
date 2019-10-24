using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace P2PMessengerTela
{
    class Program
    {
        private static string IpServidor;
        private static int PortaServidor;

        public static Cliente Cliente { get; private set; }

        static void Main(string[] args)
        {
            Console.WriteLine("P2PMessenger");

            string porta;
            do
            {

                Console.WriteLine("Informe o IP do servidor.");

                IpServidor = Console.ReadLine();

                if (!ValidaIp(IpServidor))
                {
                    Console.Clear();
                    Console.WriteLine("P2PMessenger");
                }

            } while (!ValidaIp(IpServidor));

            do
            {
                Console.WriteLine("Informe a porta do servidor.");

                porta = Console.ReadLine();

                if (!int.TryParse(porta, out PortaServidor))
                {
                    Console.Clear();
                    Console.WriteLine("P2PMessenger");
                }

            } while (!int.TryParse(porta, out PortaServidor));


            Cliente = new Cliente(IPAddress.Parse(IpServidor), PortaServidor);

            Cliente.Iniciar().Wait();
        }

        private static bool ValidaIp(string ip)
        {
            var regex = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");

            return regex.IsMatch(ip);
        }
    }

    public class Cliente
    {
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken => _cancellationTokenSource.Token;

        public bool Rodando { get; private set; }

        public IPAddress IpServidor { get; private set; }
        public int PortaServidor { get; private set; }

        public Cliente(IPAddress ipServidor, int portaServidor)
        {
            IpServidor = ipServidor;
            PortaServidor = portaServidor;
        }

        public async Task Iniciar()
        {
            if (!Rodando)
            {
                Rodando = true;
                _cancellationTokenSource = new CancellationTokenSource();

                var client = new TcpClient();

                while (!_cancellationToken.IsCancellationRequested)
                {
                    if (client.Connected)
                    {
                        Console.Clear();
                        Console.WriteLine("P2PMessenger - Tela Chat");

                        NetworkStream ns = client.GetStream();

                        while (!_cancellationToken.IsCancellationRequested && client.Connected)
                        {
                            byte[] msg = new byte[1024];
                            await ns.ReadAsync(msg, 0, msg.Length);
                            var mensagem = Encoding.Default.GetString(msg).Trim();

                            if (!string.IsNullOrEmpty(mensagem))
                            {
                                Console.WriteLine(mensagem);
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            client.Connect(IpServidor, PortaServidor);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                }
            }
        }
    }
}
