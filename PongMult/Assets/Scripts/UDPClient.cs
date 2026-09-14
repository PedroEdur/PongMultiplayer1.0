using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UDPClient : MonoBehaviour
{
    public string ipServidor = "127.0.0.1";
    public int porta = 7777;

    private UdpClient cliente;
    private Thread thread;
    private bool rodando = false;

    void Start()
    {
        IniciarCliente();
    }

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
            Debug.LogError("Erro ao iniciar cliente: " + e.Message);
        }
    }

    void EnviarMensagem(string mensagem)
    {
        try
        {
            byte[] dados = Encoding.UTF8.GetBytes(mensagem);

            cliente.Send(
                dados,
                dados.Length,
                ipServidor,
                porta
            );

            Debug.Log("Enviado: " + mensagem);
        }
        catch (Exception e)
        {
            Debug.LogError("Erro ao enviar: " + e.Message);
        }
    }

    void ReceberDados()
    {
        IPEndPoint ponto = new IPEndPoint(IPAddress.Any, 0);

        while (rodando)
        {
            try
            {
                byte[] dados = cliente.Receive(ref ponto);

                string mensagem = Encoding.UTF8.GetString(dados);

                Debug.Log("Recebido do servidor: " + mensagem);
            }
            catch (Exception e)
            {
                if (rodando)
                    Debug.LogError("Erro ao receber: " + e.Message);
            }
        }
    }

    void OnApplicationQuit()
    {
        rodando = false;

        if (cliente != null)
            cliente.Close();

        if (thread != null)
            thread.Abort();
    }
}