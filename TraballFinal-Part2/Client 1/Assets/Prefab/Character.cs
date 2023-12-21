using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Character : MonoBehaviour
{

    private Rigidbody2D playerRigidBody;

    private Animator anim;

    public Vector2 movement;

    public Vector2 posicionPrevia;

    private SpriteRenderer spritePersonaje;

    public string TeclaPulsada;

    public static ClientBehaviour scriptCliente;

    float movementSpeed = 5f;
    float jumpForce = 5f;


    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        playerRigidBody = GetComponent<Rigidbody2D>();
        spritePersonaje = GetComponentInChildren<SpriteRenderer>();

        GameObject clientServerObject = GameObject.Find("ClientServer");
        scriptCliente = clientServerObject.GetComponent<ClientBehaviour>();

        posicionPrevia = transform.position;
    }

    // Update is called once per frame
    void Update()
    {

        InputUsuario();

        posicionPrevia = transform.position;

        //Debug.Log(transform.position);

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
        //playerRigidBody.velocity = new Vector2(movement.x * movementSpeed, playerRigidBody.velocity.y*jumpForce);
        if(playerRigidBody != null)
        {
            playerRigidBody.velocity = new Vector2(movement.x * movementSpeed, 0);
        }
    }
}
