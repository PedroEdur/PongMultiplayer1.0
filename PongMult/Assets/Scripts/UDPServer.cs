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
        IPEndPoint ponto = new IPEndPoint(IPAddress.Any, 0);

        while (rodando)
        {
            try
            {
                byte[] dados = servidor.Receive(ref ponto);

                string mensagem = Encoding.UTF8.GetString(dados);

                Debug.Log("Mensagem recebida: " + mensagem);

                string resposta = "WELCOME";

                byte[] respostaBytes = Encoding.UTF8.GetBytes(resposta);

                servidor.Send(
                    respostaBytes,
                    respostaBytes.Length,
                    ponto
                );
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

        if (servidor != null)
            servidor.Close();

        if (thread != null)
            thread.Abort();
    }
}
