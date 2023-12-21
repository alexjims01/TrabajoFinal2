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
        string otropersonaje = PlayerPrefs.GetString("OtroJugador");
        string otraposicion = PlayerPrefs.GetString("posicionOtroJugador");

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

        // Instancia el personaje en la escena
        if (prefab != null)
        {
            Instantiate(prefab, spawnPoint, Quaternion.identity);
        }
        else
        {
            Debug.LogError("No se encontró el prefab del personaje: " + personajeSeleccionado);
        }
        Debug.Log(otropersonaje);
        if(otropersonaje != "")
        {
            Debug.Log("SPAWN ENEMY");
            GameObject prefab2 = FindPersonajePrefab(otropersonaje);
            if (prefab2 != null)
            {
                Vector3 spawnPoint2 = Vector3.zero;
                // Dividir la cadena en componentes (x, y, z) utilizando un carácter delimitador (por ejemplo, ',')
                string[] componentes2 = otraposicion.Replace("(", "").Replace(")", "").Split(',');

                if (componentes2.Length >= 3)
                {
                    // Intentar convertir las cadenas a valores de punto flotante
                    float x = float.Parse(componentes2[0], CultureInfo.InvariantCulture);
                    float y = float.Parse(componentes2[1], CultureInfo.InvariantCulture);
                    float z = float.Parse(componentes2[2], CultureInfo.InvariantCulture);
                    spawnPoint2 = new Vector3(x, y, z);
                }
                Instantiate(prefab2, spawnPoint2, Quaternion.identity);
            }
            else
            {
                Debug.LogError("No se encontró el prefab del personaje: " + personajeSeleccionado);
            }
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

