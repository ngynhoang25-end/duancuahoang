using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{
    public GameObject cardPrefab;
    public Transform gridParent;
    public bool canFlip = true;

    private Card firstCard, secondCard;
    private int matchesFound = 0;

    private string[,] mathData = {
        {"15 / 3", "5"}, {"8 x 2", "16"}, {"9 + 7", "16"}, {"20 - 5", "15"},
        {"6 x 3", "18"}, {"25 / 5", "5"}, {"14 + 6", "20"}, {"30 - 10", "20"}
    };

    void Start() { GenerateLevel(); }

    void GenerateLevel()
    {
        // Xóa các thẻ cũ nếu có để tránh rác
        foreach (Transform child in gridParent) Destroy(child.gameObject);

        List<KeyValuePair<int, string>> deck = new List<KeyValuePair<int, string>>();
        for (int i = 0; i < 8; i++)
        {
            deck.Add(new KeyValuePair<int, string>(i, mathData[i, 0]));
            deck.Add(new KeyValuePair<int, string>(i, mathData[i, 1]));
        }

        // Shuffle xịn
        for (int i = 0; i < deck.Count; i++)
        {
            int rnd = Random.Range(i, deck.Count);
            var temp = deck[i]; deck[i] = deck[rnd]; deck[rnd] = temp;
        }

        foreach (var item in deck)
        {
            GameObject newObj = Instantiate(cardPrefab, gridParent);
            newObj.GetComponent<Card>().SetupCard(item.Key, item.Value, this);
        }
    }

    public void OnCardFlipped(Card card)
    {
        if (firstCard == null)
        {
            firstCard = card;
            Debug.Log("Thẻ 1: " + card.valueText.text + " (ID: " + card.cardValue + ")");
        }
        else
        {
            secondCard = card;
            Debug.Log("Thẻ 2: " + card.valueText.text + " (ID: " + card.cardValue + ")");
            StartCoroutine(CheckMatch());
        }
    }

    IEnumerator CheckMatch()
    {
        canFlip = false;
        yield return new WaitForSeconds(0.6f);

        if (firstCard.cardValue == secondCard.cardValue)
        {
            Debug.Log("<color=cyan>KHỚP RỒI! Biến mất nào.</color>");
            firstCard.gameObject.SetActive(false);
            secondCard.gameObject.SetActive(false);
            matchesFound++;
            if (matchesFound == 8) Debug.Log("WIN!");
        }
        else
        {
            Debug.Log("<color=red>SAI RỒI! Úp lại.</color>");
            firstCard.CloseCard();
            secondCard.CloseCard();
        }

        firstCard = null;
        secondCard = null;
        canFlip = true;
    }
}