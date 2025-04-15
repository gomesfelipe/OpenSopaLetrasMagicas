using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Pool;
using DG.Tweening;

public class CauldronManager : MonoBehaviour
{
    [Header("Configurações")]
    [SerializeField] private GameObject letterButtonPrefab;
    [SerializeField] private RectTransform spawnArea;
    [SerializeField] private int baseWrongLetters = 5;
    [SerializeField] private int extraWrongPerLetter = 1;
    [SerializeField] private float spawnPadding = 40f;

    private List<Rect> occupiedAreas = new();
    private List<LetterButton> activeLetters = new();
    [SerializeField] private string currentWord;

    private ObjectPool<LetterButton> letterPool;

    private void Awake()
    {
        letterPool = new ObjectPool<LetterButton>(
            createFunc: () =>
            {
                GameObject obj = Instantiate(letterButtonPrefab, spawnArea);
                return obj.GetComponent<LetterButton>();
            },
            actionOnGet: (btn) => btn.gameObject.SetActive(true),
            actionOnRelease: (btn) => btn.ResetButton(),
            actionOnDestroy: (btn) => Destroy(btn.gameObject),
            collectionCheck: false,
            defaultCapacity: 30
        );
    }

    private void OnEnable()
    {
        WordManager.OnNewWord += SetupNewWord;
    }

    private void OnDisable()
    {
        WordManager.OnNewWord -= SetupNewWord;
    }

    void SetupNewWord(string word)
    {
        currentWord = word.ToUpper();
        SpawnLetters();
    }

    void SpawnLetters()
    {
        // Libera todos os anteriores
        foreach (var btn in activeLetters)
        {
            btn.OnUsed -= HandleLetterUsed;
            letterPool.Release(btn);
        }

        activeLetters.Clear();
        occupiedAreas.Clear();

        HashSet<char> uniqueWordLetters = new(currentWord.ToCharArray());
        HashSet<char> finalLetters = new(uniqueWordLetters);

        int totalWrongLetters = baseWrongLetters + (currentWord.Length * extraWrongPerLetter);
        int totalDesired = uniqueWordLetters.Count + totalWrongLetters;

        string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        while (finalLetters.Count < totalDesired)
        {
            char c = alphabet[Random.Range(0, alphabet.Length)];
            if (!uniqueWordLetters.Contains(c))
                finalLetters.Add(c);
        }

        foreach (char letter in finalLetters)
        {
            LetterButton btn = letterPool.Get();
            RectTransform rectTransform = btn.GetComponent<RectTransform>();
            rectTransform.SetParent(spawnArea, false);

            Vector2 position;
            int attempts = 0;
            do
            {
                position = GetRandomPositionInsideArea();
                attempts++;
            } while (IsOverlapping(position, rectTransform.sizeDelta, spawnPadding) && attempts < 100);

            Rect rect = new Rect(position - rectTransform.sizeDelta / 2, rectTransform.sizeDelta);
            occupiedAreas.Add(rect);

            rectTransform.anchoredPosition = position;
            rectTransform.localScale = Vector3.zero;

            btn.Setup(letter);
            btn.OnUsed += HandleLetterUsed;

            activeLetters.Add(btn);
            rectTransform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        }
    }

    void HandleLetterUsed(LetterButton btn)
    {
        btn.OnUsed -= HandleLetterUsed;
        activeLetters.Remove(btn);
        letterPool.Release(btn);

        if (activeLetters.Count == 0)
        {
            WordManager.Instance.ForceNextWord();
        }
    }

    Vector2 GetRandomPositionInsideArea()
    {
        Vector2 min = spawnArea.rect.min + new Vector2(spawnPadding, spawnPadding);
        Vector2 max = spawnArea.rect.max - new Vector2(spawnPadding, spawnPadding);

        float x = Random.Range(min.x, max.x);
        float y = Random.Range(min.y, max.y);

        return new Vector2(x, y);
    }

    bool IsOverlapping(Vector2 pos, Vector2 size, float padding)
    {
        Rect newRect = new Rect(pos - size / 2f, size + new Vector2(padding, padding));
        foreach (Rect r in occupiedAreas)
        {
            if (r.Overlaps(newRect))
                return true;
        }
        return false;
    }
}
