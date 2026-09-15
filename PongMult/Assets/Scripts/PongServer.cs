using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class PongServer : MonoBehaviour
{
    public int porta = 7777;

    public Transform bola;

    public float velocidadeBola = 5f;

    private UdpClient servidor;
    private Thread thread;

    private bool rodando = false;

    private Vector2 direcaoBola;

    void Start()
    {
        direcaoBola = new Vector2(1f, 0.5f).normalized;

        servidor = new UdpClient(porta);

        rodando = true;

        thread = new Thread(ReceberDados);
        thread.IsBackground = true;
        thread.Start();

        Debug.Log("Servidor Pong iniciado na porta " + porta);
    }

    void Update()
    {
        MovimentarBola();
    }

    void MovimentarBola()
    {
        if (bola == null)
            return;

        bola.position +=
            (Vector3)(direcaoBola * velocidadeBola * Time.deltaTime);
    }

    void OnCollisionEnter2D(Collision2D colisao)
    {
        if (colisao.gameObject.CompareTag("Wall"))
        {
            direcaoBola.y *= -1;
        }

        if (colisao.gameObject.CompareTag("Player"))
        {
            direcaoBola.x *= -1;
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

                string mensagem =
                    Encoding.UTF8.GetString(dados);

                Debug.Log("Recebido: " + mensagem);
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
    }
}