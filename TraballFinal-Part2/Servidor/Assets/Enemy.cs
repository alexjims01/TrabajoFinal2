using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    private Rigidbody2D enemyRigidBody;
    public Vector2 movement;
    //public static ServerBehaviour scriptServer;

    public Vector2 posicion;

    private float movementSpeed = 1.0f;
    private float direccion = 1;
    //GameObject enemy;
    // Start is called before the first frame update
    void Start()
    {
        //enemy = gameObject;
        enemyRigidBody = GetComponent<Rigidbody2D>();

        GameObject serverObject = GameObject.Find("ClientServer");
        //scriptServer = serverObject.GetComponent<ServerBehaviour>();

        posicion = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        float moveX = direccion * movementSpeed * Time.deltaTime;
        transform.Translate(new Vector2(moveX, 0));
        if (transform.position.x >= 4 && direccion == 1)
        {
            direccion = -1; // Cambia la dirección a izquierda
        }
        else if (transform.position.x <= -4 && direccion == -1)
        {
            direccion = 1; // Cambia la dirección a derecha
        }
        posicion = transform.position;
    }
}
