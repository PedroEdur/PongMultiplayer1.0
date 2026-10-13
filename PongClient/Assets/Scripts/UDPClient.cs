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


    // ID recebido do servidor
    private int myId = -1;


    private bool running = true;


    // Mensagens recebidas
    private ConcurrentQueue<string> mensagens =
        new ConcurrentQueue<string>();


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


        // =================================
        // HELLO
        // =================================

        EnviarMensagem(
            "HELLO"
        );


        Debug.Log(
            "Cliente iniciado"
        );

        Debug.Log(
            "Servidor: " +
            ipServidor +
            ":" +
            porta
        );
    }


    void Update()
    {
        if (!running)
            return;


        // =================================
        // PROCESSA MENSAGENS
        // =================================

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


        // =================================
        // MOVIMENTO LOCAL
        // =================================

        MoverJogador();


        // =================================
        // ENVIA POSIÇÃO
        // =================================

        if (myId != -1)
        {
            EnviarPosicao();
        }
    }


    void MoverJogador()
    {
        GameObject meuPlayer =
            MeuPlayer();


        if (meuPlayer == null)
            return;


        float movimento =
            0f;


        // W / seta para cima
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


        // S / seta para baixo
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
            "POS:" +
            x.ToString(
                "F2",
                System.Globalization.CultureInfo.InvariantCulture
            ) +
            ";" +
            y.ToString(
                "F2",
                System.Globalization.CultureInfo.InvariantCulture
            );


        EnviarMensagem(
            msg
        );
    }


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
                "Enviado: " +
                mensagem
            );
        }
        catch (Exception e)
        {
            if (running)
            {
                Debug.LogError(
                    "Erro ao enviar: " +
                    e.Message
                );
            }
        }
    }


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
                        "Erro ao receber: " +
                        e.Message
                    );
                }
            }
        }
    }


    void ProcessarMensagem(
        string mensagem
    )
    {
        // =================================
        // RECEBE ID
        // =================================

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
                    "[Cliente] Recebi ID = " +
                    myId
                );
            }


            return;
        }


        // =================================
        // RECEBE PLAYER
        // =================================

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
                return;


            if (
                !float.TryParse(
                    partes[1],
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out float x
                )
            )
                return;


            if (
                !float.TryParse(
                    partes[2],
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out float y
                )
            )
                return;


            // =================================
            // PEGA O PLAYER
            // =================================

            GameObject jogador =
                PlayerPorId(
                    id
                );


            if (jogador == null)
                return;


            // =================================
            // NÃO ALTERA O PLAYER LOCAL
            // =================================

            if (id == myId)
                return;


            // =================================
            // ALTERA O PLAYER REMOTO
            // =================================

            Vector3 posicao =
                jogador.transform.position;


            posicao.x = x;

            posicao.y = y;


            jogador.transform.position =
                posicao;


            Debug.Log(
                "[Cliente] Player " +
                id +
                " atualizado: " +
                x +
                "," +
                y
            );
        }
    }


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
                receiveThread.Join(
                    200
                );
            }
            catch
            {
            }
        }
    }
}