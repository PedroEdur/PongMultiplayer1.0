using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Globalization;

public class UDPServer : MonoBehaviour
{
    [Header("Conexão UDP")]
    public int porta = 5001;

    [Header("Placar")]
    public int placar1 = 0;
    public int placar2 = 0;

    private UdpClient server;
    private Thread receiveThread;
    private volatile bool running = false;

    private readonly object clientsLock = new object();
    private readonly Dictionary<string, int> clientIds = new Dictionary<string, int>();
    private readonly Dictionary<int, IPEndPoint> clients = new Dictionary<int, IPEndPoint>();
    private int nextId = 1;

    void Start()
    {
        try
        {
            server = new UdpClient(porta);
            running = true;
            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
            Debug.Log("Servidor UDP iniciado na porta " + porta);
        }
        catch (Exception e)
        {
            Debug.LogError("Erro ao iniciar servidor UDP: " + e.Message);
        }
    }

    private void ReceiveData()
    {
        while (running)
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = server.Receive(ref remoteEP);
                string msg = Encoding.UTF8.GetString(data);
                string key = remoteEP.Address.ToString() + ":" + remoteEP.Port;
                int clientId;

                lock (clientsLock)
                {
                    if (!clientIds.TryGetValue(key, out clientId))
                    {
                        clientId = nextId++;
                        clientIds[key] = clientId;
                        clients[clientId] = new IPEndPoint(remoteEP.Address, remoteEP.Port);
                        SendTo(remoteEP, "ASSIGN:" + clientId);
                        Debug.Log("Novo cliente " + key + " recebeu ID " + clientId);
                    }
                    else
                    {
                        clients[clientId] = new IPEndPoint(remoteEP.Address, remoteEP.Port);
                    }
                }

                if (msg.StartsWith("POS:"))
                {
                    string[] parts = msg.Substring(4).Split(';');
                    if (parts.Length != 2) continue;

                    float x;
                    float y;
                    bool xValido = float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x);
                    bool yValido = float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y);

                    if (xValido && yValido)
                    {
                        string resposta = "PLAYER:" + clientId + ";" +
                            x.ToString("F2", CultureInfo.InvariantCulture) + ";" +
                            y.ToString("F2", CultureInfo.InvariantCulture);
                        Broadcast(resposta);
                    }
                }
                else if (msg == "HELLO")
                {
                    // O ID já é atribuído acima; não é necessário fazer mais nada.
                }
            }
            catch (SocketException)
            {
                if (!running) break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception e)
            {
                if (running) Debug.LogError("Erro ao receber dados UDP: " + e.Message);
            }
        }
    }

    private void SendTo(IPEndPoint endpoint, string message)
    {
        if (server == null || endpoint == null) return;
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            server.Send(data, data.Length, endpoint);
        }
        catch (Exception e)
        {
            if (running) Debug.LogWarning("Não foi possível enviar mensagem UDP: " + e.Message);
        }
    }

    private void Broadcast(string message)
    {
        List<IPEndPoint> destinos = new List<IPEndPoint>();
        lock (clientsLock)
        {
            foreach (KeyValuePair<int, IPEndPoint> item in clients)
                destinos.Add(new IPEndPoint(item.Value.Address, item.Value.Port));
        }

        for (int i = 0; i < destinos.Count; i++)
            SendTo(destinos[i], message);
    }

    // Chamado pelo BallController quando ocorre um gol.
    public void RegistrarGol(bool golDoJogador1)
    {
        if (golDoJogador1)
            placar1++;
        else
            placar2++;

        Debug.Log("Placar: " + placar1 + " x " + placar2);
        Broadcast("SCORE:" + placar1 + ";" + placar2);
    }

    void OnApplicationQuit()
    {
        running = false;
        if (server != null)
        {
            try { server.Close(); } catch { }
            server = null;
        }
        if (receiveThread != null && receiveThread.IsAlive)
        {
            try { receiveThread.Join(200); } catch { }
        }
    }
}
