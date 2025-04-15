using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using DG.Tweening;

public class LetterButton : MonoBehaviour
{
    [SerializeField] private TMP_Text letterText;
    private char letter;
    private Button button;
    private RectTransform rectTransform;
    private Tween floatingTween;

    public event Action<LetterButton> OnUsed;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
        rectTransform = GetComponent<RectTransform>();
    }

    public void Setup(char c)
    {
        letter = c;
        letterText.text = c.ToString();
        button.interactable = true;
        gameObject.SetActive(true);

        // Inicia o movimento de flutuação
        StartFloating();
    }

    public void ResetButton()
    {
        OnUsed = null;
        StopFloating();
        gameObject.SetActive(false);
    }

    private void OnClick()
    {
        WordManager.Instance.CheckLetter(letter);
        button.interactable = false;
        OnUsed?.Invoke(this);
    }

    private void StartFloating()
    {
        // Cancela se já estiver animando
        floatingTween?.Kill();

        float offset = 10f;
        float duration = UnityEngine.Random.Range(1.5f, 2.5f);

        // Flutuação leve no eixo Y (para cima e para baixo)
        floatingTween = rectTransform
            .DOAnchorPosY(rectTransform.anchoredPosition.y + offset, duration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    private void StopFloating()
    {
        floatingTween?.Kill();
        floatingTween = null;
    }
}
