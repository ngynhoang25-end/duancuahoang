using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager
{
	private int score = 0;
	private int throws = 0;
	private int comboChain = 0;
	public static ScoreManager instance;

	public static ScoreManager GetInstance()
	{
		if (instance == null)
			instance = new ScoreManager();

		return instance;
	}

	public void UpdateScoreUI()
	{
		Text _score = GameObject.FindWithTag("Score").GetComponent<Text>();
		_score.text = $"Score: {score}";
	}

	public void AddScore(int score)
	{
		this.score += score;
		UpdateScoreUI();
	}

	public void AddScoreWithCombo(int baseScore)
	{
		this.score += baseScore * GetComboMultiplier();
		UpdateScoreUI();
	}

	public void RegisterClear()
	{
		comboChain++;
	}

	public void ResetCombo()
	{
		comboChain = 0;
	}

	public int GetComboMultiplier()
	{
		return Mathf.Max(1, comboChain + 1);
	}

	public int GetScore()
	{
		return score;
	}

	public void AddThrows()
	{
		throws++;
	}

	public int GetThrows()
	{
		return throws;
	}

	public void Reset()
	{
		score = 0;
		throws = 0;
		comboChain = 0;
		UpdateScoreUI();
	}
}
