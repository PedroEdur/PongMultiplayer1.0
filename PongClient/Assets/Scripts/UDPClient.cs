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
    public string ipServidor = "10.57.1.104";
    public int porta = 7777;

    public Transform player1;
    public Transform player2;
    public Transform bola;

    public TextMeshProUGUI textoPlacar1;
    public TextMeshProUGUI textoPlacar2;

    public float intervaloHello = 1f;

    private UdpClient cliente;
    private Thread thread;
    private bool rodando = false;
    private float tempoHello = 0f;

    private ConcurrentQueue<string> mensagensRecebidas =
        new ConcurrentQueue<string>();

    void Start()
    {
        Debug.Log("UDP CLIENT: " + ipServidor + ":" + porta);

        cliente = new UdpClient();
        rodando = true;

        thread = new Thread(ReceberDados);
        thread.IsBackground = true;
        thread.Start();

        EnviarMensagem("HELLO");
        AtualizarPlacarVisual(0, 0);
    }

    void Update()
    {
        if (!rodando) return;

        tempoHello += Time.deltaTime;

        if (tempoHello >= intervaloHello)
        {
            tempoHello = 0f;
            EnviarMensagem("HELLO");
        }

        EnviarInput();

        while (mensagensRecebidas.TryDequeue(out string mensagem))
        {
            if (mensagem.StartsWith("STATE|"))
                AplicarEstado(mensagem);
            else
                Debug.Log("Servidor: " + mensagem);
        }
    }

    void EnviarInput()
    {
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            EnviarMensagem("INPUT|UP");
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            EnviarMensagem("INPUT|DOWN");
        else
            EnviarMensagem("INPUT|NONE");
    }

    void EnviarMensagem(string mensagem)
    {
        try
        {
            if (cliente == null || !rodando) return;

            byte[] dados = Encoding.UTF8.GetBytes(mensagem);
            cliente.Send(dados, dados.Length, ipServidor, porta);

            if (mensagem != "INPUT|NONE")
                Debug.Log("ENVIADO: " + mensagem);
        }
        catch (Exception e)
        {
            if (rodando)
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
                mensagensRecebidas.Enqueue(mensagem);
            }
            catch (SocketException e)
            {
                if (!rodando || e.ErrorCode == 10004 || e.ErrorCode == 10022 ||
                    e.ErrorCode == 10053 || e.ErrorCode == 10054)
                    break;

                if (rodando) Debug.LogError("Erro ao receber: " + e.Message);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception e)
            {
                if (rodando) Debug.LogError("Erro ao receber: " + e.Message);
            }
        }
    }

    void AplicarEstado(string mensagem)
    {
        string[] partes = mensagem.Split('|');

        if (partes.Length < 7)
        {
            Debug.LogWarning("STATE incompleto: " + mensagem);
            return;
        }

        if (!float.TryParse(partes[1], NumberStyles.Float,
            CultureInfo.InvariantCulture, out float p1Y)) return;

        if (!float.TryParse(partes[2], NumberStyles.Float,
            CultureInfo.InvariantCulture, out float p2Y)) return;

        if (!float.TryParse(partes[3], NumberStyles.Float,
            CultureInfo.InvariantCulture, out float bolaX)) return;

        if (!float.TryParse(partes[4], NumberStyles.Float,
            CultureInfo.InvariantCulture, out float bolaY)) return;

        if (!int.TryParse(partes[5], NumberStyles.Integer,
            CultureInfo.InvariantCulture, out int placar1)) return;

        if (!int.TryParse(partes[6], NumberStyles.Integer,
            CultureInfo.InvariantCulture, out int placar2)) return;

        if (player1 != null)
        {
            Vector3 pos = player1.position;
            pos.y = p1Y;
            player1.position = pos;
        }

        if (player2 != null)
        {
            Vector3 pos = player2.position;
            pos.y = p2Y;
            player2.position = pos;
        }

        if (bola != null)
        {
            bola.position = new Vector3(
                bolaX,
                bolaY,
                bola.position.z
            );
        }

        AtualizarPlacarVisual(placar1, placar2);
    }

    void AtualizarPlacarVisual(int placar1, int placar2)
    {
        if (textoPlacar1 != null)
            textoPlacar1.text = placar1.ToString();

        if (textoPlacar2 != null)
            textoPlacar2.text = placar2.ToString();
    }

    void OnApplicationQuit()
    {
        FecharCliente();
    }

    void OnDestroy()
    {
        FecharCliente();
    }

    void FecharCliente()
    {
        if (!rodando) return;

        rodando = false;

        if (cliente != null)
        {
            try { cliente.Close(); }
            catch { }

            cliente = null;
        }

        if (thread != null && thread.IsAlive)
        {
            try { thread.Join(100); }
            catch { }
        }
    }
}
