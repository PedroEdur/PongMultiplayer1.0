
using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UDPServer : MonoBehaviour
{
    [Header("Conexão UDP")]
    public int porta = 10571104;

    [Header("Objetos do jogo")]
    public Transform player1;
    public Transform player2;
    public Transform bola;

    [Header("Velocidade dos jogadores")]
    public float velocidadePlayer1 = 5f;
    public float velocidadePlayer2 = 5f;

    [Header("Envio de estado")]
    public float intervaloEstado = 0.05f;

    [Header("Placar")]
    public int placar1 = 0;
    public int placar2 = 0;

    private UdpClient servidor;
    private Thread thread;
    private bool rodando = false;

    private float tempoEstado = 0f;

    private IPEndPoint jogador1;
    private IPEndPoint jogador2;

    private string comandoJogador2 = "INPUT|NONE";

    private readonly object bloqueioRede = new object();

    // =====================================================
    // INICIALIZAÇÃO
    // =====================================================

    void Start()
    {
        try
        {
            servidor = new UdpClient(porta);

            rodando = true;

            thread = new Thread(ReceberDados);
            thread.IsBackground = true;
            thread.Start();

            Debug.Log(
                "Servidor UDP iniciado na porta " + porta
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Erro ao iniciar servidor: " + e.Message
            );
        }
    }

    // =====================================================
    // UPDATE
    // =====================================================

    void Update()
    {
        if (!rodando)
            return;

        MoverPlayer1();
        MoverPlayer2();

        tempoEstado += Time.deltaTime;

        if (tempoEstado >= intervaloEstado)
        {
            tempoEstado = 0f;

            EnviarEstado();
        }
    }

    // =====================================================
    // MOVIMENTO DO PLAYER 1
    // =====================================================

    void MoverPlayer1()
    {
        if (player1 == null)
            return;

        float movimento = 0f;

        if (Input.GetKey(KeyCode.W))
        {
            movimento = 1f;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            movimento = -1f;
        }

        Vector3 posicao = player1.position;

        posicao.y += movimento *
                     velocidadePlayer1 *
                     Time.deltaTime;

        posicao.y = Mathf.Clamp(
            posicao.y,
            -3.5f,
            3.5f
        );

        player1.position = posicao;
    }

    // =====================================================
    // MOVIMENTO DO PLAYER 2
    // =====================================================

    void MoverPlayer2()
    {
        if (player2 == null)
            return;

        float movimento = 0f;

        if (comandoJogador2 == "INPUT|UP")
        {
            movimento = 1f;
        }
        else if (comandoJogador2 == "INPUT|DOWN")
        {
            movimento = -1f;
        }

        Vector3 posicao = player2.position;

        posicao.y += movimento *
                     velocidadePlayer2 *
                     Time.deltaTime;

        posicao.y = Mathf.Clamp(
            posicao.y,
            -3.5f,
            3.5f
        );

        player2.position = posicao;
    }

    // =====================================================
    // REGISTRAR GOL
    // =====================================================

    public void RegistrarGol(bool golDoJogador1)
    {
        if (!rodando)
        {
            Debug.LogWarning(
                "Gol ignorado: servidor não está ativo."
            );

            return;
        }

        if (golDoJogador1)
        {
            placar1++;

            Debug.Log(
                "GOL DO JOGADOR 1! Placar atual: " +
                placar1 + " x " + placar2
            );
        }
        else
        {
            placar2++;

            Debug.Log(
                "GOL DO JOGADOR 2! Placar atual: " +
                placar1 + " x " + placar2
            );
        }

        // Envia o placar imediatamente após o gol.
        EnviarEstado();
    }

    // =====================================================
    // MONTAR E ENVIAR ESTADO
    // =====================================================

    public void EnviarEstado()
    {
        if (!rodando)
            return;

        if (player1 == null ||
            player2 == null ||
            bola == null)
        {
            Debug.LogWarning(
                "Não foi possível enviar estado: " +
                "player1, player2 ou bola não configurado."
            );

            return;
        }

        string mensagem =
            "STATE|" +
            player1.position.y.ToString(
                CultureInfo.InvariantCulture
            ) + "|" +
            player2.position.y.ToString(
                CultureInfo.InvariantCulture
            ) + "|" +
            bola.position.x.ToString(
                CultureInfo.InvariantCulture
            ) + "|" +
            bola.position.y.ToString(
                CultureInfo.InvariantCulture
            ) + "|" +
            placar1.ToString(
                CultureInfo.InvariantCulture
            ) + "|" +
            placar2.ToString(
                CultureInfo.InvariantCulture
            );

        Debug.Log(
            "ESTADO ENVIADO: " + mensagem
        );

        EnviarParaJogador(jogador1, mensagem);
        EnviarParaJogador(jogador2, mensagem);
    }

    // =====================================================
    // ENVIAR PARA UM CLIENTE
    // =====================================================

    void EnviarParaJogador(
        IPEndPoint jogador,
        string mensagem
    )
    {
        if (jogador == null ||
            servidor == null)
        {
            return;
        }

        try
        {
            byte[] dados =
                Encoding.UTF8.GetBytes(mensagem);

            lock (bloqueioRede)
            {
                if (servidor != null)
                {
                    servidor.Send(
                        dados,
                        dados.Length,
                        jogador
                    );
                }
            }
        }
        catch (Exception e)
        {
            if (rodando)
            {
                Debug.LogError(
                    "Erro ao enviar para jogador: " +
                    e.Message
                );
            }
        }
    }

    // =====================================================
    // RECEBER DADOS
    // =====================================================

    void ReceberDados()
    {
        IPEndPoint ponto =
            new IPEndPoint(
                IPAddress.Any,
                0
            );

        while (rodando)
        {
            try
            {
                byte[] dados =
                    servidor.Receive(ref ponto);

                string mensagem =
                    Encoding.UTF8.GetString(dados);

                Debug.Log(
                    "Recebido: " +
                    mensagem +
                    " de " +
                    ponto
                );

                if (mensagem == "HELLO")
                {
                    RegistrarJogador(ponto);
                }
                else if (mensagem.StartsWith("INPUT|"))
                {
                    // Somente o jogador 2 controla o Player 2.
                    if (MesmoJogador(ponto, jogador2))
                    {
                        comandoJogador2 = mensagem;

                        Debug.Log(
                            "Input do jogador 2: " +
                            mensagem
                        );
                    }
                }
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
                    "Erro ao receber: " +
                    e.Message
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
                        "Erro ao receber: " +
                        e.Message
                    );
                }
            }
        }
    }

    // =====================================================
    // REGISTRAR JOGADORES
    // =====================================================

    void RegistrarJogador(IPEndPoint ponto)
    {
        if (MesmoJogador(ponto, jogador1))
        {
            EnviarParaJogador(
                jogador1,
                "WELCOME|PLAYER1"
            );

            Debug.Log(
                "HELLO repetido do jogador 1."
            );

            return;
        }

        if (MesmoJogador(ponto, jogador2))
        {
            EnviarParaJogador(
                jogador2,
                "WELCOME|PLAYER2"
            );

            Debug.Log(
                "HELLO repetido do jogador 2."
            );

            return;
        }

        if (jogador1 == null)
        {
            jogador1 = CriarEndpoint(ponto);

            Debug.Log(
                "Jogador 1 conectado: " +
                jogador1
            );

            EnviarParaJogador(
                jogador1,
                "WELCOME|PLAYER1"
            );
        }
        else if (jogador2 == null)
        {
            jogador2 = CriarEndpoint(ponto);

            Debug.Log(
                "Jogador 2 conectado: " +
                jogador2
            );

            EnviarParaJogador(
                jogador2,
                "WELCOME|PLAYER2"
            );
        }
        else
        {
            Debug.LogWarning(
                "Servidor já possui dois jogadores."
            );
        }
    }

    IPEndPoint CriarEndpoint(IPEndPoint ponto)
    {
        return new IPEndPoint(
            ponto.Address,
            ponto.Port
        );
    }

    bool MesmoJogador(
        IPEndPoint a,
        IPEndPoint b
    )
    {
        if (a == null || b == null)
            return false;

        return a.Address.Equals(b.Address) &&
               a.Port == b.Port;
    }

    // =====================================================
    // ENCERRAR SERVIDOR
    // =====================================================

    void OnApplicationQuit()
    {
        rodando = false;

        if (servidor != null)
        {
            servidor.Close();
            servidor = null;
        }

        if (thread != null &&
            thread.IsAlive)
        {
            thread.Join(500);
        }

        Debug.Log(
            "Servidor UDP encerrado com segurança."
        );
    }
}