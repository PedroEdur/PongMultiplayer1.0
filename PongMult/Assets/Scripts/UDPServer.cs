using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UDPServer : MonoBehaviour
{
    public int porta = 7777;

    private UdpClient servidor;
    private Thread thread;
    private bool rodando = false;

    private IPEndPoint jogador1;
    private IPEndPoint jogador2;

    void Start()
    {
        IniciarServidor();
    }

    void IniciarServidor()
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
            Debug.LogError("Erro ao iniciar servidor: " + e.Message);
        }
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

                if (mensagem == "HELLO")
                {
                    // PRIMEIRO JOGADOR
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

    void OnApplicationQuit()
    {
        rodando = false;

        if (servidor != null)
            servidor.Close();

        if (thread != null)
            thread.Abort();
    }
}
