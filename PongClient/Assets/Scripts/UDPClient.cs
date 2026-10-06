using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

public class UDPClient : MonoBehaviour
{
    [Header("Servidor")]
    public string ipServidor = "10.57.1.104";

    public int porta = 5001;

    [Header("Players")]
    public GameObject player1;

    public GameObject player2;

    [Header("Movimento")]
    public float velocidade = 5f;

    private UdpClient client;

    private IPEndPoint serverEP;

    private Thread receiveThread;

    private int myId = -1;

    private bool running = true;

    private ConcurrentQueue<string> mensagens =
        new ConcurrentQueue<string>();

    // =========================
    // INICIAR CLIENTE
    // =========================

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

        // Informa ao servidor
        // que este cliente entrou
        EnviarMensagem(
            "HELLO"
        );

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

    // =========================
    // UPDATE
    // =========================

    void Update()
    {
        if (!running)
            return;

        // Processa mensagens recebidas
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

        // Move o jogador local
        MoverJogador();

        // Envia a posição para o servidor
        if (myId != -1)
        {
            EnviarPosicao();
        }
    }

    // =========================
    // MOVIMENTAÇÃO
    // =========================

    void MoverJogador()
    {
        GameObject meuPlayer =
            MeuPlayer();

        if (meuPlayer == null)
            return;

        float movimento = 0f;

        // W ou seta para cima
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

        // S ou seta para baixo
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

        // Limite da tela
        posicao.y =
            Mathf.Clamp(
                posicao.y,
                -3.5f,
                3.5f
            );

        meuPlayer.transform.position =
            posicao;
    }

    // =========================
    // DESCOBRIR MEU PLAYER
    // =========================

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

    // =========================
    // PEGAR PLAYER PELO ID
    // =========================

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

    // =========================
    // ENVIAR POSIÇÃO
    // =========================

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

    // =========================
    // ENVIAR MENSAGEM UDP
    // =========================

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

    // =========================
    // RECEBER UDP
    // =========================

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

    // =========================
    // PROCESSAR MENSAGENS
    // =========================

    void ProcessarMensagem(
        string mensagem
    )
    {
        // =========================
        // RECEBEU ID
        // =========================

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

        // =========================
        // RECEBEU POSIÇÃO DE PLAYER
        // =========================

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

            // ID
            if (
                !int.TryParse(
                    partes[0],
                    out int id
                )
            )
            {
                return;
            }

            // X
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

            // Y
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

            // Não atualiza pela rede
            // o próprio jogador local.
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

    // =========================
    // ENCERRAR CLIENTE
    // =========================

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