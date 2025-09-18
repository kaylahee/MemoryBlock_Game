using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
	public TextMeshProUGUI scoreText;
	private int score = 0;             

	void Start()
	{
		UpdateScoreUI();
	}

	public void AddScore(int value)
	{
		score += value;
		UpdateScoreUI();
	}

	private void UpdateScoreUI()
	{
		scoreText.text = "SCORE: " + score.ToString();
	}
}
