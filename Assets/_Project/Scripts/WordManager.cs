using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine.Networking;

public class WordManager : Singleton<WordManager>
{
    public static WordManager Instance;

    [SerializeField] private TMP_Text displayText;
    [SerializeField] private TMP_Text scoreText;

    private string currentWord;
    private char[] guessedLetters;
    private int score;

    private List<string> wordCache = new();
    private int currentCacheIndex = 0;
    private const int cacheSize = 20;

    public delegate void OnWordChanged(string fullWord);
    public static event OnWordChanged OnNewWord;

    private void Awake() => Instance = this;

    private void Start()
    {
        StartCoroutine(PreencherCacheDePalavras());
        UpdateScore(0);
    }

    void GenerateNewWord()
    {
        if (currentCacheIndex < wordCache.Count)
        {
            currentWord = wordCache[currentCacheIndex++].ToUpper();
            guessedLetters = new string('_', currentWord.Length).ToCharArray();

            displayText.text = string.Join(" ", guessedLetters);
            OnNewWord?.Invoke(currentWord);
        }
        else
        {
            StartCoroutine(PreencherCacheDePalavras());
        }
    }

    IEnumerator PreencherCacheDePalavras()
    {
        wordCache.Clear();
        currentCacheIndex = 0;

        while (wordCache.Count < cacheSize)
        {
            UnityWebRequest request = UnityWebRequest.Get("https://api.dicionario-aberto.net/random");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;

                string palavra = ExtrairPalavra(json);

                if (!string.IsNullOrEmpty(palavra) &&
                    palavra.Length >= 3 &&
                    palavra.Length <= 10 &&
                    palavra.All(char.IsLetter))
                {
                    wordCache.Add(palavra);
                }
            }
            else
            {
                Debug.LogError("Erro ao buscar palavra: " + request.error);
                yield break;
            }
        }

        GenerateNewWord();
    }

    string ExtrairPalavra(string json)
    {
        int start = json.IndexOf("\"word\":\"") + 8;
        int end = json.IndexOf("\"", start);
        return json.Substring(start, end - start);
    }

    public void CheckLetter(char letter)
    {
        bool found = false;

        for (int i = 0; i < currentWord.Length; i++)
        {
            if (currentWord[i] == letter && guessedLetters[i] == '_')
            {
                guessedLetters[i] = letter;
                found = true;
            }
        }

        if (found)
            UpdateScore(+10);
        else
            UpdateScore(-5);

        UpdateDisplay();

        if (new string(guessedLetters) == currentWord)
        {
            Invoke(nameof(GenerateNewWord), 1f);
        }
    }

    public void ForceNextWord()
    {
        GenerateNewWord();
    }

    void UpdateDisplay()
    {
        displayText.text = string.Join(" ", guessedLetters);
    }

    void UpdateScore(int delta)
    {
        score += delta;
        scoreText.text = "Score: " + score;
    }

    public void RestartGame()
    {
        score = 0;
        UpdateScore(0);
        GenerateNewWord();
    }

    public void GiveHint(int hintCost = 15)
    {
        if (score < hintCost)
            return;

        List<int> hiddenIndices = new();
        for (int i = 0; i < guessedLetters.Length; i++)
        {
            if (guessedLetters[i] == '_')
                hiddenIndices.Add(i);
        }

        if (hiddenIndices.Count == 0) return;

        int randomIndex = hiddenIndices[Random.Range(0, hiddenIndices.Count)];
        guessedLetters[randomIndex] = currentWord[randomIndex];

        UpdateScore(-hintCost);
        UpdateDisplay();

        if (new string(guessedLetters) == currentWord)
            Invoke(nameof(GenerateNewWord), 1f);
    }
}
