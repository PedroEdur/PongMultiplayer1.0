
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

    private int ultimoPlacar1 = -1;
    private int ultimoPlacar2 = -1;

    // =====================================================
    // INICIALIZAÇÃO
    // =====================================================

    void Start()
    {
        ValidarReferenciasVisuais();

        AtualizarPlacarVisual(0, 0);

        IniciarCliente();
    }

    // =====================================================
    // UPDATE
    // =====================================================

    void Update()
    {
        if (!rodando)
            return;

        EnviarInput();

        while (mensagensRecebidas.TryDequeue(
            out string mensagem))
        {
            if (mensagem.StartsWith("STATE|"))
            {
                AplicarEstado(mensagem);
            }
            else
            {
                Debug.Log(
                    "Recebido do servidor: " +
                    mensagem
                );
            }
        }
    }

    // =====================================================
    // VALIDAR REFERÊNCIAS
    // =====================================================

    void ValidarReferenciasVisuais()
    {
        if (textoPlacar1 == null)
        {
            Debug.LogError(
                "ERRO: textoPlacar1 não foi conectado " +
                "no Inspector."
            );
        }
        else
        {
            Debug.Log(
                "textoPlacar1 conectado corretamente."
            );
        }

        if (textoPlacar2 == null)
        {
            Debug.LogError(
                "ERRO: textoPlacar2 não foi conectado " +
                "no Inspector."
            );
        }
        else
        {
            Debug.Log(
                "textoPlacar2 conectado corretamente."
            );
        }
    }

    // =====================================================
    // ENVIO DE INPUT
    // =====================================================

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

    // =====================================================
    // INICIAR CLIENTE
    // =====================================================

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

            Debug.Log(
                "Cliente UDP iniciado."
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Erro ao iniciar cliente: " +
                e.Message
            );
        }
    }

    // =====================================================
    // ENVIAR MENSAGEM
    // =====================================================

    void EnviarMensagem(string mensagem)
    {
        try
        {
            if (cliente == null ||
                !rodando)
            {
                return;
            }

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
                Debug.Log(
                    "Enviado: " + mensagem
                );
            }
        }
        catch (Exception e)
        {
            if (rodando)
            {
                Debug.LogError(
                    "Erro ao enviar: " +
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
    // APLICAR ESTADO RECEBIDO
    // =====================================================

    void AplicarEstado(string mensagem)
    {
        try
        {
            Debug.Log(
                "STATE RECEBIDO: " +
                mensagem
            );

            string[] partes =
                mensagem.Split('|');

            // STATE|P1Y|P2Y|BALLX|BALLY|SCORE1|SCORE2

            if (partes.Length < 7)
            {
                Debug.LogWarning(
                    "Estado incompleto: " +
                    mensagem
                );

                return;
            }

            bool p1Valido = float.TryParse(
                partes[1],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float p1Y
            );

            bool p2Valido = float.TryParse(
                partes[2],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float p2Y
            );

            bool bolaXValida = float.TryParse(
                partes[3],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float bolaX
            );

            bool bolaYValida = float.TryParse(
                partes[4],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float bolaY
            );

            bool placar1Valido = int.TryParse(
                partes[5],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int placar1
            );

            bool placar2Valido = int.TryParse(
                partes[6],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int placar2
            );

            if (!p1Valido ||
                !p2Valido ||
                !bolaXValida ||
                !bolaYValida ||
                !placar1Valido ||
                !placar2Valido)
            {
                Debug.LogError(
                    "Não foi possível interpretar " +
                    "o estado recebido: " +
                    mensagem
                );

                return;
            }

            if (placar1 != ultimoPlacar1 ||
                placar2 != ultimoPlacar2)
            {
                Debug.Log(
                    "PLACAR RECEBIDO DO SERVIDOR: " +
                    placar1 + " x " + placar2
                );

                ultimoPlacar1 = placar1;
                ultimoPlacar2 = placar2;
            }

            // Atualizar Canvas na thread principal.
            AtualizarPlacarVisual(
                placar1,
                placar2
            );

            // Atualizar Player 1.

            if (player1 != null)
            {
                Vector3 posicao =
                    player1.position;

                posicao.y = p1Y;

                player1.position = posicao;
            }

            // Atualizar Player 2.

            if (player2 != null)
            {
                Vector3 posicao =
                    player2.position;

                posicao.y = p2Y;

                player2.position = posicao;
            }

            // Atualizar bola.

            if (bola != null)
            {
                bola.position = new Vector3(
                    bolaX,
                    bolaY,
                    bola.position.z
                );
            }
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Erro ao aplicar estado: " +
                e.Message
            );
        }
    }

    // =====================================================
    // ATUALIZAR PLACAR VISUAL
    // =====================================================

    void AtualizarPlacarVisual(
        int placar1,
        int placar2
    )
    {
        if (textoPlacar1 == null ||
            textoPlacar2 == null)
        {
            Debug.LogError(
                "Não é possível atualizar o placar: " +
                "referência do Canvas ausente."
            );

            return;
        }

        textoPlacar1.text =
            placar1.ToString(
                CultureInfo.InvariantCulture
            );

        textoPlacar2.text =
            placar2.ToString(
                CultureInfo.InvariantCulture
            );

        Debug.Log(
            "CANVAS ATUALIZADO: " +
            textoPlacar1.text +
            " x " +
            textoPlacar2.text
        );
    }

    // =====================================================
    // ENCERRAR CLIENTE
    // =====================================================

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