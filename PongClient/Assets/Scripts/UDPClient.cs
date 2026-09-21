
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UDPClient : MonoBehaviour
{
    // CONFIGURAÇÃO

    public string ipServidor = "127.0.0.1";
    public int porta = 7777;

    private UdpClient cliente;
    private Thread thread;
    private bool rodando = false;

    // INICIAR CLIENTE

    void Start()
    {
        IniciarCliente();
    }

    // ETAPA 16
    // ENVIAR COMANDOS DE MOVIMENTO

    void Update()
    {
        if (!rodando)
            return;

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

    // INICIAR CONEXÃO

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

            Debug.Log("Cliente UDP iniciado.");
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Erro ao iniciar cliente: " + e.Message
            );
        }
    }

    // ENVIAR MENSAGEM

    void EnviarMensagem(string mensagem)
    {
        try
        {
            byte[] dados =
                Encoding.UTF8.GetBytes(mensagem);

            cliente.Send(
                dados,
                dados.Length,
                ipServidor,
                porta
            );

            // Evita poluir o Console com INPUT|NONE
            if (mensagem != "INPUT|NONE")
            {
                Debug.Log("Enviado: " + mensagem);
            }
        }
        catch (Exception e)
        {
            if (rodando)
            {
                Debug.LogError(
                    "Erro ao enviar: " + e.Message
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
                    cliente.Receive(ref ponto);

                string mensagem =
                    Encoding.UTF8.GetString(dados);

                Debug.Log(
                    "Recebido do servidor: " +
                    mensagem
                );
            }
            catch (SocketException e)
            {
                if (!rodando)
                    break;

                if (e.ErrorCode == 10004 ||
                    e.ErrorCode == 10022)
                {
                    break;
                }

                Debug.LogError(
                    "Erro de socket: " +
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

    // ENCERRAR CLIENTE

    void OnApplicationQuit()
    {
        rodando = false;

        if (cliente != null)
        {
            cliente.Close();
            cliente = null;
        }
    }
}