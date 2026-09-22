
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.Globalization;
using UnityEngine;
using TMPro;

public class UDPClient : MonoBehaviour
{
    [Header("Conexão UDP")]
    public string ipServidor = "127.0.0.1";
    public int porta = 7777;

    [Header("Objetos do jogo")]
    public Transform player1;
    public Transform player2;
    public Transform bola;

    [Header("Placar visual")]
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

        // Placar inicial
        AtualizarPlacarVisual(0, 0);
    }

    void Update()
    {
        if (!rodando)
            return;

        EnviarInput();

        while (mensagensRecebidas.TryDequeue(out string mensagem))
        {
            if (mensagem.StartsWith("STATE|"))
            {
                AplicarEstado(mensagem);
            }
            else
            {
                Debug.Log(
                    "Recebido do servidor: " + mensagem
                );
            }
        }
    }

    // ENVIO DE INPUT

    void EnviarInput()
    {
        if (Input.GetKey(KeyCode.W) ||
            Input.GetKey(KeyCode.UpArrow))
        {
            EnviarMensagem("INPUT|UP");
        }
        else if (Input.GetKey(KeyCode.S) ||
                 Input.GetKey(KeyCode.DownArrow))
        {
            EnviarMensagem("INPUT|DOWN");
        }
        else
        {
            EnviarMensagem("INPUT|NONE");
        }
    }

    // INICIAR CLIENTE=

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
            Debug.LogError(
                "Erro ao iniciar cliente: " + e.Message
            );
        }
    }

    // ENVIAR MENSAGEM

    void EnviarMensagem(string mensagem)
    {
        try
        {
            if (cliente == null)
                return;

            byte[] dados =
                Encoding.UTF8.GetBytes(mensagem);

            cliente.Send(
                dados,
                dados.Length,
                ipServidor,
                porta
            );

            if (mensagem != "INPUT|NONE")
            {
                Debug.Log("Enviado: " + mensagem);
            }
        }
        catch (Exception e)
        {
            if (rodando)
            {
                Debug.LogError(
                    "Erro ao enviar: " + e.Message
                );
            }
        }
    }

    // RECEBER DADOS

    void ReceberDados()
    {
        IPEndPoint ponto =
            new IPEndPoint(IPAddress.Any, 0);

        while (rodando)
        {
            try
            {
                byte[] dados =
                    cliente.Receive(ref ponto);

                string mensagem =
                    Encoding.UTF8.GetString(dados);

                mensagensRecebidas.Enqueue(mensagem);
            }
            catch (SocketException e)
            {
                if (!rodando ||
                    e.ErrorCode == 10004 ||
                    e.ErrorCode == 10022 ||
                    e.ErrorCode == 10053 ||
                    e.ErrorCode == 10054)
                {
                    break;
                }

                Debug.LogError(
                    "Erro ao receber: " + e.Message
                );
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception e)
            {
                if (rodando)
                {
                    Debug.LogError(
                        "Erro ao receber: " + e.Message
                    );
                }
            }
        }
    }

    // APLICAR ESTADO RECEBIDO

    void AplicarEstado(string mensagem)
    {
        try
        {
            string[] partes =
                mensagem.Split('|');

            // Formato esperado:
            // STATE|P1Y|P2Y|BALLX|BALLY|SCORE1|SCORE2

            if (partes.Length < 7)
            {
                Debug.LogWarning(
                    "Estado recebido está incompleto: " +
                    mensagem
                );

                return;
            }

            // Posição do Player 1
            float p1Y = float.Parse(
                partes[1],
                CultureInfo.InvariantCulture
            );

            // Posição do Player 2
            float p2Y = float.Parse(
                partes[2],
                CultureInfo.InvariantCulture
            );

            // Posição X da bola
            float bolaX = float.Parse(
                partes[3],
                CultureInfo.InvariantCulture
            );

            // Posição Y da bola
            float bolaY = float.Parse(
                partes[4],
                CultureInfo.InvariantCulture
            );

            // Placar do Player 1
            int placar1 = int.Parse(
                partes[5],
                CultureInfo.InvariantCulture
            );

            // Placar do Player 2
            int placar2 = int.Parse(
                partes[6],
                CultureInfo.InvariantCulture
            );

            Debug.Log(
                "PLACAR RECEBIDO DO SERVIDOR: " +
                placar1 + " x " + placar2
            );

            // ATUALIZAR PLACAR

            AtualizarPlacarVisual(
                placar1,
                placar2
            );

            // ATUALIZAR PLAYER 1

            if (player1 != null)
            {
                Vector3 posicao =
                    player1.position;

                posicao.y = p1Y;

                player1.position = posicao;
            }

            // ATUALIZAR PLAYER 2

            if (player2 != null)
            {
                Vector3 posicao =
                    player2.position;

                posicao.y = p2Y;

                player2.position = posicao;
            }

            // ATUALIZAR BOLA

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
        catch (FormatException e)
        {
            Debug.LogError(
                "Erro de formato no estado recebido: " +
                e.Message
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Erro ao aplicar estado: " +
                e.Message
            );
        }
    }

    // ATUALIZAR PLACAR VISUAL

    void AtualizarPlacarVisual(
        int placar1,
        int placar2
    )
    {
        if (textoPlacar1 != null)
        {
            textoPlacar1.text =
                placar1.ToString();

            Debug.Log(
                "Texto Placar 1 atualizado: " +
                textoPlacar1.text
            );
        }
        else
        {
            Debug.LogError(
                "Texto Placar 1 não está conectado " +
                "no Inspector!"
            );
        }

        if (textoPlacar2 != null)
        {
            textoPlacar2.text =
                placar2.ToString();

            Debug.Log(
                "Texto Placar 2 atualizado: " +
                textoPlacar2.text
            );
        }
        else
        {
            Debug.LogError(
                "Texto Placar 2 não está conectado " +
                "no Inspector!"
            );
        }
    }

    // ENCERRAR CLIENTE

    void OnApplicationQuit()
    {
        rodando = false;

        if (cliente != null)
        {
            cliente.Close();
            cliente = null;
        }

        if (thread != null &&
            thread.IsAlive)
        {
            thread.Join(100);
        }

        Debug.Log(
            "Cliente UDP encerrado."
        );
    }
}