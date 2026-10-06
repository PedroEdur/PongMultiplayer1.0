using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public class UDPServer : MonoBehaviour
{
    [Header("Servidor")]
    public int porta = 5001;

    [Header("Placar")]
    public int placarJogador1 = 0;
    public int placarJogador2 = 0;

    private UdpClient server;
    private IPEndPoint anyEP;
    private Thread receiveThread;

    private Dictionary<string, int> clientIds =
        new Dictionary<string, int>();

    private Dictionary<int, IPEndPoint> clients =
        new Dictionary<int, IPEndPoint>();

    private int nextId = 1;

    private bool running = true;

    // =====================================================
    // INICIALIZAÇÃO
    // =====================================================

    void Start()
    {
        server = new UdpClient(porta);

        anyEP = new IPEndPoint(
            IPAddress.Any,
            0
        );

        receiveThread = new Thread(
            ReceiveData
        );

        receiveThread.IsBackground = true;

        receiveThread.Start();

        Debug.Log(
            "Servidor UDP iniciado na porta " +
            porta
        );
    }

    // =====================================================
    // RECEBER DADOS
    // =====================================================

    void ReceiveData()
    {
        while (running)
        {
            try
            {
                byte[] data =
                    server.Receive(
                        ref anyEP
                    );

                string msg =
                    Encoding.UTF8.GetString(
                        data
                    );

                string key =
                    anyEP.Address.ToString()
                    + ":"
                    + anyEP.Port;

                // =================================================
                // NOVO CLIENTE
                // =================================================

                if (!clientIds.ContainsKey(key))
                {
                    int id = nextId++;

                    clientIds[key] = id;

                    clients[id] =
                        new IPEndPoint(
                            anyEP.Address,
                            anyEP.Port
                        );

                    string assignMsg =
                        "ASSIGN:" + id;

                    byte[] assignData =
                        Encoding.UTF8.GetBytes(
                            assignMsg
                        );

                    server.Send(
                        assignData,
                        assignData.Length,
                        anyEP
                    );

                    Debug.Log(
                        "Novo cliente conectado: "
                        + key
                        + " -> ID "
                        + id
                    );

                    // Envia também o placar atual
                    EnviarPlacarParaCliente(
                        anyEP
                    );
                }

                int clientId =
                    clientIds[key];

                // Atualiza endereço do cliente
                clients[clientId] =
                    new IPEndPoint(
                        anyEP.Address,
                        anyEP.Port
                    );

                // =================================================
                // RECEBER POSIÇÃO
                // =================================================

                if (msg.StartsWith("POS:"))
                {
                    string coords =
                        msg.Substring(4);

                    string[] parts =
                        coords.Split(';');

                    if (parts.Length == 2)
                    {
                        if (
                            float.TryParse(
                                parts[0],
                                System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out float x
                            )
                            &&
                            float.TryParse(
                                parts[1],
                                System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out float y
                            )
                        )
                        {
                            Debug.Log(
                                "[Servidor] ID "
                                + clientId
                                + " -> X: "
                                + x
                                + " Y: "
                                + y
                            );

                            string resposta =
                                "PLAYER:"
                                + clientId
                                + ";"
                                + x.ToString(
                                    "F2",
                                    System.Globalization.CultureInfo.InvariantCulture
                                )
                                + ";"
                                + y.ToString(
                                    "F2",
                                    System.Globalization.CultureInfo.InvariantCulture
                                );

                            Broadcast(
                                resposta
                            );
                        }
                    }
                }
            }
            catch (SocketException)
            {
                if (!running)
                {
                    break;
                }
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception e)
            {
                if (running)
                {
                    Debug.LogError(
                        "Erro no servidor: "
                        + e.Message
                    );
                }
            }
        }
    }

    // =====================================================
    // REGISTRAR GOL
    // =====================================================

    public void RegistrarGol(
        bool golDoJogador1
    )
    {
        if (golDoJogador1)
        {
            placarJogador1++;

            Debug.Log(
                "GOL DO JOGADOR 1!"
            );
        }
        else
        {
            placarJogador2++;

            Debug.Log(
                "GOL DO JOGADOR 2!"
            );
        }

        Debug.Log(
            "PLACAR: "
            + placarJogador1
            + " x "
            + placarJogador2
        );

        EnviarPlacar();
    }

    // =====================================================
    // ENVIAR PLACAR
    // =====================================================

    void EnviarPlacar()
    {
        string mensagem =
            "SCORE:"
            + placarJogador1
            + ";"
            + placarJogador2;

        Broadcast(
            mensagem
        );
    }

    // =====================================================
    // ENVIAR PLACAR PARA UM CLIENTE
    // =====================================================

    void EnviarPlacarParaCliente(
        IPEndPoint destino
    )
    {
        string mensagem =
            "SCORE:"
            + placarJogador1
            + ";"
            + placarJogador2;

        byte[] data =
            Encoding.UTF8.GetBytes(
                mensagem
            );

        try
        {
            server.Send(
                data,
                data.Length,
                destino
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Erro ao enviar placar: "
                + e.Message
            );
        }
    }

    // =====================================================
    // ENVIAR PARA TODOS OS CLIENTES
    // =====================================================

    void Broadcast(
        string mensagem
    )
    {
        byte[] data =
            Encoding.UTF8.GetBytes(
                mensagem
            );

        foreach (
            KeyValuePair<int, IPEndPoint> client
            in clients
        )
        {
            try
            {
                server.Send(
                    data,
                    data.Length,
                    client.Value
                );
            }
            catch
            {
                // Ignora erro individual
            }
        }
    }

    // =====================================================
    // RESETAR PLACAR
    // =====================================================

    public void ResetarPlacar()
    {
        placarJogador1 = 0;
        placarJogador2 = 0;

        Debug.Log(
            "Placar resetado."
        );

        EnviarPlacar();
    }

    // =====================================================
    // ENCERRAR SERVIDOR
    // =====================================================

    void OnApplicationQuit()
    {
        running = false;

        if (server != null)
        {
            try
            {
                server.Close();
            }
            catch
            {
            }

            server = null;
        }

        if (
            receiveThread != null &&
            receiveThread.IsAlive
        )
        {
            try
            {
                receiveThread.Join(200);
            }
            catch
            {
            }
        }

        Debug.Log(
            "Servidor UDP encerrado."
        );
    }
}