using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using TMPro;

public class UDPClient : MonoBehaviour
{
    [Header("Servidor")]
    public string ipServidor = " 10.57.1.104";

    public int porta = 5001;

    [Header("Players")]
    public GameObject player1;

    public GameObject player2;

    [Header("Movimento")]
    public float velocidade = 5f;

    [Header("Placar")]
    public TMP_Text textoPlacarPlayer1;

    public TMP_Text textoPlacarPlayer2;

    private UdpClient client;

    private IPEndPoint serverEP;

    private Thread receiveThread;

    private int myId = -1;

    private bool running = true;

    private ConcurrentQueue<string> mensagens =
        new ConcurrentQueue<string>();

    // =====================================================
    // PLACAR LOCAL
    // =====================================================

    private int placarPlayer1 = 0;

    private int placarPlayer2 = 0;

    // =====================================================
    // START
    // =====================================================

    void Start()
    {
        client =
            new UdpClient();

        serverEP =
            new IPEndPoint(
                IPAddress.Parse(
                    ipServidor
                ),
                porta
            );

        client.Connect(
            serverEP
        );

        receiveThread =
            new Thread(
                ReceiveData
            );

        receiveThread.IsBackground = true;

        receiveThread.Start();

        EnviarMensagem(
            "HELLO"
        );

        AtualizarPlacar();

        Debug.Log(
            "Cliente UDP iniciado."
        );

        Debug.Log(
            "Servidor: "
            + ipServidor
            + ":"
            + porta
        );
    }

    // =====================================================
    // UPDATE
    // =====================================================

    void Update()
    {
        if (!running)
            return;

        while (
            mensagens.TryDequeue(
                out string mensagem
            )
        )
        {
            ProcessarMensagem(
                mensagem
            );
        }

        MoverJogador();

        if (myId != -1)
        {
            EnviarPosicao();
        }
    }

    // =====================================================
    // MOVIMENTAÇÃO
    // =====================================================

    void MoverJogador()
    {
        GameObject meuPlayer =
            MeuPlayer();

        if (meuPlayer == null)
            return;

        float movimento = 0f;

        if (
            Input.GetKey(
                KeyCode.W
            )
            ||
            Input.GetKey(
                KeyCode.UpArrow
            )
        )
        {
            movimento = 1f;
        }

        if (
            Input.GetKey(
                KeyCode.S
            )
            ||
            Input.GetKey(
                KeyCode.DownArrow
            )
        )
        {
            movimento = -1f;
        }

        Vector3 posicao =
            meuPlayer.transform.position;

        posicao.y +=
            movimento *
            velocidade *
            Time.deltaTime;

        posicao.y =
            Mathf.Clamp(
                posicao.y,
                -3.5f,
                3.5f
            );

        meuPlayer.transform.position =
            posicao;
    }

    // =====================================================
    // MEU PLAYER
    // =====================================================

    GameObject MeuPlayer()
    {
        if (myId == 1)
        {
            return player1;
        }

        if (myId == 2)
        {
            return player2;
        }

        return null;
    }

    // =====================================================
    // PLAYER PELO ID
    // =====================================================

    GameObject PlayerPorId(
        int id
    )
    {
        if (id == 1)
        {
            return player1;
        }

        if (id == 2)
        {
            return player2;
        }

        return null;
    }

    // =====================================================
    // ENVIAR POSIÇÃO
    // =====================================================

    void EnviarPosicao()
    {
        GameObject meuPlayer =
            MeuPlayer();

        if (meuPlayer == null)
            return;

        float x =
            meuPlayer.transform.position.x;

        float y =
            meuPlayer.transform.position.y;

        string msg =
            "POS:"
            + x.ToString(
                "F2",
                System.Globalization.CultureInfo.InvariantCulture
            )
            + ";"
            + y.ToString(
                "F2",
                System.Globalization.CultureInfo.InvariantCulture
            );

        EnviarMensagem(
            msg
        );
    }

    // =====================================================
    // ENVIAR MENSAGEM
    // =====================================================

    void EnviarMensagem(
        string mensagem
    )
    {
        try
        {
            byte[] data =
                Encoding.UTF8.GetBytes(
                    mensagem
                );

            client.Send(
                data,
                data.Length
            );

            Debug.Log(
                "Enviado: "
                + mensagem
            );
        }
        catch (Exception e)
        {
            if (running)
            {
                Debug.LogError(
                    "Erro ao enviar: "
                    + e.Message
                );
            }
        }
    }

    // =====================================================
    // RECEBER UDP
    // =====================================================

    void ReceiveData()
    {
        IPEndPoint remoteEP =
            new IPEndPoint(
                IPAddress.Any,
                0
            );

        while (running)
        {
            try
            {
                byte[] data =
                    client.Receive(
                        ref remoteEP
                    );

                string msg =
                    Encoding.UTF8.GetString(
                        data
                    );

                mensagens.Enqueue(
                    msg
                );
            }
            catch (SocketException)
            {
                if (!running)
                    break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception e)
            {
                if (running)
                {
                    Debug.LogError(
                        "Erro ao receber: "
                        + e.Message
                    );
                }
            }
        }
    }

    // =====================================================
    // PROCESSAR MENSAGEM
    // =====================================================

    void ProcessarMensagem(
        string mensagem
    )
    {
        // =================================================
        // RECEBEU ID
        // =================================================

        if (
            mensagem.StartsWith(
                "ASSIGN:"
            )
        )
        {
            string idTexto =
                mensagem.Substring(
                    7
                );

            if (
                int.TryParse(
                    idTexto,
                    out int id
                )
            )
            {
                myId = id;

                Debug.Log(
                    "[Cliente] Recebi ID = "
                    + myId
                );
            }

            return;
        }

        // =================================================
        // RECEBEU PLACAR
        // =================================================

        if (
            mensagem.StartsWith(
                "SCORE:"
            )
        )
        {
            ProcessarPlacar(
                mensagem
            );

            return;
        }

        // =================================================
        // RECEBEU POSIÇÃO
        // =================================================

        if (
            mensagem.StartsWith(
                "PLAYER:"
            )
        )
        {
            string dados =
                mensagem.Substring(
                    7
                );

            string[] partes =
                dados.Split(';');

            if (partes.Length != 3)
                return;

            if (
                !int.TryParse(
                    partes[0],
                    out int id
                )
            )
            {
                return;
            }

            if (
                !float.TryParse(
                    partes[1],
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out float x
                )
            )
            {
                return;
            }

            if (
                !float.TryParse(
                    partes[2],
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out float y
                )
            )
            {
                return;
            }

            GameObject jogador =
                PlayerPorId(
                    id
                );

            if (jogador == null)
                return;

            if (id == myId)
                return;

            Vector3 posicao =
                jogador.transform.position;

            posicao.x = x;
            posicao.y = y;

            jogador.transform.position =
                posicao;

            Debug.Log(
                "[Cliente] Player "
                + id
                + " atualizado: X="
                + x
                + " Y="
                + y
            );
        }
    }

    // =====================================================
    // PROCESSAR PLACAR
    // =====================================================

    void ProcessarPlacar(
        string mensagem
    )
    {
        string dados =
            mensagem.Substring(
                6
            );

        string[] partes =
            dados.Split(';');

        if (partes.Length != 2)
            return;

        if (
            !int.TryParse(
                partes[0],
                out placarPlayer1
            )
        )
        {
            return;
        }

        if (
            !int.TryParse(
                partes[1],
                out placarPlayer2
            )
        )
        {
            return;
        }

        Debug.Log(
            "[Cliente] PLACAR: "
            + placarPlayer1
            + " x "
            + placarPlayer2
        );

        AtualizarPlacar();
    }

    // =====================================================
    // ATUALIZAR TEXTO DO PLACAR
    // =====================================================

    void AtualizarPlacar()
    {
        if (textoPlacarPlayer1 != null)
        {
            textoPlacarPlayer1.text =
                placarPlayer1.ToString();
        }

        if (textoPlacarPlayer2 != null)
        {
            textoPlacarPlayer2.text =
                placarPlayer2.ToString();
        }
    }

    // =====================================================
    // ENCERRAR CLIENTE
    // =====================================================

    void OnApplicationQuit()
    {
        running = false;

        if (client != null)
        {
            try
            {
                client.Close();
            }
            catch
            {
            }

            client = null;
        }

        if (
            receiveThread != null &&
            receiveThread.IsAlive
        )
        {
            try
            {
                receiveThread.Join(200);
            }
            catch
            {
            }
        }

        Debug.Log(
            "Cliente UDP encerrado."
        );
    }
}