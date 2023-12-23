using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;
using UnityEditor.PackageManager;
using Codice;
using UnityEditor;
using System.IO;
using UnityEditor.VersionControl;
using UnityEngine.TextCore.Text;


/*
// CLAVES MENSAJES //
    E -> Error
    H
    X
    C -> Seleccion de personaje
    S -> Personaje aceptado + Posicion Spawn
    P -> Lista Personajes Disponibles
    X -> Posicion Jugadores
    M -> Movimiento/Accion del jugador
*/

public class ClientBehaviour : MonoBehaviour
{
    NetworkDriver m_Driver;
    NetworkConnection m_Connection;
    NetworkPipeline m_MyPipeline; // Nueva variable para almacenar el pipeline

    //private string conexion = "Peticion de conexion";

    [SerializeField] TMP_InputField IPField;
    private string IPaddr;

    [SerializeField] TMP_InputField PortField;
    private string serverPort;

    [SerializeField] Button boton;

    private FixedString4096Bytes IdCliente;

    string personajeSeleccionado = "";

    public GameObject[] personajesPrefabs;

    private Dictionary<string, Vector3> spawnPoints = new Dictionary<string, Vector3>();

    public static ClientBehaviour Instance { get; private set; }

    Character characterScript;
    GameObject personaje;
    public Vector3 posicionSpawn;

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

    struct MensajeClienteServidor
    {
        public char CodigoMensaje;
        public FixedString4096Bytes NombresCliente;
        public FixedString4096Bytes Personaje;
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

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        m_Driver = NetworkDriver.Create();

        // Crear el pipeline con Fragmentation y ReliableSequenced
        m_MyPipeline = m_Driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));

        if(IPField != null && PortField != null && boton != null)
        {
            IPField.onValueChanged.AddListener(ActualizarDireccionIP);
            PortField.onValueChanged.AddListener(ActualizarPuerto);
            boton.onClick.AddListener(ConnectToServer);
        }

        SharedData.Instance.CharacterSelected += CallCharacterSelected;
    }

    void ActualizarDireccionIP(string nuevaDireccion)
    {
        // Guardar la direcci�n IP ingresada
        IPaddr = nuevaDireccion;
    }

    void ActualizarPuerto(string nuevoPUerto)
    {
        // Guardar el puerto ingresada
        serverPort = nuevoPUerto;
    }

    void OnDestroy()
    {
        if (SharedData.Instance != null)
        {
        }

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
                MensajeClienteServidor PIC = new MensajeClienteServidor();
                PIC.CodigoMensaje = 'X';
                writer.WriteByte((byte)PIC.CodigoMensaje);
                m_Driver.EndSend(writer);
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                char codigoMensaje = (char)stream.ReadByte();
                if (codigoMensaje == 'E')
                {
                    string idUsuario = stream.ReadFixedString4096().ToString();
                    string mensaje = stream.ReadFixedString4096().ToString();

                    //Debug.Log(mensaje);
                }
                else if (codigoMensaje == 'P')
                {
                    // Recibir la lista de personajes disponibles
                    int cantidadPersonajes = stream.ReadInt();

                    Debug.Log("Personajes disponibles:");
                    for (int i = 0; i < cantidadPersonajes; i++)
                    {
                        string personajeDisponible = stream.ReadFixedString4096().ToString();
                        Debug.Log(personajeDisponible);
                    }
                }
                else if (codigoMensaje == 'S')
                {
                    string idUsuario = stream.ReadFixedString4096().ToString();
                    personajeSeleccionado = stream.ReadFixedString4096().ToString();
                    string posicionComoCadena = stream.ReadFixedString4096().ToString();

                    posicionSpawn = StringToVector3(posicionComoCadena);

                    spawnPoints.Add(personajeSeleccionado, posicionSpawn);

                    Debug.Log($"El cliente {idUsuario} esta listo");

                    if (spawnPoints.Count == 2)
                    {
                        LoadGame(personajeSeleccionado, posicionComoCadena);
                    }
                    
                }
                else if(codigoMensaje == 'X')
                {
                    string mensaje = stream.ReadFixedString4096().ToString();

                    personajeSeleccionado = mensaje;
                }
                else if (codigoMensaje == 'M')
                {
                    // Procesar el mensaje de movimiento
                    string nombrePersonaje = stream.ReadFixedString4096().ToString();
                    float posNewX = stream.ReadFloat();
                    float posNewY = stream.ReadFloat();

                    personaje = GameObject.Find(nombrePersonaje + "(Clone)");

                    if (personaje != null)
                    {
                        // Acceder al componente Character asociado al GameObject
                        characterScript = personaje.GetComponent<Character>();

                        // Verificar si se encontró el componente Character
                        if (characterScript != null)
                        {
                            // Ahora puedes acceder a las variables o métodos del script Character
                            
                            characterScript.ActualizarMovimiento(new Vector2(posNewX, posNewY));
                        }
                    }
                    else if(personajeSeleccionado != "")
                    {
                        //El juego aun no ha instanciado ese personaje
                        GameObject prefab = FindPersonajePrefab(nombrePersonaje);
                        Instantiate(prefab, spawnPoints[nombrePersonaje], Quaternion.identity);
                    }          
                }
                else
                {
                    MensajeServidorCliente mensaje = new MensajeServidorCliente();
                    mensaje.CodigoMensaje = codigoMensaje;
                    mensaje.NombreServidor = stream.ReadFixedString32();
                    mensaje.NombresCliente = stream.ReadFixedString4096();
                    mensaje.NombresClienteAnterior = stream.ReadFixedString4096();
                    mensaje.TiempoTranscurrido = stream.ReadFloat();

                    IdCliente = mensaje.NombresCliente;
                }
                
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Disconnected from the server.");
                m_Connection = default;
            }
        }
    }

    void ConnectToServer()
    {
        ushort _serverPort = Convert.ToUInt16(serverPort);
        //Debug.Log($"Connecting to server with ip addr -> {IPaddr} and port -> {_serverPort}");
        var endpoint = NetworkEndpoint.Parse(IPaddr, _serverPort);
        m_Connection = m_Driver.Connect(endpoint);

        if (m_Connection.IsCreated)
        {
            Debug.Log("Connected to the server successfully!");

            //DontDestroyOnLoad(gameObject);

            LoadCharacterSelection();

        }
        else
        {
            Debug.LogError("Failed to connect to the server.");
        }

    }

    public void DisconnectFromServer()
    {
        if (m_Connection.IsCreated)
        {
            m_Connection.Disconnect(m_Driver);
            m_Connection = default;
            Debug.Log("Disconnected from the server.");
        }
        else
        {
            Debug.LogWarning("No active connection to disconnect.");
        }
    }

    public void LoadCharacterSelection()
    {
        // Puedes especificar el nombre de la nueva escena que deseas cargar
        string nuevaEscena = "CharacterSelection";
        
        // Cargar la nueva escena
        SceneManager.LoadScene(nuevaEscena);

    }

    public void LoadGame(string character, string _posicionSpawn)
    {
        PlayerPrefs.SetString("PersonajeSeleccionado", character);
        PlayerPrefs.SetString("PosicionSpawn", _posicionSpawn);

        SceneManager.LoadScene("Game");

    }

    // Función para convertir una cadena de posición a un Vector3
    private Vector3 StringToVector3(string posicionSpawn)
    {
        // La cadena tiene el formato "(x, y, z)"
        // Elimina los paréntesis y divide la cadena en componentes separados por comas
        string[] components = posicionSpawn.Trim('(', ')').Split(',');

        // Convierte cada componente a un float
        float x = float.Parse(components[0]);
        float y = float.Parse(components[1]);
        float z = float.Parse(components[2]);

        // Retorna el Vector3 creado a partir de los componentes
        return new Vector3(x, y, z);
    }


    private void CallCharacterSelected(string selectedCharacterName)
    {
        // Aqu� puedes manejar el nombre del personaje seleccionado
        //Debug.Log($"Personaje seleccionado -> {selectedCharacterName}");

        MensajeClienteServidor SeleccionPersonaje = new MensajeClienteServidor();
        SeleccionPersonaje.CodigoMensaje = 'C';
        SeleccionPersonaje.Personaje = selectedCharacterName;
        SeleccionPersonaje.NombresCliente = IdCliente;

        EnviarMensajeAlServidor(SeleccionPersonaje);

    }

    private void EnviarMensajeAlServidor(MensajeClienteServidor mensaje)
    {
        if (m_Connection.IsCreated)
        {
            // Crear un escritor para el mensaje
            var writer = m_Driver.BeginSend(m_MyPipeline, m_Connection, out var tempWriter);

            // Escribir los datos del mensaje en el escritor
            tempWriter.WriteByte((byte)mensaje.CodigoMensaje);
            tempWriter.WriteFixedString4096(mensaje.NombresCliente);
            tempWriter.WriteFixedString4096(mensaje.Personaje);

            // Finalizar el env�o
            m_Driver.EndSend(tempWriter);

        }
    }

    public void EnviarInputServidor(Vector2 posAnt, string tecla)
    {
        MensajeMovimientoClienteServidor mensaje = new MensajeMovimientoClienteServidor();

        mensaje.codigoMensaje = 'M';
        mensaje.TeclaPulsada = tecla;
        mensaje.PosAntX = posAnt.x;
        mensaje.PosAntY = posAnt.y;

        if (m_Connection.IsCreated)
        {
            // Crear un escritor para el mensaje
            var writer = m_Driver.BeginSend(m_MyPipeline, m_Connection, out var tempWriter);

            // Escribir los datos del mensaje en el escritor
            tempWriter.WriteByte((byte)mensaje.codigoMensaje);
            tempWriter.WriteFixedString4096(mensaje.TeclaPulsada);
            tempWriter.WriteFloat(mensaje.PosAntX);
            tempWriter.WriteFloat(mensaje.PosAntY);

            // Finalizar el env�o
            m_Driver.EndSend(tempWriter);

        }

    }

    // Busca el prefab del personaje por nombre
    GameObject FindPersonajePrefab(string nombrePersonaje)
    {
        foreach (var prefab in personajesPrefabs)
        {
            if (prefab.name == nombrePersonaje)
            {
                return prefab;
            }
        }
        return null;
    }
}
