// RankingManager.cs (VERS�O FINAL CORRIGIDA)
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using System.Linq;

public class RankingManager : MonoBehaviour
{
    [Header("UI do Jogo (Conforme sua Hierarquia)")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI rankingText;
    public GameObject nameEntryPanel;
    public TextMeshProUGUI[] nameLetters;
    public RectTransform cursor;

    [Header("UI de Teste")]
    public GameObject testPanel;
    public TMP_InputField scoreInputField;

    // ... (vari�veis internas permanecem as mesmas)
    private int playerScore;
    private DatabaseReference dbReference;
    private List<ScoreEntry> topScores = new List<ScoreEntry>();
    private bool isEnteringName = false;
    private int currentLetter = 0;
    private char[] currentName = { 'A', 'A', 'A' };
    private const string ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private bool isRankingLoaded = false;

    // O resto do script � o mesmo...
    void Start()
    {
        Debug.Log("RankingManager: Iniciando...");
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
#if UNITY_EDITOR
        if (testPanel != null) testPanel.SetActive(true);
#else
        if (testPanel != null) testPanel.SetActive(false);
#endif
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                LoadRanking();
            }
            else { Debug.LogError($"RankingManager: Falha nas depend�ncias do Firebase: {dependencyStatus}"); }
        });
    }

    void Update()
    {
        if (!isEnteringName) return;
        HandleNameEntryInput();
    }

    public void TestGameOverWithInputField()
    {
        if (int.TryParse(scoreInputField.text, out int scoreFromInput))
        {
            Debug.LogWarning($"--- MODO DE TESTE: Ativando Game Over com pontua��o: {scoreFromInput} ---");
            ShowGameOver(scoreFromInput);
        }
        else { Debug.LogError($"'{scoreInputField.text}' n�o � um n�mero v�lido!"); }
    }

    // =================================================================
    // AQUI EST� A CORRE��O
    // =================================================================
    public void ShowGameOver(int finalScore)
    {
        Debug.Log($"ShowGameOver chamado com a pontua��o: {finalScore}");
        playerScore = finalScore;

        if (testPanel != null) testPanel.SetActive(false);
        gameOverPanel.SetActive(true);

        // L�GICA DE VERIFICA��O REESCRITA E MAIS SEGURA
        bool isHighScore = false;
        if (topScores.Count < 10)
        {
            // Se houver menos de 10 scores, qualquer pontua��o nova entra no ranking.
            isHighScore = true;
            Debug.Log("Verifica��o: Menos de 10 scores no ranking. � um high score!");
        }
        else
        {
            // Se o ranking estiver cheio, compare com a menor pontua��o.
            int lowestScoreInTop10 = topScores.Last().score;
            Debug.Log($"Verifica��o: O ranking est� cheio. Comparando {finalScore} com a menor pontua��o {lowestScoreInTop10}.");
            if (finalScore > lowestScoreInTop10)
            {
                isHighScore = true;
            }
        }

        // Agora o resto da l�gica funciona como esperado
        if (isHighScore)
        {
            Debug.Log("Resultado: Pontua��o alta! Mostrando painel de nome.");
            StartNameEntry();
        }
        else
        {
            Debug.Log("Resultado: Pontua��o baixa. Escondendo painel de nome.");
            nameEntryPanel.SetActive(false);
        }
    }
    // =================================================================
    // FIM DA CORRE��O
    // =================================================================

    // O resto do script � o mesmo...
    #region Fun��es de Suporte
    void StartNameEntry()
    {
        nameEntryPanel.SetActive(true);
        isEnteringName = true;
        currentLetter = 0;
        currentName = new char[] { 'A', 'A', 'A' };
        UpdateNameUI();
    }
    void FinalizeNameEntry()
    {
        isEnteringName = false;
        nameEntryPanel.SetActive(false);
        string finalName = new string(currentName);
        ScoreEntry newScore = new ScoreEntry(finalName, playerScore);
        string key = dbReference.Child("scores").Push().Key;
        dbReference.Child("scores").Child(key).SetRawJsonValueAsync(JsonUtility.ToJson(newScore));
        Debug.Log($"Salvando no Firebase: Nome='{finalName}', Pontua��o={playerScore}");
        LoadRanking();
    }
    void HandleNameEntryInput()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow)) { currentLetter = (currentLetter + 1) % 3; UpdateCursorPosition(); }
        if (Input.GetKeyDown(KeyCode.LeftArrow)) { currentLetter = (currentLetter - 1 + 3) % 3; UpdateCursorPosition(); }
        if (Input.GetKeyDown(KeyCode.UpArrow)) { ChangeCharacter(1); }
        if (Input.GetKeyDown(KeyCode.DownArrow)) { ChangeCharacter(-1); }
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) { FinalizeNameEntry(); }
    }
    void LoadRanking()
    {
        Debug.Log("Carregando ranking do Firebase...");
        dbReference.Child("scores").OrderByChild("score").LimitToLast(10).GetValueAsync().ContinueWithOnMainThread(task => {
            if (task.IsFaulted) { Debug.LogError("Erro ao carregar o ranking: " + task.Exception); }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                topScores.Clear();
                if (snapshot.Exists)
                {
                    foreach (var childSnapshot in snapshot.Children) { topScores.Add(JsonUtility.FromJson<ScoreEntry>(childSnapshot.GetRawJsonValue())); }
                }
                topScores.Reverse();
                isRankingLoaded = true;
                Debug.Log($"Ranking carregado com sucesso! {topScores.Count} scores encontrados.");
                if (gameOverPanel.activeSelf) UpdateRankingUI();
            }
        });
    }
    void UpdateRankingUI()
    {
        rankingText.text = "RANKING\n\n";
        for (int i = 0; i < topScores.Count; i++) { rankingText.text += $"{i + 1}. {topScores[i].name} - {topScores[i].score}\n"; }
    }
    void ChangeCharacter(int direction)
    {
        int charIndex = ALPHABET.IndexOf(currentName[currentLetter]);
        charIndex = (charIndex + direction + ALPHABET.Length) % ALPHABET.Length;
        currentName[currentLetter] = ALPHABET[charIndex];
        UpdateNameUI();
    }
    void UpdateNameUI()
    {
        for (int i = 0; i < 3; i++) { nameLetters[i].text = currentName[i].ToString(); }
        UpdateCursorPosition();
    }
    void UpdateCursorPosition()
    {
        if (cursor == null || nameLetters[currentLetter] == null) return;
        cursor.position = new Vector3(
            nameLetters[currentLetter].transform.position.x,
            nameLetters[currentLetter].transform.position.y - (nameLetters[currentLetter].rectTransform.rect.height / 2) - 10f,
            nameLetters[currentLetter].transform.position.z
        );
    }
    #endregion
}