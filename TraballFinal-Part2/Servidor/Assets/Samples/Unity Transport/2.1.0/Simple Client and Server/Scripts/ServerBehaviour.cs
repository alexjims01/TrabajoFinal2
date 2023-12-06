using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using System.Collections.Generic;
using Codice.Client.Common.Encryption;
using TMPro;

namespace Unity.Networking.Transport.Samples
{
    public class ServerBehaviour : MonoBehaviour
    {
        NetworkDriver m_Driver;
        NativeList<NetworkConnection> m_Connections;
        NetworkPipeline m_MyPipeline; // Nueva variable para almacenar el pipeline

        private string nombreServidor = "Servidor Unity 1.0";
        private string idCliente = "Cliente";
        private float tiempoInicio;

        private List<string> nombresClientes = new List<string>();

        [SerializeField] TextMeshProUGUI textoMeshPro;

        void Start()
        {
            m_Driver = NetworkDriver.Create();
            m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);

            // Crear el pipeline con Fragmentation y ReliableSequenced
            m_MyPipeline = m_Driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));

            var endpoint = NetworkEndpoint.AnyIpv4.WithPort(8080);

            if (m_Driver.Bind(endpoint) != 0)
            {
                Debug.LogError("Failed to bind to port 8080.");
                return;
            }
            m_Driver.Listen();

            tiempoInicio = Time.time;

            string serverIP = GetLocalIPAddress();
            Debug.Log($"Server IP: {serverIP}");

            textoMeshPro.text = serverIP;

        }

        string GetLocalIPAddress()
        {
            string localIP = string.Empty;
            foreach (System.Net.IPAddress ip in System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName()))
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }


        void OnDestroy()
        {
            if (m_Driver.IsCreated)
            {
                m_Driver.Dispose();
                m_Connections.Dispose();
            }
        }

        void Update()
        {
            m_Driver.ScheduleUpdate().Complete();

            // Clean up connections.
            for (int i = 0; i < m_Connections.Length; i++)
            {
                if (!m_Connections[i].IsCreated)
                {
                    m_Connections.RemoveAtSwapBack(i);
                    i--;
                }
            }

            // Accept new connections.
            NetworkConnection c;
            while ((c = m_Driver.Accept()) != default)
            {
                Debug.Log("Cliente conectado");
                m_Connections.Add(c);

            }

            for (int i = 0; i < m_Connections.Length; i++)
            {
                DataStreamReader stream;
                NetworkEvent.Type cmd;
                while ((cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream)) != NetworkEvent.Type.Empty)
                {
                    if (cmd == NetworkEvent.Type.Data)
                    {
                        char codigoMensaje = 'H';
                        byte byteCodigoMensaje = (byte)codigoMensaje;
                        int numeroAleatorio = Random.Range(100, 1000);
                        FixedString4096Bytes nombreCliente = idCliente + numeroAleatorio.ToString();
                        float tiempoTranscurrido = Time.time - tiempoInicio;

                        nombresClientes.Add(nombreCliente.ToString());

                        // Usar el pipeline creado al enviar datos
                        m_Driver.BeginSend(m_MyPipeline, m_Connections[i], out var writer);
                        writer.WriteByte(byteCodigoMensaje);
                        writer.WriteFixedString32(nombreServidor);
                        writer.WriteFixedString4096(nombreCliente);
                        if(nombresClientes.Count > 1)
                        {
                            int indexClienteAnterior = nombresClientes.Count - 2;
                            string nombreClienteAnterior = nombresClientes[indexClienteAnterior].ToString();
                            writer.WriteFixedString4096(nombreClienteAnterior);
                        }
                        else
                        {
                            string nombreClienteAnterior = " ";
                            writer.WriteFixedString4096(nombreClienteAnterior);
                        }
                        
                        writer.WriteFloat(tiempoTranscurrido);

                        m_Driver.EndSend(writer);

                    }
                    else if (cmd == NetworkEvent.Type.Disconnect)
                    {
                        Debug.Log("Client disconnected");
                        m_Connections[i] = default;
                        break;
                    }
                }
            }
        }
    }
}
