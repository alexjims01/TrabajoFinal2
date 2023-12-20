using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;

public class GameManager : MonoBehaviour
{
    public GameObject[] personajesPrefabs;

    void Start()
    {
        string personajeSeleccionado = PlayerPrefs.GetString("PersonajeSeleccionado");
        string posicionSpawnString = PlayerPrefs.GetString("PosicionSpawn");

        Vector3 spawnPoint = Vector3.zero;
        // Dividir la cadena en componentes (x, y, z) utilizando un carácter delimitador (por ejemplo, ',')
        string[] componentes = posicionSpawnString.Replace("(", "").Replace(")", "").Split(',');

        if (componentes.Length >= 3)
        {
            // Intentar convertir las cadenas a valores de punto flotante
            float x = float.Parse(componentes[0], CultureInfo.InvariantCulture);
            float y = float.Parse(componentes[1], CultureInfo.InvariantCulture);
            float z = float.Parse(componentes[2], CultureInfo.InvariantCulture);
            spawnPoint = new Vector3(x, y, z);
        }

        GameObject prefab = FindPersonajePrefab(personajeSeleccionado);
        Debug.Log(spawnPoint);
        // Instancia el personaje en la escena
        if (prefab != null)
        {
            Instantiate(prefab, spawnPoint, Quaternion.identity);
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

