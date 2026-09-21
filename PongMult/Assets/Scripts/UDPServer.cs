using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UDPServer : MonoBehaviour
{
    public int porta = 7777;

    public Transform player1;
    public Transform player2;
    public Transform bola;

    private int placar1 = 0;
    private int placar2 = 0;

    private UdpClient servidor;
    private Thread thread;
    private bool rodando = false;

    private IPEndPoint jogador1;
    private IPEndPoint jogador2;

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

    void Update()
    {
        EnviarEstado();
    }

    void EnviarEstado()
    {
        if (player1 == null ||
            player2 == null ||
            bola == null)
        {
            return;
        }

        string mensagem =
            "STATE|" +
            player1.position.y + "|" +
            player2.position.y + "|" +
            bola.position.x + "|" +
            bola.position.y + "|" +
            placar1 + "|" +
            placar2;

        Debug.Log("Estado: " + mensagem);
    }

    void EnviarParaJogador(
        IPEndPoint jogador,
        string mensagem
    )
    {
        if (jogador == null)
            return;

        byte[] dados =
            Encoding.UTF8.GetBytes(mensagem);

        servidor.Send(
            dados,
            dados.Length,
            jogador
        );

        Debug.Log(
            "Enviado para " +
            jogador +
            ": " +
            mensagem
        );
    }

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
                    Debug.Log(
                        "Comando recebido do jogador: " +
                        mensagem
                    );
                }

                if (mensagem == "HELLO")
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

                    else if (jogador2 == null)
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
                }
            }
            catch (Exception e)
            {
                if (rodando)
                {
                    Debug.LogError(
                        "Erro ao receber: " + e.Message
                    );
                }
                else
                {
                    break;
                }
            }
        }
    }

    void OnApplicationQuit()
    {
        rodando = false;

        if (servidor != null)
        {
            servidor.Close();
        }
    }
}