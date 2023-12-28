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
        // Obtener la dirección actual del movimiento
        Vector2 currentDirection = (Vector2)transform.position - lastPosition;

        // Actualizar la última posición
        lastPosition = transform.position;

        // Cambiar la dirección del sprite en función de la dirección del movimiento
        if (currentDirection.x > 0)
        {
            // El enemigo se está moviendo hacia la derecha
            anim.SetTrigger("Run");
            spritePersonaje.flipX = false;
        }
        else if (currentDirection.x < 0)
        {
            // El enemigo se está moviendo hacia la izquierda
            anim.SetTrigger("Run");
            spritePersonaje.flipX = true;
        }
    }
}
