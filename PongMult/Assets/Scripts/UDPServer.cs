
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Globalization;
using UnityEngine;

public class UDPServer : MonoBehaviour
{
    [Header("Configuração UDP")]
    public int porta = 7777;

    [Header("Objetos do jogo")]
    public Transform player1;
    public Transform player2;
    public Transform bola;

    [Header("Placar")]
    private int placar1 = 0;
    private int placar2 = 0;

    [Header("Velocidade dos jogadores")]
    public float velocidadePlayer1 = 5f;
    public float velocidadePlayer2 = 5f;

    [Header("Envio de estado")]
    public float intervaloEstado = 0.05f;

    private UdpClient servidor;
    private Thread thread;
    private bool rodando = false;

    private float tempoEstado = 0f;

    private IPEndPoint jogador1;
    private IPEndPoint jogador2;

    private string comandoJogador2 = "INPUT|NONE";

    // INICIAR SERVIDOR

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

    // UPDATE

    void Update()
    {
        MoverPlayer1();
        MoverPlayer2();

        tempoEstado += Time.deltaTime;

        if (tempoEstado >= intervaloEstado)
        {
            tempoEstado = 0f;

            EnviarEstado();
        }
    }

    // REGISTRAR GOL

    public void RegistrarGol(bool golDoJogador1)
    {
        if (golDoJogador1)
        {
            placar1++;

            Debug.Log(
                "Gol do Jogador 1! Placar: " +
                placar1 + " x " + placar2
            );
        }
        else
        {
            placar2++;

            Debug.Log(
                "Gol do Jogador 2! Placar: " +
                placar1 + " x " + placar2
            );
        }

        // Envia imediatamente o placar atualizado
        EnviarEstado();
    }

    // MOVER PLAYER 1

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

    // MOVER PLAYER 2

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

    // ENVIAR ESTADO

    void EnviarEstado()
    {
        if (player1 == null ||
            player2 == null ||
            bola == null)
        {
            return;
        }

        // Usa ponto decimal para compatibilidade
        // com o CultureInfo.InvariantCulture do cliente

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

        EnviarParaJogador(
            jogador1,
            mensagem
        );

        EnviarParaJogador(
            jogador2,
            mensagem
        );
    }

    // ENVIAR PARA UM JOGADOR

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

            servidor.Send(
                dados,
                dados.Length,
                jogador
            );

            Debug.Log(
                "Estado enviado: " + mensagem
            );
        }
        catch (Exception e)
        {
            if (rodando)
            {
                Debug.LogError(
                    "Erro ao enviar estado: " +
                    e.Message
                );
            }
        }
    }

    // RECEBER DADOS

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

                if (mensagem.StartsWith("INPUT|"))
                {
                    // O input do jogador 2 é processado
                    // pelo servidor no Update
                    comandoJogador2 = mensagem;

                    Debug.Log(
                        "Comando recebido: " +
                        mensagem
                    );
                }

                if (mensagem == "HELLO")
                {
                    RegistrarJogador(ponto);
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
    // REGISTRAR JOGADORES

    void RegistrarJogador(IPEndPoint ponto)
    {
        if (jogador1 == null)
        {
            jogador1 = new IPEndPoint(
                ponto.Address,
                ponto.Port
            );

            Debug.Log(
                "Jogador 1 conectado: " +
                jogador1
            );

            EnviarParaJogador(
                jogador1,
                "WELCOME|PLAYER1"
            );
        }
        else if (jogador2 == null &&
                 !MesmoJogador(ponto, jogador1))
        {
            jogador2 = new IPEndPoint(
                ponto.Address,
                ponto.Port
            );

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
            Debug.Log(
                "Jogador já conectado ou limite atingido: " +
                ponto
            );
        }
    }

    // COMPARAR JOGADORES

    bool MesmoJogador(
        IPEndPoint jogadorA,
        IPEndPoint jogadorB
    )
    {
        if (jogadorA == null ||
            jogadorB == null)
        {
            return false;
        }

        return jogadorA.Address.Equals(
            jogadorB.Address
        ) &&
        jogadorA.Port == jogadorB.Port;
    }

    // ENCERRAR SERVIDOR

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