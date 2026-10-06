using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
<<<<<<< HEAD
using TMPro;
=======
using System.Collections.Concurrent;
>>>>>>> parent of 1ec71a0 (placares client1)

public class UDPClient : MonoBehaviour
{
    [Header("Servidor")]
    public string ipServidor = "10.57.1.104";
<<<<<<< HEAD
=======

>>>>>>> parent of 1ec71a0 (placares client1)
    public int porta = 5001;

    [Header("Jogadores")]
    public GameObject player1;
    public GameObject player2;

    [Header("Bola")]
    public GameObject bola;

<<<<<<< HEAD
    [Header("Placar")]
    public TMP_Text textoPlacarPlayer1;
    public TMP_Text textoPlacarPlayer2;

=======
>>>>>>> parent of 1ec71a0 (placares client1)
    private UdpClient client;
    private IPEndPoint servidorEP;
    private Thread receiveThread;

    private bool running = true;

<<<<<<< HEAD
    private int meuId = 0;

    private int placarPlayer1 = 0;
    private int placarPlayer2 = 0;

    // Posição dos jogadores recebida da rede
    private Vector2 posPlayer1;
    private Vector2 posPlayer2;

    private bool recebeuPlayer1 = false;
    private bool recebeuPlayer2 = false;

    // Posição da bola recebida da rede
    private Vector2 posicaoBola;
    private bool recebeuBola = false;

    // =====================================================
    // INICIALIZAÇÃO
    // =====================================================
=======
    private ConcurrentQueue<string> mensagens =
        new ConcurrentQueue<string>();

    // =========================
    // INICIAR CLIENTE
    // =========================
>>>>>>> parent of 1ec71a0 (placares client1)

    void Start()
    {
        client = new UdpClient();

        servidorEP =
            new IPEndPoint(
                IPAddress.Parse(ipServidor),
                porta
            );

        receiveThread =
            new Thread(
                ReceiveData
            );

        receiveThread.IsBackground = true;
        receiveThread.Start();

<<<<<<< HEAD
        Debug.Log(
            "Cliente UDP iniciado."
        );

=======
        // Informa ao servidor
        // que este cliente entrou
>>>>>>> parent of 1ec71a0 (placares client1)
        EnviarMensagem(
            "HELLO"
        );

<<<<<<< HEAD
        AtualizarPlacar();
=======
        Debug.Log(
            "Cliente UDP iniciado."
        );

        Debug.Log(
            "Servidor: "
            + ipServidor
            + ":"
            + porta
        );
>>>>>>> parent of 1ec71a0 (placares client1)
    }

    // =========================
    // UPDATE
    // =========================

    void Update()
    {
        // -------------------------------------------------
        // ATUALIZA JOGADORES
        // -------------------------------------------------

<<<<<<< HEAD
        if (recebeuPlayer1)
=======
        // Processa mensagens recebidas
        while (
            mensagens.TryDequeue(
                out string mensagem
            )
        )
>>>>>>> parent of 1ec71a0 (placares client1)
        {
            if (player1 != null)
            {
                player1.transform.position =
                    new Vector3(
                        posPlayer1.x,
                        posPlayer1.y,
                        player1.transform.position.z
                    );
            }

            recebeuPlayer1 = false;
        }

        if (recebeuPlayer2)
        {
            if (player2 != null)
            {
                player2.transform.position =
                    new Vector3(
                        posPlayer2.x,
                        posPlayer2.y,
                        player2.transform.position.z
                    );
            }

            recebeuPlayer2 = false;
        }

        // -------------------------------------------------
        // ATUALIZA BOLA
        // -------------------------------------------------

        if (recebeuBola)
        {
            if (bola != null)
            {
                bola.transform.position =
                    new Vector3(
                        posicaoBola.x,
                        posicaoBola.y,
                        bola.transform.position.z
                    );
            }

            recebeuBola = false;
        }

        // -------------------------------------------------
        // ENVIA POSIÇÃO DO MEU JOGADOR
        // -------------------------------------------------

        GameObject meuPlayer = null;

        if (meuId == 1)
        {
            meuPlayer = player1;
        }
        else if (meuId == 2)
        {
            meuPlayer = player2;
        }

        if (meuPlayer != null)
        {
            string mensagem =
                "POS:"
                + meuPlayer.transform.position.x.ToString(
                    "F2",
                    System.Globalization.CultureInfo.InvariantCulture
                )
                + ";"
                + meuPlayer.transform.position.y.ToString(
                    "F2",
                    System.Globalization.CultureInfo.InvariantCulture
                );

            EnviarMensagem(
                mensagem
            );
        }
<<<<<<< HEAD
    }

    // =====================================================
    // RECEBER DADOS
    // =====================================================
=======

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
>>>>>>> parent of 1ec71a0 (placares client1)

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

                string mensagem =
                    Encoding.UTF8.GetString(
                        data
                    );

                ProcessarMensagem(
                    mensagem
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
                        "Erro no cliente: "
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
<<<<<<< HEAD
        // -------------------------------------------------
        // ID
        // -------------------------------------------------
=======
        // =========================
        // RECEBEU ID
        // =========================
>>>>>>> parent of 1ec71a0 (placares client1)

        if (
            mensagem.StartsWith(
                "ASSIGN:"
            )
        )
        {
            string idTexto =
                mensagem.Substring(7);

            if (
                int.TryParse(
                    idTexto,
                    out meuId
                )
            )
            {
                Debug.Log(
                    "[Cliente] Meu ID: "
                    + meuId
                );
            }

            return;
        }

<<<<<<< HEAD
        // -------------------------------------------------
        // PLAYER
        // -------------------------------------------------

        if (
            mensagem.StartsWith(
                "PLAYER:"
            )
        )
        {
            ProcessarJogador(
                mensagem
            );

            return;
        }

        // -------------------------------------------------
        // PLACAR
        // -------------------------------------------------

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

        // -------------------------------------------------
        // BOLA
        // -------------------------------------------------
=======
        // =========================
        // RECEBEU POSIÇÃO DE PLAYER
        // =========================
>>>>>>> parent of 1ec71a0 (placares client1)

        if (
            mensagem.StartsWith(
                "BALL:"
            )
        )
        {
<<<<<<< HEAD
            ProcessarBola(
                mensagem
=======
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
>>>>>>> parent of 1ec71a0 (placares client1)
            );

            return;
        }
    }

<<<<<<< HEAD
    // =====================================================
    // PROCESSAR JOGADOR
    // =====================================================

    void ProcessarJogador(
        string mensagem
    )
    {
        string dados =
            mensagem.Substring(7);

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

        if (id == 1)
        {
            posPlayer1 =
                new Vector2(
                    x,
                    y
                );

            recebeuPlayer1 = true;
        }
        else if (id == 2)
        {
            posPlayer2 =
                new Vector2(
                    x,
                    y
                );

            recebeuPlayer2 = true;
        }
    }

    // =====================================================
    // PROCESSAR BOLA
    // =====================================================

    void ProcessarBola(
        string mensagem
    )
    {
        string dados =
            mensagem.Substring(5);

        string[] partes =
            dados.Split(';');

        if (partes.Length != 2)
            return;

        if (
            !float.TryParse(
                partes[0],
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
                partes[1],
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out float y
            )
        )
        {
            return;
        }

        posicaoBola =
            new Vector2(
                x,
                y
            );

        recebeuBola = true;
    }

    // =====================================================
    // PROCESSAR PLACAR
    // =====================================================

    void ProcessarPlacar(
        string mensagem
    )
    {
        string dados =
            mensagem.Substring(6);

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
    // ATUALIZAR PLACAR
    // =====================================================

    void AtualizarPlacar()
    {
        if (
            textoPlacarPlayer1 != null
        )
        {
            textoPlacarPlayer1.text =
                placarPlayer1.ToString();
        }

        if (
            textoPlacarPlayer2 != null
        )
        {
            textoPlacarPlayer2.text =
                placarPlayer2.ToString();
        }
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
                data.Length,
                servidorEP
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Erro ao enviar mensagem: "
                + e.Message
            );
        }
    }

    // =====================================================
    // ENCERRAR
    // =====================================================
=======
    // =========================
    // ENCERRAR CLIENTE
    // =========================
>>>>>>> parent of 1ec71a0 (placares client1)

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