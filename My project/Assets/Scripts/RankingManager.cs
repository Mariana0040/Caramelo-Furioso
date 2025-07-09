// RankingManager.cs (VERSÃO FINAL CORRIGIDA E OTIMIZADA)
using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using System.Linq;

public class RankingManager : MonoBehaviour
{
    [Header("UI do Jogo")]
    public GameObject gameOverPanel;
    public GameObject nameEntryPanel;
    public TextMeshProUGUI[] nameLetters;
    public RectTransform cursor;
    // <<< ALTERAÇÃO 1: Adicionando o campo para o texto do score >>>
    public TextMeshProUGUI scoreDisplayText; // Arraste o texto do score aqui!

    [Header("UI do Ranking Visual")]
    public GameObject entradaRankingPrefab;
    public Transform containerDaLista;

    [Header("Sprites das Barras de Ranking")]
    public Sprite spriteOuro;
    public Sprite spritePrata;
    public Sprite spriteBronze;
    public Sprite spritePadrao;

    [Header("Configurações de Teste")]
    public int scoreParaTeste = 5000;

    // Variáveis internas
    private int playerScore;
    private DatabaseReference dbReference;
    private List<ScoreEntry> topScores = new List<ScoreEntry>();
    private bool isEnteringName = false;
    private int currentLetter = 0;
    private char[] currentName = { 'A', 'A', 'A' };
    private const string ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private bool isRankingLoaded = false;

    void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                LoadRanking(true); // Carrega o ranking e inicia o jogo
            }
            else { Debug.LogError($"Falha nas dependências do Firebase: {dependencyStatus}"); }
        });
    }

    public void ShowGameOver(int finalScore)
    {
        playerScore = finalScore;
        gameOverPanel.SetActive(true);

        // <<< ALTERAÇÃO 2: Atualiza o texto do score na tela de nome >>>
        if (scoreDisplayText != null)
        {
            scoreDisplayText.text = finalScore.ToString();
        }

        bool isHighScore = topScores.Count < 10 || finalScore > topScores.Last().score;

        if (isHighScore)
        {
            StartNameEntry();
        }
        else
        {
            nameEntryPanel.SetActive(false);
            UpdateRankingUI();
        }
    }

    // <<< ALTERAÇÃO 3: Lógica de atualização otimista >>>
    void FinalizeNameEntry()
    {
        isEnteringName = false;
        nameEntryPanel.SetActive(false);
        string finalName = new string(currentName);

        // 1. Cria a nova entrada de score
        ScoreEntry newScore = new ScoreEntry(finalName, playerScore);

        // 2. Salva no Firebase (isso vai rodar em segundo plano)
        string key = dbReference.Child("scores").Push().Key;
        dbReference.Child("scores").Child(key).SetRawJsonValueAsync(JsonUtility.ToJson(newScore));
        Debug.Log($"Salvando no Firebase: Nome='{finalName}', Pontuação={playerScore}");

        // 3. ATUALIZAÇÃO OTIMISTA: Adiciona na lista local e atualiza a UI imediatamente!
        topScores.Add(newScore);
        // Reordena a lista local pela pontuação, do maior para o menor
        topScores = topScores.OrderByDescending(s => s.score).ToList();
        // Garante que a lista não tenha mais de 10 entradas
        if (topScores.Count > 10)
        {
            topScores = topScores.GetRange(0, 10);
        }

        // 4. Mostra o resultado na tela na hora!
        UpdateRankingUI();
    }

    // <<< ALTERAÇÃO 4: Pequena mudança para carregar e iniciar o jogo >>>
    void LoadRanking(bool startGameOnLoad = false)
    {
        dbReference.Child("scores").OrderByChild("score").LimitToLast(10).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted) { return; }
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                topScores.Clear();
                if (snapshot.Exists) { foreach (var childSnapshot in snapshot.Children) { topScores.Add(JsonUtility.FromJson<ScoreEntry>(childSnapshot.GetRawJsonValue())); } }
                topScores.Reverse(); // Firebase retorna em ordem crescente, então revertemos
                isRankingLoaded = true;

                // Se for a primeira carga, inicia o "Game Over"
                if (startGameOnLoad)
                {
                    ShowGameOver(scoreParaTeste);
                }
                // Se o painel já estiver ativo, só atualiza a UI
                else if (gameOverPanel.activeSelf)
                {
                    UpdateRankingUI();
                }
            }
        });
    }

    void UpdateRankingUI()
    {
        if (containerDaLista == null) return;
        foreach (Transform item in containerDaLista) Destroy(item.gameObject);

        for (int i = 0; i < topScores.Count; i++)
        {
            GameObject novaLinha = Instantiate(entradaRankingPrefab, containerDaLista);
            UnityEngine.UI.Image barraImagem = novaLinha.GetComponent<UnityEngine.UI.Image>();

            if (i == 0) barraImagem.sprite = spriteOuro;
            else if (i == 1) barraImagem.sprite = spritePrata;
            else if (i == 2) barraImagem.sprite = spriteBronze;
            else barraImagem.sprite = spritePadrao;

            TMP_Text textoNome = novaLinha.transform.Find("NomeJogador").GetComponent<TMP_Text>();
            TMP_Text textoPontuacao = novaLinha.transform.Find("Pontuacao").GetComponent<TMP_Text>();

            textoNome.text = topScores[i].name;
            textoPontuacao.text = topScores[i].score.ToString();
        }
    }

    #region Código Inalterado (Controle de Nome)
    void OnGUI()
    {
        if (!isEnteringName) return;
        Event e = Event.current;
        if (e.type == EventType.KeyDown) HandleNameEntryInput(e.keyCode);
    }
    void HandleNameEntryInput(KeyCode key)
    {
        if (key == KeyCode.RightArrow) { currentLetter = (currentLetter + 1) % 3; UpdateCursorPosition(); }
        else if (key == KeyCode.LeftArrow) { currentLetter = (currentLetter - 1 + 3) % 3; UpdateCursorPosition(); }
        else if (key == KeyCode.UpArrow) { ChangeCharacter(1); }
        else if (key == KeyCode.DownArrow) { ChangeCharacter(-1); }
        else if (key == KeyCode.Space || key == KeyCode.Return) { FinalizeNameEntry(); }
    }
    void StartNameEntry()
    {
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        nameEntryPanel.SetActive(true);
        isEnteringName = true;
        currentLetter = 0;
        currentName = new char[] { 'A', 'A', 'A' };
        UpdateNameUI();
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
    [System.Serializable]
    public class ScoreEntry { public string name; public int score; public ScoreEntry(string name, int score) { this.name = name; this.score = score; } }
    #endregion
}