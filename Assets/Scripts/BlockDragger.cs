using UnityEngine;

public class BlockDragger : MonoBehaviour
{
	public BlockPuzzleGame gameManager;

	private void OnMouseDown()
	{
		Debug.Log("Click");
		gameManager.OnBlockSelected(gameObject);
	}

	private void OnMouseUp()
	{
		Debug.Log("Up");
		gameManager.OnBlockDropped();
	}
}
