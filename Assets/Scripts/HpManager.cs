using TMPro;
using UnityEngine;

public class HpManager : MonoBehaviour
{
	public TextMeshProUGUI hpText;
	public Stm32Communicator comportManager;

	void Start()
	{
		//UpdateHpUI();
	}

	//public void SubHp(int value)
	//{
	//	hp -= value;
	//	UpdateHpUI();
	//}

	private void Update()
	{
		hpText.text = "HP: " + comportManager.hp.ToString();
	}
}
