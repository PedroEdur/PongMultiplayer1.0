using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UDPServer : MonoBehaviour
{
    // CONFIGURAÇÃO

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

            Debug.Log("Servidor UDP iniciado na porta " + porta);
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
        EnviarEstado();
    }

    // MONTAR ESTADO DO JOGO

    void EnviarEstado()
    {
        if (player1 == null ||
            player2 == null ||
            bola == null)
        {
            Debug.LogWarning("Algum objeto não foi configurado no UDPServer!");
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

                // PRIMEIRO JOGADOR
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
                    }

                    // SEGUNDO JOGADOR
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
                    }
                }
            }
            catch
            {
                if (!rodando)
                    break;
            }
        }
    }

    // ENCERRAR SERVIDOR

    void OnApplicationQuit()
    {
        rodando = false;

        if (servidor != null)
        {
            servidor.Close();
        }
    }
}