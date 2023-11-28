using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;

namespace Unity.Networking.Transport.Samples
{
    public class ServerBehaviour : MonoBehaviour
    {
        NetworkDriver m_Driver;
        NativeList<NetworkConnection> m_Connections;
        NetworkPipeline m_MyPipeline; // Nueva variable para almacenar el pipeline

        void Start()
        {
            m_Driver = NetworkDriver.Create();
            m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);

            // Crear el pipeline con Fragmentation y ReliableSequenced
            m_MyPipeline = m_Driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));

            var endpoint = NetworkEndpoint.AnyIpv4.WithPort(80);
            if (m_Driver.Bind(endpoint) != 0)
            {
                Debug.LogError("Failed to bind to port 80.");
                return;
            }
            m_Driver.Listen();
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
                m_Connections.Add(c);
                Debug.Log("Accepted a connection.");
            }

            for (int i = 0; i < m_Connections.Length; i++)
            {
                DataStreamReader stream;
                NetworkEvent.Type cmd;
                while ((cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream)) != NetworkEvent.Type.Empty)
                {
                    if (cmd == NetworkEvent.Type.Data)
                    {
                        uint number = stream.ReadUInt();

                        Debug.Log($"Got {number} from a client, adding 2 to it.");
                        number += 2;

                        // Usar el pipeline creado al enviar datos
                        m_Driver.BeginSend(m_MyPipeline, m_Connections[i], out var writer);
                        writer.WriteUInt(number);
                        m_Driver.EndSend(writer);
                    }
                    else if (cmd == NetworkEvent.Type.Disconnect)
                    {
                        Debug.Log("Client disconnected from the server.");
                        m_Connections[i] = default;
                        break;
                    }
                }
            }
        }
    }
}
