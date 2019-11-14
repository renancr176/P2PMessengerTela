using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace P2PMessengerTela
{
    class Program
    {
        private static string IpServidor;
        private static int PortaServidor;

        public static Cliente Cliente { get; private set; }

        static void Main(string[] args)
        {
            Console.WriteLine("P2PMessengerTela");

            string porta;
            do
            {

                Console.WriteLine("Informe o IP do servidor.");

                IpServidor = Console.ReadLine();

                if (!ValidaIp(IpServidor))
                {
                    Console.Clear();
                    Console.WriteLine("P2PMessengerTela");
                }

            } while (!ValidaIp(IpServidor));

            do
            {
                Console.WriteLine("Informe a porta do servidor.");

                porta = Console.ReadLine();

                if (!int.TryParse(porta, out PortaServidor))
                {
                    Console.Clear();
                    Console.WriteLine("P2PMessengerTela");
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
                        Console.WriteLine("P2PMessengerTela");

                        NetworkStream ns = client.GetStream();

                        while (!_cancellationToken.IsCancellationRequested && client.Connected)
                        {
                            byte[] msg = new byte[999999];
                            await ns.ReadAsync(msg, 0, msg.Length);

                            var jsonStr = Encoding.Default.GetString(msg);

                            var mensagem = JsonConvert.DeserializeObject<Mensagem>(jsonStr);

                            if (mensagem != null)
                            {
                                switch (mensagem.TipoMensagem)
                                {
                                    case TipoMensagemEnum.Mensagem:
                                        Console.WriteLine(mensagem.Texto);
                                        break;
                                    case TipoMensagemEnum.Arquivo:
                                        if (mensagem.Arquivo != null && mensagem.Arquivo.Valido())
                                        {
                                            Console.WriteLine($"Arquivo {mensagem.Arquivo.Nome} recebido.");

                                            try
                                            {
                                                var caminhoBase =
                                                    $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\\Arquivos Recebidos";

                                                Directory.CreateDirectory(caminhoBase);

                                                var caminho = caminhoBase + $"\\{mensagem.Arquivo.Nome}";

                                                using (var imageFile = new FileStream(caminho, FileMode.Create))
                                                {
                                                    imageFile.Write(mensagem.Arquivo.GetBytes(), 0,
                                                        mensagem.Arquivo.GetBytes().Length);
                                                    imageFile.Flush();
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                            }
                                        }

                                        break;
                                }
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

    public class Mensagem
    {
        public TipoMensagemEnum TipoMensagem { get; set; }
        public string Texto { get; set; }
        public Arquivo Arquivo { get; set; }
    }

    public class Arquivo
    {
        public string Nome { get; set; }
        public string ArquivoTexto { get; set; }

        public bool Valido()
        {
            return (!string.IsNullOrEmpty(Nome?.Trim()) && !string.IsNullOrEmpty(ArquivoTexto?.Trim()));
        }

        public byte[] GetBytes()
        {
            return Convert.FromBase64String(ArquivoTexto);
        }
    }

    public enum TipoMensagemEnum
    {
        Mensagem,
        Arquivo
    }
}
