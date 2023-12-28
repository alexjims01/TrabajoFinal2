using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    private Animator anim;
    private SpriteRenderer spritePersonaje;
    private Vector2 lastPosition;

    private void Start()
    {
        anim = GetComponentInChildren<Animator>();
        spritePersonaje = GetComponentInChildren<SpriteRenderer>();
        lastPosition = transform.position;
    }

    private void Update()
    {
        // Obtener la direcci�n actual del movimiento
        Vector2 currentDirection = (Vector2)transform.position - lastPosition;

        // Actualizar la �ltima posici�n
        lastPosition = transform.position;

        // Cambiar la direcci�n del sprite en funci�n de la direcci�n del movimiento
        if (currentDirection.x > 0)
        {
            // El enemigo se est� moviendo hacia la derecha
            anim.SetTrigger("Run");
            spritePersonaje.flipX = false;
        }
        else if (currentDirection.x < 0)
        {
            // El enemigo se est� moviendo hacia la izquierda
            anim.SetTrigger("Run");
            spritePersonaje.flipX = true;
        }
    }
}
