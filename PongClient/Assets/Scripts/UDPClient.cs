using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using UnityEngine;
using TMPro;

public class UDPClient : MonoBehaviour
{
    public string ipServidor = "127.0.0.1";
    public int porta = 7777;

    public Transform player1;
    public Transform player2;
    public Transform bola;

    public TextMeshProUGUI textoPlacar1;
    public TextMeshProUGUI textoPlacar2;

    private UdpClient cliente;
    private Thread thread;
    private bool rodando = false;

    private ConcurrentQueue<string> mensagensRecebidas =
        new ConcurrentQueue<string>();

    void Start()
    {
        IniciarCliente();
    }

    void Update()
    {
        if (!rodando)
            return;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            EnviarMensagem("INPUT|UP");
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            EnviarMensagem("INPUT|DOWN");
        else
            EnviarMensagem("INPUT|NONE");

        while (mensagensRecebidas.TryDequeue(out string mensagem))
        {
            if (mensagem.StartsWith("STATE|"))
            {
                AplicarEstado(mensagem);
            }
            else
            {
                Debug.Log("Recebido do servidor: " + mensagem);
            }
        }
    }

    void IniciarCliente()
    {
        try
        {
            cliente = new UdpClient();
            rodando = true;

            thread = new Thread(ReceberDados);
            thread.IsBackground = true;
            thread.Start();

            EnviarMensagem("HELLO");

            Debug.Log("Cliente UDP iniciado.");
        }
        catch (Exception e)
        {
            Debug.LogError("Erro ao iniciar cliente: " + e.Message);
        }
    }

    void EnviarMensagem(string mensagem)
    {
        try
        {
            if (cliente == null)
                return;

            byte[] dados = Encoding.UTF8.GetBytes(mensagem);
            cliente.Send(dados, dados.Length, ipServidor, porta);

            if (mensagem != "INPUT|NONE")
                Debug.Log("Enviado: " + mensagem);
        }
        catch (Exception e)
        {
            if (rodando)
                Debug.LogError("Erro ao enviar: " + e.Message);
        }
    }

    void ReceberDados()
    {
        IPEndPoint ponto = new IPEndPoint(IPAddress.Any, 0);

        while (rodando)
        {
            try
            {
                byte[] dados = cliente.Receive(ref ponto);
                string mensagem = Encoding.UTF8.GetString(dados);

                mensagensRecebidas.Enqueue(mensagem);
            }
            catch (SocketException e)
            {
                if (!rodando ||
                    e.ErrorCode == 10004 ||
                    e.ErrorCode == 10022 ||
                    e.ErrorCode == 10053)
                {
                    break;
                }

                Debug.LogError("Erro ao receber: " + e.Message);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception e)
            {
                if (rodando)
                    Debug.LogError("Erro ao receber: " + e.Message);
            }
        }
    }

    void AplicarEstado(string mensagem)
    {
        try
        {
            string[] partes = mensagem.Split('|');

            if (partes.Length < 7)
                return;

            float p1Y = float.Parse(
                partes[1],
                System.Globalization.CultureInfo.InvariantCulture
            );

            float p2Y = float.Parse(
                partes[2],
                System.Globalization.CultureInfo.InvariantCulture
            );

            float bolaX = float.Parse(
                partes[3],
                System.Globalization.CultureInfo.InvariantCulture
            );

            float bolaY = float.Parse(
                partes[4],
                System.Globalization.CultureInfo.InvariantCulture
            );

            int placar1 = int.Parse(partes[5]);
            int placar2 = int.Parse(partes[6]);

            if (textoPlacar1 != null)
            {
                textoPlacar1.text = placar1.ToString();
            }

            if (textoPlacar2 != null)
            {
                textoPlacar2.text = placar2.ToString();
            }

            if (player1 != null)
            {
                Vector3 posicao = player1.position;
                posicao.y = p1Y;
                player1.position = posicao;
            }

            if (player2 != null)
            {
                Vector3 posicao = player2.position;
                posicao.y = p2Y;
                player2.position = posicao;
            }

            if (bola != null)
            {
                bola.position = new Vector3(
                    bolaX,
                    bolaY,
                    bola.position.z
                );
            }

            Debug.Log(
                "Estado aplicado. Placar: " +
                placar1 + " x " + placar2
            );
        }
        catch (Exception e)
        {
            Debug.LogError("Erro ao aplicar estado: " + e.Message);
        }
    }

    void OnApplicationQuit()
    {
        rodando = false;

        if (cliente != null)
        {
            cliente.Close();
            cliente = null;
        }

        if (thread != null && thread.IsAlive)
        {
            thread.Join(100);
        }
    }
}
