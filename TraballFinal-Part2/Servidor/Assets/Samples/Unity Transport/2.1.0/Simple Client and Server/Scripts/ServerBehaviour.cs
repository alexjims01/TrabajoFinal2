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

        private List<string> PersonajesDisponibles = new List<string>();

        private Dictionary<string, string> personajesPorCliente = new Dictionary<string, string>();

        private Dictionary<string, NetworkConnection> conexionesPorId = new Dictionary<string, NetworkConnection>();


        [SerializeField] TextMeshProUGUI textoMeshPro;
        [SerializeField] TextMeshProUGUI ListaClientesConectados;


        struct MensajeServidorCliente
        {
            public char CodigoMensaje;
            public FixedString4096Bytes NombresCliente;
            public FixedString4096Bytes Mensaje;
        }

        void Start()
        {
            PersonajesDisponibles.Add("Martial Hero");
            PersonajesDisponibles.Add("Hero Knight");

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

            ClientesConectados();

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
                        char codigoMensaje = (char)stream.ReadByte();
                        if (codigoMensaje == 'C')
                        {
                            string idUsuario = stream.ReadFixedString4096().ToString();
                            string nuevoPersonaje = stream.ReadFixedString4096().ToString();

                            // Verificar si el nuevo personaje ya ha sido seleccionado por otro cliente
                            if (personajesPorCliente.ContainsValue(nuevoPersonaje))
                            {
                                // El personaje ya fue seleccionado por otro cliente, enviar mensaje de error.
                                Debug.LogWarning($"{idUsuario} intentó seleccionar el personaje {nuevoPersonaje}, pero ya estaba ocupado.");
                                EnviarErrorAlCliente(idUsuario, "Este personaje ya ha sido seleccionado por otro jugador.");
                            }
                            else
                            {
                                // Verificar si el usuario ya ha seleccionado un personaje
                                if (personajesPorCliente.TryGetValue(idUsuario, out string personajeAnterior))
                                {
                                    if (personajeAnterior == nuevoPersonaje)
                                    {
                                        // El usuario seleccionó el mismo personaje, no es un error.
                                        Debug.Log($"{idUsuario} intentó seleccionar el mismo personaje {nuevoPersonaje}.");
                                    }
                                    else
                                    {
                                        // El usuario seleccionó un nuevo personaje, actualizar la información.
                                        Debug.Log($"{idUsuario} ha cambiado de personaje. Anterior: {personajeAnterior}, Nuevo: {nuevoPersonaje}");
                                        personajesPorCliente[idUsuario] = nuevoPersonaje;

                                        // Añadir el personaje anterior a la lista de PersonajesDisponibles
                                        if (!string.IsNullOrEmpty(personajeAnterior))
                                        {
                                            PersonajesDisponibles.Add(personajeAnterior);
                                        }

                                        // Remover el nuevo personaje de la lista de PersonajesDisponibles
                                        PersonajesDisponibles.Remove(nuevoPersonaje);

                                        // Enviar la lista actualizada de personajes disponibles a todos los clientes
                                        EnviarPersonajesDisponibles();

                                        // Actualizar la información de los clientes conectados
                                        ClientesConectados();
                                    }
                                }
                                else
                                {
                                    // El usuario aún no ha seleccionado un personaje, realizar la selección.
                                    Debug.Log($"{idUsuario} ha elegido el personaje {nuevoPersonaje} correctamente");
                                    personajesPorCliente[idUsuario] = nuevoPersonaje;

                                    // Remover el nuevo personaje de la lista de PersonajesDisponibles
                                    PersonajesDisponibles.Remove(nuevoPersonaje);

                                    // Enviar la lista actualizada de personajes disponibles a todos los clientes
                                    EnviarPersonajesDisponibles();

                                    // Actualizar la información de los clientes conectados
                                    ClientesConectados();

                                    // Añadir la conexión a la lista de conexiones por ID de usuario
                                    conexionesPorId[idUsuario] = m_Connections[i];
                                }
                            }
                        }

                        else if (codigoMensaje == 'X')
                        {
                            codigoMensaje = 'H';
                            byte byteCodigoMensaje = (byte)codigoMensaje;
                            int numeroAleatorio = Random.Range(100, 1000);
                            FixedString4096Bytes nombreCliente = idCliente + numeroAleatorio.ToString();
                            float tiempoTranscurrido = Time.time - tiempoInicio;

                            nombresClientes.Add(nombreCliente.ToString());

                            conexionesPorId.Add(nombreCliente.ToString(), m_Connections[i]);

                            ClientesConectados();

                            // Usar el pipeline creado al enviar datos
                            m_Driver.BeginSend(m_MyPipeline, m_Connections[i], out var writer);
                            writer.WriteByte(byteCodigoMensaje);
                            writer.WriteFixedString32(nombreServidor);
                            writer.WriteFixedString4096(nombreCliente);
                            if (nombresClientes.Count > 1)
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

                            EnviarPersonajesDisponibles();
                        }
                    }
                    else if (cmd == NetworkEvent.Type.Disconnect)
                    {
                        Debug.Log("Client disconnected");

                        // Obtener el ID del cliente desconectado
                        string idClienteDesconectado = ObtenerIdClienteDesconectado(m_Connections[i]);

                        // Verificar si el cliente desconectado tenía un personaje elegido
                        if (personajesPorCliente.TryGetValue(idClienteDesconectado, out string personajeDesconectado))
                        {
                            // Añadir el personaje nuevamente a la lista de PersonajesDisponibles
                            if (!string.IsNullOrEmpty(personajeDesconectado))
                            {
                                PersonajesDisponibles.Add(personajeDesconectado);
                            }
                        }

                        // Remover al cliente desconectado de las listas
                        nombresClientes.Remove(idClienteDesconectado);
                        personajesPorCliente.Remove(idClienteDesconectado);
                        conexionesPorId.Remove(idClienteDesconectado);

                        // Establecer la conexión como default después de realizar las operaciones
                        m_Connections[i] = default;

                        // Actualizar la lista de clientes conectados
                        if (nombresClientes.Count > 0)
                        {
                            EnviarPersonajesDisponibles();
                        }

                        // Salir del bucle para evitar iterar sobre una colección modificada
                        break;
                    }
                }
            }
        }

        void EnviarErrorAlCliente(string idUsuario, string mensaje)
        {
            MensajeServidorCliente msg = new MensajeServidorCliente();

            msg.CodigoMensaje = 'E';
            msg.NombresCliente = idUsuario;
            msg.Mensaje = mensaje;

            // Obtener la conexión del cliente utilizando el diccionario
            if (conexionesPorId.TryGetValue(idUsuario, out var connection))
            {
                // Enviar el mensaje de error al cliente
                m_Driver.BeginSend(m_MyPipeline, connection, out var writer);
                writer.WriteByte((byte)msg.CodigoMensaje);
                writer.WriteFixedString4096(msg.NombresCliente);
                writer.WriteFixedString4096(msg.Mensaje);
                m_Driver.EndSend(writer);
            }
            else
            {
                Debug.LogError($"No se pudo encontrar la conexión para el cliente con ID {idUsuario}");
            }
        }

        void ClientesConectados()
        {
            ListaClientesConectados.text = string.Empty;

            foreach (var idCliente in nombresClientes)
            {
                if (personajesPorCliente.ContainsKey(idCliente))
                {
                    ListaClientesConectados.text += $"{idCliente} -> {personajesPorCliente[idCliente]}\n";
                }
                else
                {
                    ListaClientesConectados.text += $"{idCliente}\n";
                }
            }
        }

        string ObtenerIdClienteDesconectado(NetworkConnection connection)
        {
            foreach (var kvp in conexionesPorId)
            {
                if (kvp.Value == connection)
                {
                    return kvp.Key;
                }
            }

            return null;
        }

        void EnviarPersonajesDisponibles()
        {
            foreach (var connection in m_Connections)
            {
                // Usar el pipeline creado al enviar datos
                m_Driver.BeginSend(m_MyPipeline, connection, out var writer);
                writer.WriteByte((byte)'P'); // Código de mensaje para la lista de personajes disponibles

                // Escribir la cantidad de personajes disponibles
                writer.WriteInt(PersonajesDisponibles.Count);

                // Escribir cada personaje disponible
                foreach (var personaje in PersonajesDisponibles)
                {
                    writer.WriteFixedString4096(personaje);
                }

                m_Driver.EndSend(writer);
            }
        }
    }
}
