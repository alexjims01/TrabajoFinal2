using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;
using UnityEditor.PackageManager;
using UnityEditor;

public class CharacterSelection : MonoBehaviour
{
    public GameObject[] characters;
    public int selectedCharacter = 0;
    public string characterName;

    public TextMeshProUGUI textoMeshPro;

    //public static event Action<string> CharacterSelected;

    private void Update()
    {
        characterName = characters[selectedCharacter].name;
        textoMeshPro.text = characterName;

    }

    public void NextCharacter()
    {
        characters[selectedCharacter].SetActive(false);
        selectedCharacter = (selectedCharacter + 1) % characters.Length;
        characters[selectedCharacter].SetActive(true);
    }

    public void PreviousCharacter()
    {
        characters[selectedCharacter].SetActive(false);
        selectedCharacter --;
        if(selectedCharacter < 0 )
        {
            selectedCharacter += characters.Length;
        }
        characters[selectedCharacter].SetActive(true);
    }

    public void SelectPlayer()
    {
        PlayerPrefs.SetInt("selectedCharacter", selectedCharacter);
        PlayerPrefs.SetString("characterName", characters[selectedCharacter].name);
        characterName = characters[selectedCharacter].name;

        SharedData.Instance.CallCharacterSelected(characterName);

    }
}

