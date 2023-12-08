using UnityEngine;

public class SharedData : MonoBehaviour
{
    public static SharedData Instance { get; private set; }

    public delegate void CharacterSelectedHandler(string selectedCharacterName);
    public event CharacterSelectedHandler CharacterSelected;

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

    public void CallCharacterSelected(string selectedCharacterName)
    {
        CharacterSelected?.Invoke(selectedCharacterName);
    }
}