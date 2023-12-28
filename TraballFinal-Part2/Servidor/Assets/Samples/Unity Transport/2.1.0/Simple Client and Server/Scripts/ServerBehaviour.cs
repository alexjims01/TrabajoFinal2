using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using System.Collections.Generic;
using Codice.Client.Common.Encryption;
using TMPro;
using UnityEngine.SceneManagement;
using System;

/*
// CLAVES MENSAJES //
    E -> Error
    H -> Conexión realizada
    X -> Petición de conexión
    C -> Seleccion de personaje
    S -> Personaje aceptado + Posicion Spawn
    P -> Lista Personajes Disponibles
    X -> Posicion Jugadores
    M -> Movimiento/Accion del jugador
    W -> Spawn Enemigo
*/


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
        private List<string> JugadoresJugando = new List<string>();

        private Dictionary<string, string> personajesPorCliente = new Dictionary<string, string>();

        private Dictionary<string, NetworkConnection> conexionesPorId = new Dictionary<string, NetworkConnection>();


        [SerializeField] TextMeshProUGUI textoMeshPro;
        [SerializeField] TextMeshProUGUI ListaClientesConectados;

        public Transform SpawnPoint_1;
        public Transform SpawnPoint_2;

        private List<Transform> SpawnPointDisponibles = new List<Transform>();
        private List<Transform> SpawnPointOcupados = new List<Transform>();


        private bool partidaEmpezada = false;
        private bool temporizadorInicido = false;
        private float tiempoCargaEscena;
        private float tiempoEsperaEnemigo = 2.0f;
        private bool enemigoSpawned = false;
        public GameObject enemy;

        struct MensajeServidorCliente
        {
            public char CodigoMensaje;
            public FixedString4096Bytes NombresCliente;
            public FixedString4096Bytes Mensaje;
        }


        struct MensajePersonajeSeleccionado
        {
            public char CodigoMensaje;
            public FixedString4096Bytes NombresCliente;
            public FixedString4096Bytes Personaje;
            public FixedString4096Bytes Spawn;
        }
        struct MensajeMovimientoClienteServidor
        {
            public char codigoMensaje;
            public FixedString4096Bytes TeclaPulsada;
            public float PosAntX;
            public float PosAntY;
        }

        struct MensajeMovimientoServidorCliente
        {
            public char codigoMensaje;
            public FixedString4096Bytes nombrePersonaje;
            public float PosNewX;
            public float PosNewY;
        }

        struct MensajeSpawnEnemigo
        {
            public char CodigoMensaje;
            public FixedString4096Bytes Personaje;
            public FixedString4096Bytes Spawn;
        }

        void Start()
        {
            PersonajesDisponibles.Add("Martial Hero");
            PersonajesDisponibles.Add("Hero Knight");
            SpawnPointDisponibles.Add(SpawnPoint_1);
            SpawnPointDisponibles.Add(SpawnPoint_2);

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
                                Debug.LogWarning($"{idUsuario} intent� seleccionar el personaje {nuevoPersonaje}, pero ya estaba ocupado.");
                                EnviarErrorAlCliente(idUsuario, "Este personaje ya ha sido seleccionado por otro jugador.");
                            }
                            else
                            {
                                // Verificar si el usuario ya ha seleccionado un personaje
                                if (personajesPorCliente.TryGetValue(idUsuario, out string personajeAnterior))
                                {
                                    if (personajeAnterior == nuevoPersonaje)
                                    {
                                        // El usuario seleccion� el mismo personaje, no es un error.
                                        Debug.Log($"{idUsuario} intent� seleccionar el mismo personaje {nuevoPersonaje}.");
                                    }
                                    else
                                    {
                                        // El usuario seleccion� un nuevo personaje, actualizar la informaci�n.
                                        Debug.Log($"{idUsuario} ha cambiado de personaje. Anterior: {personajeAnterior}, Nuevo: {nuevoPersonaje}");
                                        personajesPorCliente[idUsuario] = nuevoPersonaje;

                                        // A�adir el personaje anterior a la lista de PersonajesDisponibles
                                        if (!string.IsNullOrEmpty(personajeAnterior))
                                        {
                                            JugadoresJugando.Remove(personajeAnterior);
                                            PersonajesDisponibles.Add(personajeAnterior);
                                        }

                                        // Remover el nuevo personaje de la lista de PersonajesDisponibles
                                        JugadoresJugando.Add(nuevoPersonaje);
                                        PersonajesDisponibles.Remove(nuevoPersonaje);

                                        // Enviar la lista actualizada de personajes disponibles a todos los clientes
                                        EnviarPersonajesDisponibles();

                                        // Actualizar la informaci�n de los clientes conectados
                                        ClientesConectados();
                                    }
                                }
                                else
                                {
                                    // El usuario a�n no ha seleccionado un personaje, realizar la selecci�n.
                                    Debug.Log($"{idUsuario} ha elegido el personaje {nuevoPersonaje} correctamente");
                                    personajesPorCliente[idUsuario] = nuevoPersonaje;

                                    // Remover el nuevo personaje de la lista de PersonajesDisponibles
                                    JugadoresJugando.Add(nuevoPersonaje);
                                    PersonajesDisponibles.Remove(nuevoPersonaje);

                                    // Enviar la lista actualizada de personajes disponibles a todos los clientes
                                    EnviarPersonajesDisponibles();

                                    // Actualizar la informaci�n de los clientes conectados
                                    ClientesConectados();

                                    // A�adir la conexi�n a la lista de conexiones por ID de usuario
                                    conexionesPorId[idUsuario] = m_Connections[i];
                                }
                                EnviarPersonajeAceptado(idUsuario, nuevoPersonaje);

                            }
                        }

                        else if (codigoMensaje == 'X')
                        {
                            codigoMensaje = 'H';
                            byte byteCodigoMensaje = (byte)codigoMensaje;
                            int numeroAleatorio = UnityEngine.Random.Range(100, 1000);
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

                        else if (codigoMensaje == 'M')
                        {
                            if(!partidaEmpezada)
                            {
                                partidaEmpezada = true;
                            }
                            MensajeMovimientoClienteServidor mensajeMovimiento = new MensajeMovimientoClienteServidor();

                            mensajeMovimiento.codigoMensaje = codigoMensaje;
                            mensajeMovimiento.TeclaPulsada = stream.ReadFixedString4096();
                            mensajeMovimiento.PosAntX = stream.ReadFloat();
                            mensajeMovimiento.PosAntY = stream.ReadFloat();

                            CalcularNuevaPosicionCliente(mensajeMovimiento, m_Connections[i]);
                        }

                    }
                    else if (cmd == NetworkEvent.Type.Disconnect)
                    {
                        Debug.Log("Client disconnected");

                        // Obtener el ID del cliente desconectado
                        string idClienteDesconectado = ObtenerIdClienteDesconectado(m_Connections[i]);

                        // Verificar si el cliente desconectado ten�a un personaje elegido
                        if (personajesPorCliente.TryGetValue(idClienteDesconectado, out string personajeDesconectado))
                        {
                            // A�adir el personaje nuevamente a la lista de PersonajesDisponibles
                            if (!string.IsNullOrEmpty(personajeDesconectado))
                            {
                                JugadoresJugando.Remove(personajeDesconectado);
                                PersonajesDisponibles.Add(personajeDesconectado);
                            }
                        }

                        // Remover al cliente desconectado de las listas
                        nombresClientes.Remove(idClienteDesconectado);
                        personajesPorCliente.Remove(idClienteDesconectado);
                        conexionesPorId.Remove(idClienteDesconectado);

                        // Establecer la conexi�n como default despu�s de realizar las operaciones
                        m_Connections[i] = default;

                        // Actualizar la lista de clientes conectados
                        if (nombresClientes.Count > 0)
                        {
                            EnviarPersonajesDisponibles();
                        }

                        // Salir del bucle para evitar iterar sobre una colecci�n modificada
                        break;
                    }
                }
            }
            if(partidaEmpezada && !temporizadorInicido)
            {
                temporizadorInicido = true;
                tiempoCargaEscena = Time.time;
            }
            if (temporizadorInicido && !enemigoSpawned && Time.time - tiempoCargaEscena > tiempoEsperaEnemigo)
            {
                // Lógica para spawnear un enemigo
                SpawnearEnemigo();
                enemigoSpawned = true;
            }
        }


/*
        struct MensajePersonajeSeleccionado
        {
            public char CodigoMensaje;
            public FixedString4096Bytes NombresCliente;
            public FixedString4096Bytes Personaje;
            public FixedString4096Bytes Spawn;
        }
*/
        void SpawnearEnemigo()
        {
            MensajeSpawnEnemigo msg = new MensajeSpawnEnemigo();
            msg.CodigoMensaje = 'W';
            msg.Personaje = "Skeleton";

            Vector3 spawnPosition = ObtenerPosicionSpawnUnica();
            

            msg.Spawn = spawnPosition.ToString();

            foreach (var conexion in m_Connections)
            {
                // Enviar el mensaje al cliente
                m_Driver.BeginSend(m_MyPipeline, conexion, out var writer);
                writer.WriteByte((byte)msg.CodigoMensaje);
                writer.WriteFixedString4096(msg.Personaje);
                writer.WriteFixedString4096(msg.Spawn);

                m_Driver.EndSend(writer);
            }
            Instantiate(enemy, spawnPosition, Quaternion.identity);
        }


        void CalcularNuevaPosicionCliente(MensajeMovimientoClienteServidor mensajeMovimiento, NetworkConnection connection)
        {
            Vector2 unidadesDesplazamiento = new Vector2();

            float desplazamiento = 1.0f;
            float salto = 10f;

            if (mensajeMovimiento.TeclaPulsada == "A") // Cambia "A" a la cadena correcta que representa la tecla A
            {
                unidadesDesplazamiento.x -= desplazamiento;
            }
            else if (mensajeMovimiento.TeclaPulsada == "D") // Cambia "D" a la cadena correcta que representa la tecla D
            {
                unidadesDesplazamiento.x += desplazamiento;
            }
            else if (mensajeMovimiento.TeclaPulsada == "W") // Cambia "W" a la cadena correcta que representa la tecla W
            {
                //unidadesDesplazamiento.y += salto;
            }
            else if (mensajeMovimiento.TeclaPulsada == "S") // Cambia "S" a la cadena correcta que representa la tecla S
            {
                //NuevaPosicionCliente.y -= desplazamiento;
            }

            string idCliente = ObtenerIdClienteDesconectado(connection);

            // La nueva posición se ha actualizado en función de la tecla pulsada
            //Debug.Log($"Nueva posición calculada para el cliente {idCliente}: " + NuevaPosicionCliente);

            EnviarPosicionClientes(idCliente, unidadesDesplazamiento);
            //EnviarPosicionRestoClientes(idCliente, unidadesDesplazamiento);


        }

        private void EnviarPosicionClientes(string idCliente, Vector2 nuevaPosicionCliente)
        {
            //FixedString4096Bytes personajeCliente;
            personajesPorCliente.TryGetValue(idCliente, out string personajeCliente);

            MensajeMovimientoServidorCliente mensaje = new MensajeMovimientoServidorCliente();

            mensaje.codigoMensaje = 'M';
            mensaje.nombrePersonaje = personajeCliente;
            mensaje.PosNewX = nuevaPosicionCliente.x;
            mensaje.PosNewY = nuevaPosicionCliente.y;

            foreach (var connection in m_Connections)
            {
                // Usar el pipeline creado al enviar datos
                m_Driver.BeginSend(m_MyPipeline, connection, out var writer);
                writer.WriteByte((byte)mensaje.codigoMensaje);
                writer.WriteFixedString4096(mensaje.nombrePersonaje);
                writer.WriteFloat(mensaje.PosNewX);
                writer.WriteFloat(mensaje.PosNewY);

                m_Driver.EndSend(writer);
            }

        }

        void EnviarErrorAlCliente(string idUsuario, string mensaje)
        {
            MensajeServidorCliente msg = new MensajeServidorCliente();

            msg.CodigoMensaje = 'E';
            msg.NombresCliente = idUsuario;
            msg.Mensaje = mensaje;

            // Obtener la conexi�n del cliente utilizando el diccionario
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
                Debug.LogError($"No se pudo encontrar la conexi�n para el cliente con ID {idUsuario}");
            }
        }

        void EnviarPersonajeAceptado(string idUsuario, string mensaje)
        {
            MensajePersonajeSeleccionado msg = new MensajePersonajeSeleccionado();
            msg.CodigoMensaje = 'S';
            msg.NombresCliente = idUsuario;
            msg.Personaje = mensaje;

            // Generar una posición de spawn única
            Vector3 spawnPosition = ObtenerPosicionSpawnUnica();

            // Guardar la posición generada para asegurarse de que no se repita
            posicionesDeSpawn.Add(spawnPosition);

            msg.Spawn = spawnPosition.ToString();

            foreach (var conexion in m_Connections)
            {
                // Enviar el mensaje al cliente
                m_Driver.BeginSend(m_MyPipeline, conexion, out var writer);
                writer.WriteByte((byte)msg.CodigoMensaje);
                writer.WriteFixedString4096(msg.NombresCliente);
                writer.WriteFixedString4096(msg.Personaje);
                writer.WriteFixedString4096(msg.Spawn);

                m_Driver.EndSend(writer);
            }
        }

        // Estructura para almacenar posiciones de spawn únicas
        private HashSet<Vector3> posicionesDeSpawn = new HashSet<Vector3>();

        // Función para obtener una posición de spawn única
        Vector3 ObtenerPosicionSpawnUnica()
        {
            float minX = -5.0f;
            float maxX = 5.0f;
            float minY = 0.78f;
            float maxY = 0.78f;
            float minZ = 0.0f;
            float maxZ = 0.0f;

            Vector3 spawnPosition;

            do
            {
                float x = UnityEngine.Random.Range(minX, maxX);
                float y = UnityEngine.Random.Range(minY, maxY);
                float z = UnityEngine.Random.Range(minZ, maxZ);

                spawnPosition = new Vector3(x, y, z);
            } while (!posicionesDeSpawn.Add(spawnPosition)); // Asegurarse de que la posición sea única

            return spawnPosition;
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
                writer.WriteByte((byte)'P'); // C�digo de mensaje para la lista de personajes disponibles

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
