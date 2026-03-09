using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class CardManager : MonoBehaviour
{
    // URLs de las APIs
    private string fakeApiURL = "https://my-json-server.typicode.com/DannieLudens/Sistemas-Interactivos-distribuidos/users";
    private string rickAndMortyURL = "https://rickandmortyapi.com/api/character";

    // Referencias UI (se buscan por codigo, no se arrastran)
    private TMP_Dropdown userDropdown;
    private TextMeshProUGUI userNameText;
    private RawImage[] cardImages = new RawImage[10];
    private TextMeshProUGUI[] cardNameTexts = new TextMeshProUGUI[10];

    // Datos
    private User[] allUsers;

    void Start()
    {
        // Buscar el Dropdown en el Canvas
        userDropdown = GameObject.Find("Dropdown").GetComponent<TMP_Dropdown>();

        // Buscar el texto del nombre del usuario
        userNameText = GameObject.Find("Text (TMP)").GetComponent<TextMeshProUGUI>();

        // Buscar las 10 RawImages
        // La primera se llama "RawImage", las demas "RawImage (1)" hasta "RawImage (9)"
        cardImages[0] = GameObject.Find("RawImage").GetComponent<RawImage>();
        for (int i = 1; i < 10; i++)
        {
            cardImages[i] = GameObject.Find("RawImage (" + i + ")").GetComponent<RawImage>();
        }

        // Crear un texto debajo de cada RawImage para el nombre del personaje
        for (int i = 0; i < 10; i++)
        {
            CreateCardNameText(i);
        }

        // Configurar el evento del Dropdown
        userDropdown.onValueChanged.AddListener(OnUserSelected);

        // Cargar los usuarios desde la API falsa
        StartCoroutine(GetAllUsers());
    }

    void CreateCardNameText(int index)
    {
        // Crear un nuevo GameObject para el texto
        GameObject textObj = new GameObject("CardName_" + index);

        // Hacerlo hijo del Canvas (mismo padre que las RawImages)
        textObj.transform.SetParent(cardImages[index].transform.parent);

        // Agregar el componente TextMeshProUGUI
        cardNameTexts[index] = textObj.AddComponent<TextMeshProUGUI>();
        cardNameTexts[index].text = "";
        cardNameTexts[index].fontSize = 14;
        cardNameTexts[index].alignment = TextAlignmentOptions.Center;
        cardNameTexts[index].color = Color.white;

        // Posicionar debajo de la RawImage correspondiente
        RectTransform cardRect = cardImages[index].GetComponent<RectTransform>();
        RectTransform textRect = textObj.GetComponent<RectTransform>();

        // Copiar posicion de la carta y mover hacia abajo
        textRect.anchorMin = cardRect.anchorMin;
        textRect.anchorMax = cardRect.anchorMax;
        textRect.anchoredPosition = cardRect.anchoredPosition + new Vector2(0, -cardRect.sizeDelta.y / 2 - 15);
        textRect.sizeDelta = new Vector2(cardRect.sizeDelta.x, 30);
        textRect.localScale = Vector3.one;
    }

    // ==================== CONSULTAS A API FALSA ====================

    IEnumerator GetAllUsers()
    {
        UnityWebRequest www = UnityWebRequest.Get(fakeApiURL);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error al obtener usuarios: " + www.error);
        }
        else
        {
            // JsonUtility no soporta arrays raiz, entonces lo envolvemos
            string jsonArray = www.downloadHandler.text;
            string wrappedJson = "{\"users\":" + jsonArray + "}";
            UserList userList = JsonUtility.FromJson<UserList>(wrappedJson);
            allUsers = userList.users;

            // Llenar el Dropdown con los nombres de los usuarios
            userDropdown.ClearOptions();
            var options = new System.Collections.Generic.List<string>();
            for (int i = 0; i < allUsers.Length; i++)
            {
                options.Add(allUsers[i].name);
            }
            userDropdown.AddOptions(options);

            // Cargar el primer usuario automaticamente
            OnUserSelected(0);
        }
    }

    void OnUserSelected(int index)
    {
        if (allUsers == null || index >= allUsers.Length) return;

        // Detener TODAS las corrutinas anteriores para evitar conflictos
        StopAllCoroutines();

        User selectedUser = allUsers[index];
        userNameText.text = "Usuario: " + selectedUser.name;

        // Cargar las cartas del usuario seleccionado
        StartCoroutine(LoadUserDeck(selectedUser));
    }

    // ==================== CONSULTAS A API DE TERCEROS ====================

    IEnumerator LoadUserDeck(User user)
    {
        // Limpiar cartas anteriores
        for (int i = 0; i < 10; i++)
        {
            cardImages[i].texture = null;
            cardNameTexts[i].text = "Cargando...";
        }

        // Consultar cada personaje del deck uno por uno para mantener control
        for (int i = 0; i < user.deck.Length && i < 10; i++)
        {
            yield return StartCoroutine(GetCharacter(user.deck[i], i));
            yield return new WaitForSeconds(0.3f);
        }
    }

    IEnumerator GetCharacter(int characterId, int cardIndex)
    {
        UnityWebRequest www = UnityWebRequest.Get(rickAndMortyURL + "/" + characterId);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error al obtener personaje " + characterId + ": " + www.error + " | Response Code: " + www.responseCode + " | URL: " + rickAndMortyURL + "/" + characterId);
            cardNameTexts[cardIndex].text = www.error + " Code:" + www.responseCode;
        }
        else
        {
            Character character = JsonUtility.FromJson<Character>(www.downloadHandler.text);
            cardNameTexts[cardIndex].text = character.name;
            Debug.Log("Carta " + cardIndex + ": " + character.name + " - " + character.species);

            // Descargar la imagen del personaje
            StartCoroutine(GetCharacterImage(character.image, cardIndex));
        }
    }

    IEnumerator GetCharacterImage(string imageUrl, int cardIndex)
    {
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Error al descargar imagen: " + uwr.error);
            }
            else
            {
                var texture = DownloadHandlerTexture.GetContent(uwr);
                cardImages[cardIndex].texture = texture;
            }
        }
    }
}

// ==================== CLASES MODELO ====================

// Modelo del usuario que viene de la API falsa
[System.Serializable]
public class User
{
    public int id;
    public string name;
    public int[] deck;
}

// Wrapper para deserializar el array de usuarios
// JsonUtility no puede deserializar arrays directamente, necesita un objeto que los envuelva
[System.Serializable]
public class UserList
{
    public User[] users;
}

// Modelo del personaje que viene de la API de Rick and Morty
// Solo definimos los campos que necesitamos extraer
[System.Serializable]
public class Character
{
    public int id;
    public string name;
    public string species;
    public string image;
}