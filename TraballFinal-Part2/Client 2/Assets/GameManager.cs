using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject[] personajesPrefabs;

    void Start()
    {
        string personajeSeleccionado = PlayerPrefs.GetString("PersonajeSeleccionado");

        GameObject prefab = FindPersonajePrefab(personajeSeleccionado);

        // Instancia el personaje en la escena
        if (prefab != null)
        {
            Instantiate(prefab, Vector3.zero, Quaternion.identity);
        }
        else
        {
            Debug.LogError("No se encontró el prefab del personaje: " + personajeSeleccionado);
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
