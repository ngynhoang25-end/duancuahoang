using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
	private const string CurrentLevelKey = "BubbleShooterPro_CurrentLevel";
	private const string UnlockedLevelKey = "BubbleShooterPro_UnlockedLevel";

	#region Singleton
	public static LevelManager instance;

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		DontDestroyOnLoad(gameObject);
	}
	#endregion

	public Grid grid;
	public Transform bubblesArea;
	public List<GameObject> bubblesPrefabs;
	public GameObject specialBubblePrefab;
	public List<GameObject> bubblesInScene;
	public List<GameObject> levels;
	public List<string> colorsInScene;
	public int currentLevel = 0;
	public GameObject levelText;
	public float baseDropSpeed = 50f;
	public float dropSpeedIncreasePerLevel = 5f;
	public int baseSpecialBubbleCount = 1;
	public int specialBubbleCountPerLevel = 1;
	private int unlockedLevel = 0;

	private void Start()
	{
		grid = GetComponent<Grid>();
		LoadProgress();
	}

	public void NextLevel()
	{
		GameManager.instance.WinMenu.SetActive(false);
		UnlockLevel(currentLevel + 1);
		StartLevel(currentLevel + 1);
	}

	public void RestartLevel()
	{
		GameManager.instance.LoseMenu.SetActive(false);
		StartLevel(currentLevel);
	}

	public void StartNewGame()
	{
		GameManager.instance.startUI.SetActive(false);
		GameManager.instance.levelsUI.SetActive(true);
	}

	System.Collections.IEnumerator LoadLevel(int level)
	{
		yield return new WaitForSeconds(0.1f);

		ScoreManager.GetInstance().Reset();
		GameManager.instance.dropSpeed = baseDropSpeed + (level * dropSpeedIncreasePerLevel);
		GameObject levelToLoad = Instantiate(levels[level]);
		FillWithBubbles(levelToLoad, bubblesPrefabs);

		SnapChildrensToGrid(bubblesArea);
		InsertSpecialBubbles();
		UpdateListOfBubblesInScene();

		GameManager.instance.shootScript.CreateNewBubbles();
	}

	public void StartLevel(int level)
	{
		GameManager.instance.levelsUI.SetActive(false);
		if (level >= levels.Count)
			level = 0;
		if (level > unlockedLevel)
			UnlockLevel(level);

		currentLevel = level;
		SaveProgress();
		levelText.GetComponent<UnityEngine.UI.Text>().text = "Level " + (level + 1);
		StartCoroutine(LoadLevel(level));
	}

	public void InsertSpecialBubbles()
	{
		int specialCount = Mathf.Clamp(baseSpecialBubbleCount + (currentLevel * specialBubbleCountPerLevel), 1, 6);
		List<Transform> specials = new List<Transform>();
		for (int i = 0; i < specialCount; i++)
		{
			int randomBubble = Random.Range(0, bubblesArea.childCount);
			Transform bubble = bubblesArea.GetChild(randomBubble);

			if (!specials.Contains(bubble))
			{
				specials.Add(bubble);
				Instantiate(specialBubblePrefab, bubble.position, Quaternion.identity, bubblesArea);
				Destroy(bubble.gameObject);
			}
		}
	}

	public void ClearLevel()
	{
		foreach (Transform t in bubblesArea)
			Destroy(t.gameObject);
	}

	public int GetBubbleAreaChildCount()
	{
		return bubblesArea.childCount;
	}

	#region Snap to Grid
	private void SnapChildrensToGrid(Transform parent)
	{
		foreach (Transform t in parent)
		{
			SnapToNearestGripPosition(t);
		}
	}

	public void SnapToNearestGripPosition(Transform t)
	{
		Vector3Int cellPosition = grid.WorldToCell(t.position);
		t.position = grid.GetCellCenterWorld(cellPosition);
		t.rotation = Quaternion.identity;

	}
	#endregion

	private void FillWithBubbles(GameObject go, List<GameObject> _prefabs)
	{
		foreach (Transform t in go.transform)
		{
			var bubble = Instantiate(_prefabs[Random.Range(0, _prefabs.Count)], bubblesArea);
			bubble.transform.position = t.position;
		}

		Destroy(go);
	}

	public void UpdateListOfBubblesInScene()
	{
		List<string> colors = new List<string>();
		List<GameObject> newListOfBubbles = new List<GameObject>();

		foreach (Transform t in bubblesArea)
		{
			Bubble bubbleScript = t.GetComponent<Bubble>();
			if (colors.Count < bubblesPrefabs.Count && !colors.Contains(bubbleScript.bubbleColor.ToString()))
			{
				string color = bubbleScript.bubbleColor.ToString();

				foreach (GameObject prefab in bubblesPrefabs)
				{
					if (color.Equals(prefab.GetComponent<Bubble>().bubbleColor.ToString()))
					{
						colors.Add(color);
						newListOfBubbles.Add(prefab);
					}
				}
			}
		}

		colorsInScene = colors;
		bubblesInScene = newListOfBubbles;
	}

	public void SetAsBubbleAreaChild(Transform bubble)
	{
		SnapToNearestGripPosition(bubble);
		bubble.SetParent(bubblesArea);
	}

	private void LoadProgress()
	{
		if (levels == null || levels.Count == 0)
			return;

		currentLevel = Mathf.Clamp(PlayerPrefs.GetInt(CurrentLevelKey, 0), 0, levels.Count - 1);
		unlockedLevel = Mathf.Clamp(PlayerPrefs.GetInt(UnlockedLevelKey, 0), 0, levels.Count - 1);
	}

	private void SaveProgress()
	{
		PlayerPrefs.SetInt(CurrentLevelKey, currentLevel);
		PlayerPrefs.SetInt(UnlockedLevelKey, unlockedLevel);
		PlayerPrefs.Save();
	}

	private void UnlockLevel(int level)
	{
		if (levels == null || levels.Count == 0)
			return;

		int maxLevelIndex = levels.Count - 1;
		unlockedLevel = Mathf.Clamp(Mathf.Max(unlockedLevel, level), 0, maxLevelIndex);
		SaveProgress();
	}
}
