using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using System.Collections.Generic;

namespace Unity.Networking.Transport.Samples
{
    public class ClientBehaviour : MonoBehaviour
    {
        NetworkDriver m_Driver;
        NetworkConnection m_Connection;
        NetworkPipeline m_MyPipeline; // Nueva variable para almacenar el pipeline

        private string conexion = "Peticion de conexion";

        struct MensajeServidorCliente
        {
            public char CodigoMensaje;
            public FixedString4096Bytes NombreServidor;
            public FixedString4096Bytes NombresCliente;
            public FixedString4096Bytes NombresClienteAnterior;
            public float TiempoTranscurrido;

            public override string ToString()
            {
                return $"Codigo Mensaje: {CodigoMensaje}\nNombre Servidor: {NombreServidor}\nNombre Cliente: {NombresCliente}\nNombre Cliente Anterior: {NombresClienteAnterior}\nTiempo de vida del servidor: {TiempoTranscurrido}";
            }
        }
        void Start()
        {
            m_Driver = NetworkDriver.Create();

            // Crear el pipeline con Fragmentation y ReliableSequenced
            m_MyPipeline = m_Driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));

            var endpoint = NetworkEndpoint.Parse("192.168.1.57", 8080);
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

                    // Usar el pipeline creado al enviar datos
                    m_Driver.BeginSend(m_MyPipeline, m_Connection, out var writer);
                    writer.WriteFixedString32(conexion);
                    m_Driver.EndSend(writer);
                }
                else if (cmd == NetworkEvent.Type.Data)
                {

                    MensajeServidorCliente mensaje = new MensajeServidorCliente();
                    mensaje.CodigoMensaje = (char)stream.ReadByte();
                    mensaje.NombreServidor = stream.ReadFixedString32();
                    mensaje.NombresCliente = stream.ReadFixedString4096();
                    mensaje.NombresClienteAnterior = stream.ReadFixedString4096();
                    mensaje.TiempoTranscurrido = stream.ReadFloat();

                    Debug.Log(mensaje);

                    m_Connection.Disconnect(m_Driver);
                    m_Connection = default;
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    m_Connection = default;
                }
            }
        }
    }
}
