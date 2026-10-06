using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;

public class UDPClient : MonoBehaviour
{
    [Header("Servidor")]
    public string ipServidor = "10.57.1.104";
    public int porta = 5001;

    [Header("Jogadores")]
    public GameObject player1;
    public GameObject player2;

    [Header("Bola")]
    public GameObject bola;

    [Header("Placar")]
    public TMP_Text textoPlacarPlayer1;
    public TMP_Text textoPlacarPlayer2;

    private UdpClient client;
    private IPEndPoint servidorEP;
    private Thread receiveThread;

    private bool running = true;

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

        Debug.Log(
            "Cliente UDP iniciado."
        );

        EnviarMensagem(
            "HELLO"
        );

        AtualizarPlacar();
    }

    // =====================================================
    // UPDATE
    // =====================================================

    void Update()
    {
        // -------------------------------------------------
        // ATUALIZA JOGADORES
        // -------------------------------------------------

        if (recebeuPlayer1)
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
    }

    // =====================================================
    // RECEBER DADOS
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

    // =====================================================
    // PROCESSAR MENSAGEM
    // =====================================================

    void ProcessarMensagem(
        string mensagem
    )
    {
        // -------------------------------------------------
        // ID
        // -------------------------------------------------

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

        if (
            mensagem.StartsWith(
                "BALL:"
            )
        )
        {
            ProcessarBola(
                mensagem
            );

            return;
        }
    }

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