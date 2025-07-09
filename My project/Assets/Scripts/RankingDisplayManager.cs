// RankingDisplayManager.cs (VERSÃO COM LIMITE DE 10 GARANTIDO)
using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using System.Linq;

public class RankingDisplayManager : MonoBehaviour
{
    [Header("UI do Ranking Visual")]
    public GameObject entradaRankingPrefab;
    public Transform containerDaLista;

    [Header("Feedback Visual")]
    public GameObject loadingIndicator;

    [Header("Sprites das Barras de Ranking")]
    public Sprite spriteOuro;
    public Sprite spritePrata;
    public Sprite spriteBronze;
    public Sprite spritePadrao;

    private DatabaseReference dbReference;
    private DatabaseReference scoresRef;

    void Start()
    {
        if (loadingIndicator != null) loadingIndicator.SetActive(true);
        if (containerDaLista != null) containerDaLista.gameObject.SetActive(false);

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            if (task.Result == DependencyStatus.Available)
            {
                dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                // <<< MUDANÇA: Escuta a base de "scores" inteira >>>
                scoresRef = dbReference.Child("scores");
                scoresRef.ValueChanged += HandleRankingChange;
            }
        });
    }

    void HandleRankingChange(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        if (args.Snapshot != null && args.Snapshot.Exists)
        {
            List<ScoreEntry> allScores = new List<ScoreEntry>();
            foreach (var childSnapshot in args.Snapshot.Children)
            {
                allScores.Add(JsonUtility.FromJson<ScoreEntry>(childSnapshot.GetRawJsonValue()));
            }

            // <<< CORREÇÃO PRINCIPAL: Ordena e limita a lista AQUI >>>
            List<ScoreEntry> top10 = allScores
                                        .OrderByDescending(s => s.score) // Ordena do maior para o menor
                                        .Take(10)                         // Pega apenas os 10 primeiros
                                        .ToList();                        // Converte de volta para uma lista

            UpdateRankingUI(top10);
        }
        else
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            if (containerDaLista != null) containerDaLista.gameObject.SetActive(true);
            foreach (Transform item in containerDaLista) { Destroy(item.gameObject); }
        }
    }

    void UpdateRankingUI(List<ScoreEntry> scores)
    {
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        if (containerDaLista != null) containerDaLista.gameObject.SetActive(true);

        foreach (Transform item in containerDaLista) { Destroy(item.gameObject); }

        // O loop agora só vai rodar para os 10 itens (ou menos)
        for (int i = 0; i < scores.Count; i++)
        {
            GameObject novaLinha = Instantiate(entradaRankingPrefab, containerDaLista);
            UnityEngine.UI.Image barraImagem = novaLinha.GetComponent<UnityEngine.UI.Image>();

            if (i == 0) barraImagem.sprite = spriteOuro;
            else if (i == 1) barraImagem.sprite = spritePrata;
            else if (i == 2) barraImagem.sprite = spriteBronze;
            else barraImagem.sprite = spritePadrao;

            TMP_Text textoNome = novaLinha.transform.Find("NomeJogador").GetComponent<TMP_Text>();
            TMP_Text textoPontuacao = novaLinha.transform.Find("Pontuacao").GetComponent<TMP_Text>();

            textoNome.text = scores[i].name;
            textoPontuacao.text = scores[i].score.ToString();
        }
    }

    void OnDestroy()
    {
        if (scoresRef != null)
        {
            scoresRef.ValueChanged -= HandleRankingChange;
        }
    }
}