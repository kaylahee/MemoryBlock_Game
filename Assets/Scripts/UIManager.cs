using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
	// ��ư���� ȣ���� �Լ�
	public void OnStartButtonClicked()
	{
		SceneManager.LoadScene("GameScene"); 
	}

	public void OnExitButtonClicked()
	{
		Application.Quit();
	}
}
