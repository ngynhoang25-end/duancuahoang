using UnityEngine;
using TMPro;

public class Card : MonoBehaviour
{
    public TextMeshProUGUI valueText;
    public GameObject cardBack;
    [HideInInspector] public int cardValue;

    private GameController controller;
    private bool isFlipped = false;

    public void SetupCard(int val, string txt, GameController ctrl)
    {
        cardValue = val;
        valueText.text = txt;
        controller = ctrl;
        cardBack.SetActive(true);
        isFlipped = false; // Reset trạng thái
    }

    public void OnClickCard()
    {
        // Chặn click nếu: Thẻ đã lật, hoặc đang trong quá trình chờ kiểm tra
        if (isFlipped || !controller.canFlip) return;

        Flip();
        controller.OnCardFlipped(this);
    }

    public void Flip()
    {
        isFlipped = true;
        cardBack.SetActive(false);
    }

    public void CloseCard()
    {
        isFlipped = false;
        cardBack.SetActive(true);
    }
}