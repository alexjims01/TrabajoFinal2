using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;

namespace Unity.Networking.Transport.Samples
{
    public class ClientBehaviour : MonoBehaviour
    {
        NetworkDriver m_Driver;
        NetworkConnection m_Connection;
        NetworkPipeline m_MyPipeline; // Nueva variable para almacenar el pipeline

        private string conexion = "Peticion de conexion";


        void Start()
        {
            m_Driver = NetworkDriver.Create();

            // Crear el pipeline con Fragmentation y ReliableSequenced
            m_MyPipeline = m_Driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));

            var endpoint = NetworkEndpoint.Parse("192.168.56.1", 8080);
            m_Connection = m_Driver.Connect(endpoint);
        }

        void OnDestroy()
        {
            m_Driver.Dispose();
        }

        void Update()
        {
            m_Driver.ScheduleUpdate().Complete();

            if (!m_Connection.IsCreated)
            {
                return;
            }

            Unity.Collections.DataStreamReader stream;
            NetworkEvent.Type cmd;
            while ((cmd = m_Connection.PopEvent(m_Driver, out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Connect)
                {
                    //Debug.Log("We are now connected to the server.");

                    // Usar el pipeline creado al enviar datos
                    m_Driver.BeginSend(m_MyPipeline, m_Connection, out var writer);
                    writer.WriteFixedString32(conexion);
                    m_Driver.EndSend(writer);
                }
                else if (cmd == NetworkEvent.Type.Data)
                {
                    byte bytecodigoMensaje = stream.ReadByte();
                    char charcodigoMensaje = (char)bytecodigoMensaje;
                    FixedString32Bytes nombreServidor = stream.ReadFixedString32();
                    FixedString4096Bytes nombreCliente = stream.ReadFixedString4096();
                    FixedString4096Bytes nombreClienteAnterior = stream.ReadFixedString4096();
                    float tiempoTranscurrido = stream.ReadFloat();

                    Debug.Log($"Codigo mensaje -> {charcodigoMensaje}");
                    Debug.Log($"Nombre Servidor -> {nombreServidor}");
                    Debug.Log($"Nombre Cliente -> {nombreCliente}");
                    Debug.Log($"Nombre Ciente Anterior -> {nombreClienteAnterior}");
                    Debug.Log($"Tiempo encendido -> {tiempoTranscurrido} segundos");

                    m_Connection.Disconnect(m_Driver);
                    m_Connection = default;
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client got disconnected from server.");
                    m_Connection = default;
                }
            }
        }
    }
}
