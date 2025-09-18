using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
	// 버튼에서 호출할 함수
	public void OnStartButtonClicked()
	{
		SceneManager.LoadScene("GameScene"); 
	}

	public void OnExitButtonClicked()
	{
		Application.Quit();
	}
}
