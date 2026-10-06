using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;

public class UDPClient : MonoBehaviour
{
    [Header("Servidor")]
    public string ipServidor = "127.0.0.1";
    public int porta = 7777;

    [Header("Objetos")]
    public Transform player1;
    public Transform player2;
    public Transform bola;

    [Header("Rigidbody dos Players")]
    public Rigidbody2D rbPlayer1;
    public Rigidbody2D rbPlayer2;

    [Header("Placar")]
    public TextMeshProUGUI textoPlacar1;
    public TextMeshProUGUI textoPlacar2;

    [Header("Conexão")]
    public float intervaloHello = 1f;

    private UdpClient cliente;
    private Thread thread;
    private bool rodando = false;

    private float tempoHello = 0f;

    private ConcurrentQueue<string> mensagensRecebidas =
        new ConcurrentQueue<string>();


    void Start()
    {
        Debug.Log("=================================");
        Debug.Log("UDP CLIENT INICIANDO");
        Debug.Log("Servidor: " + ipServidor);
        Debug.Log("Porta: " + porta);
        Debug.Log("=================================");

        cliente = new UdpClient();

        rodando = true;

        thread = new Thread(ReceberDados);
        thread.IsBackground = true;
        thread.Start();

        EnviarMensagem("HELLO");

        AtualizarPlacarVisual(0, 0);
    }


    void Update()
    {
        if (!rodando)
            return;


        // HELLO periódico
        tempoHello += Time.deltaTime;

        if (tempoHello >= intervaloHello)
        {
            tempoHello = 0f;

            EnviarMensagem("HELLO");
        }


        // Envia o teclado
        EnviarInput();


        // Processa mensagens recebidas
        while (mensagensRecebidas.TryDequeue(out string mensagem))
        {
            if (mensagem.StartsWith("STATE|"))
            {
                AplicarEstado(mensagem);
            }
            else
            {
                Debug.Log("SERVIDOR: " + mensagem);
            }
        }
    }


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


    void EnviarMensagem(string mensagem)
    {
        try
        {
            if (cliente == null || !rodando)
                return;

            byte[] dados = Encoding.UTF8.GetBytes(mensagem);

            cliente.Send(
                dados,
                dados.Length,
                ipServidor,
                porta
            );

            if (mensagem != "INPUT|NONE")
            {
                Debug.Log("ENVIADO: " + mensagem);
            }
        }
        catch (Exception e)
        {
            if (rodando)
            {
                Debug.LogError(
                    "ERRO AO ENVIAR: " + e.Message
                );
            }
        }
    }


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

                if (rodando)
                {
                    Debug.LogError(
                        "ERRO AO RECEBER: " +
                        e.Message
                    );
                }
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
                        "ERRO AO RECEBER: " +
                        e.Message
                    );
                }
            }
        }
    }


    void AplicarEstado(string mensagem)
    {
        string[] partes =
            mensagem.Split('|');


        if (partes.Length < 7)
        {
            Debug.LogWarning(
                "STATE INCOMPLETO: " +
                mensagem
            );

            return;
        }


        bool p1OK = float.TryParse(
            partes[1],
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out float p1Y
        );


        bool p2OK = float.TryParse(
            partes[2],
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out float p2Y
        );


        bool bolaXOK = float.TryParse(
            partes[3],
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out float bolaX
        );


        bool bolaYOK = float.TryParse(
            partes[4],
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out float bolaY
        );


        bool placar1OK = int.TryParse(
            partes[5],
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int placar1
        );


        bool placar2OK = int.TryParse(
            partes[6],
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int placar2
        );


        if (!p1OK || !p2OK ||
            !bolaXOK || !bolaYOK ||
            !placar1OK || !placar2OK)
        {
            Debug.LogWarning(
                "ERRO AO INTERPRETAR STATE: " +
                mensagem
            );

            return;
        }


        // ============================
        // PLAYER 1
        // ============================

        if (player1 != null)
        {
            Vector3 posicao =
                player1.position;

            posicao.y = p1Y;

            player1.position =
                posicao;
        }


        if (rbPlayer1 != null)
        {
            Vector2 posicao =
                rbPlayer1.position;

            posicao.y = p1Y;

            rbPlayer1.position =
                posicao;
        }


        // ============================
        // PLAYER 2
        // ============================

        if (player2 != null)
        {
            Vector3 posicao =
                player2.position;

            posicao.y = p2Y;

            player2.position =
                posicao;
        }


        if (rbPlayer2 != null)
        {
            Vector2 posicao =
                rbPlayer2.position;

            posicao.y = p2Y;

            rbPlayer2.position =
                posicao;
        }


        // ============================
        // BOLA
        // ============================

        if (bola != null)
        {
            bola.position =
                new Vector3(
                    bolaX,
                    bolaY,
                    bola.position.z
                );
        }


        // ============================
        // PLACAR
        // ============================

        AtualizarPlacarVisual(
            placar1,
            placar2
        );


        Debug.Log(
            "STATE RECEBIDO -> " +
            "P1Y: " + p1Y +
            " | P2Y: " + p2Y +
            " | Bola: " + bolaX +
            "," + bolaY
        );
    }


    void AtualizarPlacarVisual(
        int placar1,
        int placar2)
    {
        if (textoPlacar1 != null)
        {
            textoPlacar1.text =
                placar1.ToString();
        }


        if (textoPlacar2 != null)
        {
            textoPlacar2.text =
                placar2.ToString();
        }
    }


    void OnApplicationQuit()
    {
        FecharCliente();
    }


    void OnDestroy()
    {
        FecharCliente();
    }


    void FecharCliente()
    {
        if (!rodando)
            return;


        rodando = false;


        if (cliente != null)
        {
            try
            {
                cliente.Close();
            }
            catch
            {
            }

            cliente = null;
        }


        if (thread != null &&
            thread.IsAlive)
        {
            try
            {
                thread.Join(100);
            }
            catch
            {
            }
        }
    }
}