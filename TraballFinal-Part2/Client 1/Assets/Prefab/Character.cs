using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    private Rigidbody2D playerRigidBody;
    private Animator anim;
    private SpriteRenderer spritePersonaje;

    public Vector2 movement;
    public Vector2 posicionPrevia;
    public string TeclaPulsada;
    public static ClientBehaviour scriptCliente;
    public static GameManager scriptGameManager;

    float movementSpeed = 5f;
    float jumpForce = 5f;

    private GameObject mainCamera;
    private Camera cameraComponent;

    GameObject personaje;

    // Start is called before the first frame update
    void Start()
    {
        personaje = gameObject;
        anim = GetComponentInChildren<Animator>();
        playerRigidBody = GetComponent<Rigidbody2D>();
        spritePersonaje = GetComponentInChildren<SpriteRenderer>();

        GameObject clientServerObject = GameObject.Find("ClientServer");
        scriptCliente = clientServerObject.GetComponent<ClientBehaviour>();

        GameObject gameManagerObject = GameObject.Find("GameManager");
        scriptGameManager = gameManagerObject.GetComponent<GameManager>();

        Vector3 spawnPoint = scriptCliente.posicionSpawn;


        posicionPrevia = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        InputUsuario();

        posicionPrevia = transform.position;

        if (movement.x > 0)
        {
            anim.SetTrigger("Run");
            spritePersonaje.flipX = false;
        }
        else if (movement.x < 0)
        {
            anim.SetTrigger("Run");
            spritePersonaje.flipX = true;
        }
    }

    public void InputUsuario()
    {
        TeclaPulsada = "";

        if (Input.GetKey(KeyCode.A))
        {
            TeclaPulsada = "A";
        }
        if (Input.GetKey(KeyCode.W))
        {
            TeclaPulsada = "W";
        }
        if (Input.GetKey(KeyCode.S))
        {
            TeclaPulsada = "S";
        }
        if (Input.GetKey(KeyCode.D))
        {
            TeclaPulsada = "D";
        }

        scriptCliente.EnviarInputServidor(posicionPrevia, TeclaPulsada);
    }

    public void ActualizarMovimiento(Vector2 nuevaPosicion)
    {
        movement = nuevaPosicion;
        if (playerRigidBody != null)
        {
            playerRigidBody.velocity = new Vector2(movement.x * movementSpeed, 0);
        }
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        // Este método se llama cuando el collider de este objeto toca otro collider
        // Comprueba si el objeto con el que colisionó tiene un tag específico, por ejemplo, "Obstaculo"
        if (other.gameObject.CompareTag("Enemigo"))
        {
            scriptGameManager.MostrarMensajeGameOver();
            scriptCliente.EnviarMuertePersonaje();
        }
    }
}
