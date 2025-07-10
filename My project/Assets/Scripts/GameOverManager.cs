// GameOverManager.cs (VERSÃO COM INPUT FIELD)
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Essencial para o Input Field
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using System.Linq;

public class GameOverManager : MonoBehaviour
{
    [Header("UI do Game Over")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI scoreDisplayText;
    public GameObject nameEntryPanel;

    // <<< MUDANÇA PRINCIPAL: Adeus letras, olá Input Field! >>>
    public TMP_InputField nameInputField;
    public UnityEngine.UI.Button confirmButton;

    [Header("UI do Top 3 (Locais Fixos)")]
    public TextMeshProUGUI[] top3NameTexts;
    public TextMeshProUGUI[] top3ScoreTexts;

    [Header("Configurações")]
    public string menuSceneName = "Menu";

    // Variáveis internas
    private DatabaseReference dbReference;
    private int finalScore; // <<< NOVO: Variável para guardar a pontuação lida

    void Start()
    {
        // <<< MUDANÇA PRINCIPAL AQUI >>>
        // 1. Lê a pontuação que foi salva em PlayerPrefs.
        // O segundo parâmetro (0) é um valor padrão caso a chave "finalScore" não seja encontrada.
        finalScore = PlayerPrefs.GetInt("finalScore", 0);

        // 2. Atualiza o texto na tela com a pontuação lida.
        if (scoreDisplayText != null)
        {
            scoreDisplayText.text = finalScore.ToString();
        }

        gameOverPanel.SetActive(false);
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                LoadTop3AndStartGame();
            }
        });
        // <<< MUDANÇA: Conecta a função ao botão via código >>>
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(FinalizeNameEntry);
        }
    }

    

    // <<< REMOVIDO: O método Update() não é mais necessário para input >>>

    void StartNameEntry()
    {
        nameEntryPanel.SetActive(true);
        // Foca automaticamente no campo de texto para o jogador já poder digitar
        if (nameInputField != null)
        {
            nameInputField.Select();
            nameInputField.ActivateInputField();
        }
    }

    void FinalizeNameEntry()
    {
        // Pega o texto diretamente do Input Field
        string finalName = nameInputField.text;

        // Validação: não permite nome vazio
        if (string.IsNullOrWhiteSpace(finalName))
        {
            // Opcional: mostrar uma mensagem de erro ao jogador
            Debug.LogWarning("Nome não pode ser vazio!");
            return;
        }

        ScoreEntry newScore = new ScoreEntry(finalName, finalScore);

        string key = dbReference.Child("scores").Push().Key;

        dbReference.Child("scores").Child(key).SetRawJsonValueAsync(JsonUtility.ToJson(newScore)).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                SceneManager.LoadScene(menuSceneName);
            }
        });
    }

    #region Código de Setup Inalterado
    void LoadTop3AndStartGame()
    {
        dbReference.Child("scores").OrderByChild("score").LimitToLast(3).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                List<ScoreEntry> top3 = new List<ScoreEntry>();
                if (task.Result.Exists) { foreach (var childSnapshot in task.Result.Children) { top3.Add(JsonUtility.FromJson<ScoreEntry>(childSnapshot.GetRawJsonValue())); } }
                top3.Reverse();
                UpdateTop3UI(top3);
            }
        });
    }

    void UpdateTop3UI(List<ScoreEntry> top3)
    {
        for (int i = 0; i < 3; i++)
        {
            if (i < top3.Count) { top3NameTexts[i].text = top3[i].name; top3ScoreTexts[i].text = top3[i].score.ToString(); }
            else { top3NameTexts[i].text = "---"; top3ScoreTexts[i].text = "---"; }
        }
    }

    void ShowGameOverScreen(int finalScore)
    {
        gameOverPanel.SetActive(true);
        if (scoreDisplayText != null) scoreDisplayText.text = finalScore.ToString();
        StartNameEntry();
    }
    #endregion
}

// <<< IMPORTANTE: A classe ScoreEntry >>>
// Você ainda precisa ter um arquivo separado "ScoreEntry.cs" com este conteúdo:
/*
[System.Serializable]
public class ScoreEntry
{
    public string name;
    public int score;

    public ScoreEntry(string name, int score)
    {
        this.name = name;
        this.score = score;
    }
}
*/