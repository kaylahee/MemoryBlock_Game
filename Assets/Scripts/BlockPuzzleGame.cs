using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.HID;
using System.Collections;

public class BlockPuzzleGame : MonoBehaviour
{
	// === Public Variables ===
	public float cellSize = 0.7f; // �׸��� �� ũ��
	public int boardSize = 10; // ���� ũ�� (10x10)

	// === Prefabs and References ===
	public GameObject cellPrefab; // �׸��� �� ������
	public Transform boardHolder; // ���� ������ ���� �θ� ������Ʈ
	public GameObject[] blockPrefabs; // ��� ������ �迭

	public GameObject[] spawnBlockPosition;

	// === Private Variables ===
	private GameObject[,] grid; // ���� ���� �׸���
	private List<GameObject> currentBlocks = new List<GameObject>(); // ���� ������ ��� ����Ʈ

	private int score = 0;

	private GameObject selectedBlock;
	private Vector3 offset;
	private bool isDragging = false;

	public GameObject panel;
	public TextMeshProUGUI hpText;

	public ScoreManager scoreManager;
	public Stm32Communicator comportManager;

	public bool canGet = false;

	void Start()
	{
		InitializeGame();
		//SpawnBlocks();
	}

	void Update()
	{
		HandleInput();

		// === Game Over üũ ===
		if (currentBlocks.Count > 0)
		{
			CheckForLines();

			bool canPlaceAny = false;
			foreach (GameObject block in currentBlocks)
			{
				if (CanPlaceBlockAnywhere(block))
				{
					canPlaceAny = true;
					break;
				}
			}

			if (!canPlaceAny)
			{
				// Coroutine���� ���� ȣ��
				StartCoroutine(DelayedGameOver(1.5f)); // 1.5�� �� GameOver
			}
		}

		if (comportManager.hp == 0)
		{
			GameOver();
		}

		if (canGet)
		{
			SpawnBlocks();
			canGet = false;
		}

		
	}

	private IEnumerator DelayedGameOver(float delay)
	{
		yield return new WaitForSeconds(delay);

		GameOver(); // ���� ���� ó�� (�� ��ȯ ��)
	}

	/// <summary>
	/// ���� �ʱ�ȭ: ���� ����
	/// </summary>
	void InitializeGame()
	{
		grid = new GameObject[boardSize, boardSize];

		for (int y = 0; y < boardSize; y++)
		{
			for (int x = 0; x < boardSize; x++)
			{
				Vector3 position = new Vector3(x * cellSize, y * cellSize, 0);
				GameObject cell = Instantiate(cellPrefab, position, Quaternion.identity);
				cell.transform.SetParent(boardHolder);
				cell.name = $"Cell ({x},{y})";
				grid[x, y] = cell;
			}
		}

		boardHolder.transform.position = new Vector3(-5, -2, 0);
		Debug.Log("���� ���� ���� �Ϸ�.");
	}

	public void ReSpawnBlocks()
	{
		currentBlocks.Clear();
		// �����ϰ� ������ �迭���� ����� �����ͼ� ����
		for (int i = 0; i < spawnBlockPosition.Length; i++) // 3���� ����� ����
		{
			Destroy(spawnBlockPosition[i].transform.GetChild(0).gameObject);
		}

		for (int i = 0; i < spawnBlockPosition.Length; i++) // 3���� ����� ����
		{
			if (blockPrefabs.Length > 0)
			{
				int randomIndex = Random.Range(0, blockPrefabs.Length);
				GameObject newBlock = Instantiate(blockPrefabs[randomIndex]);
				newBlock.transform.position = spawnBlockPosition[i].transform.position;
				newBlock.transform.SetParent(spawnBlockPosition[i].transform);
				currentBlocks.Add(newBlock);

				// ��Ͽ� �巡�� ��ũ��Ʈ �߰�
				if (newBlock.GetComponent<BlockDragger>() == null)
				{
					BlockDragger dragger = newBlock.AddComponent<BlockDragger>();
					dragger.gameManager = this;
				}
			}
		}
	}

	/// <summary>
	/// ��� ����
	/// </summary>

	void SpawnBlocks()
	{
		for (int i = 0; i < spawnBlockPosition.Length; i++)
		{
			if (spawnBlockPosition[i].transform.childCount == 0)
			{
				int randomIndex = Random.Range(0, blockPrefabs.Length);
				GameObject newBlock = Instantiate(blockPrefabs[randomIndex]);
				newBlock.transform.position = spawnBlockPosition[i].transform.position;
				newBlock.transform.SetParent(spawnBlockPosition[i].transform);
				currentBlocks.Add(newBlock);

				if (newBlock.GetComponent<BlockDragger>() == null)
				{
					BlockDragger dragger = newBlock.AddComponent<BlockDragger>();
					dragger.gameManager = this;
				}
			}
		}

		Debug.Log("��� ���� �Ϸ�.");
	}

	/// <summary>
	/// ���콺 �Է� �ڵ鸵
	/// </summary>
	void HandleInput()
	{
		if (isDragging && selectedBlock != null)
		{
			Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			selectedBlock.transform.position = new Vector3(mousePosition.x - offset.x, mousePosition.y - offset.y, 0);
		}
	}

	Transform pre_parent;
	Color pre_color;
	Vector3 pre_position;

	/// <summary>
	/// ����� �������� �� ȣ��
	/// </summary>
	public void OnBlockSelected(GameObject block)
	{
		selectedBlock = block;
		isDragging = true;

		pre_position = selectedBlock.transform.position;
		pre_color = selectedBlock.transform.GetComponent<SpriteRenderer>().color;
		pre_parent = selectedBlock.transform.parent;

		offset = Camera.main.ScreenToWorldPoint(Input.mousePosition) - selectedBlock.transform.position;
		selectedBlock.transform.GetComponent<SpriteRenderer>().color = Color.gray;
	}

	/// <summary>
	/// ��� ��� �� ȣ��
	/// </summary>
	public void OnBlockDropped()
	{

		isDragging = false;
		selectedBlock.transform.GetComponent<SpriteRenderer>().color = Color.white;

		if (CanPlaceBlock(selectedBlock))
		{
			PlaceBlock(selectedBlock);
			currentBlocks.Remove(selectedBlock);
			CheckForLines();
			Debug.Log($"����� ���忡 ��ġ�Ǿ����ϴ�. ���� ����: {score}");
		}
		else
		{
			// ��ġ ���� �� ���� ��ġ�� �ǵ����� / ���� ������ ��������
			selectedBlock.transform.position = pre_position;
			selectedBlock.transform.SetParent(pre_parent);
			selectedBlock.transform.GetComponent<SpriteRenderer>().color = pre_color;

			Debug.Log("����� ���� �� ���� ��ġ�Դϴ�.");
		}

		selectedBlock = null;
	}

	/// <summary>
	/// ����� ���� �� �ִ��� �Ǵ�
	/// </summary>
	bool CanPlaceBlock(GameObject block)
	{
		block.transform.SetParent(boardHolder.transform);

		//Debug.Log(block.transform.localPosition);

		// ���� ����� ��ġ�� �˻�
		Vector3 blockPos = block.transform.localPosition;
		int x = Mathf.RoundToInt(blockPos.x / cellSize);
		int y = Mathf.RoundToInt(blockPos.y / cellSize);

		// ���� ������ ����ų� �̹� �ٸ� ����� �ִ� ���
		if (x < 0 || x >= boardSize || y < 0 || y >= boardSize)
		{
			return false;
		}

		if (block.name.Contains("T"))
		{
			if (x + 1 < 0 || x + 1 >= boardSize || y + 1 < 0 || y + 1 >= boardSize)
			{
				return false;
			}
			if (x - 1 < 0 || x - 1 >= boardSize)
			{
				return false;
			}

			if (grid[x, y].GetComponent<CellState>().Colored || grid[x + 1, y].GetComponent<CellState>().Colored
				|| grid[x - 1, y].GetComponent<CellState>().Colored || grid[x, y + 1].GetComponent<CellState>().Colored)
			{
				return false;
			}
		}
		else if (block.name.Contains("Z"))
		{
			if (x + 1 < 0 || x + 1 >= boardSize || y + 1 < 0 || y + 1 >= boardSize)
			{
				return false;
			}
			if (x - 1 < 0 || x - 1 >= boardSize || y - 1 < 0 || y - 1 >= boardSize)
			{
				return false;
			}

			if (grid[x, y].GetComponent<CellState>().Colored || grid[x - 1, y].GetComponent<CellState>().Colored
				|| grid[x + 1, y - 1].GetComponent<CellState>().Colored || grid[x, y - 1].GetComponent<CellState>().Colored)
			{
				return false;
			}
		}
		else if (block.name.Contains("S"))
		{
			if (x + 1 < 0 || x + 1 >= boardSize || y + 1 < 0 || y + 1 >= boardSize)
			{
				return false;
			}
			if (x - 1 < 0 || x - 1 >= boardSize)
			{
				return false;
			}

			if (grid[x, y].GetComponent<CellState>().Colored || grid[x + 1, y + 1].GetComponent<CellState>().Colored
				|| grid[x - 1, y].GetComponent<CellState>().Colored || grid[x, y + 1].GetComponent<CellState>().Colored)
			{
				return false;
			}
		}
		else if (block.name.Contains("L"))
		{
			if (x + 1 < 0 || x + 1 >= boardSize || y + 1 < 0 || y + 1 >= boardSize)
			{
				return false;
			}
			if (x - 1 < 0 || x - 1 >= boardSize)
			{
				return false;
			}

			if (grid[x, y].GetComponent<CellState>().Colored || grid[x + 1, y].GetComponent<CellState>().Colored
				|| grid[x - 1, y].GetComponent<CellState>().Colored || grid[x + 1, y + 1].GetComponent<CellState>().Colored)
			{
				return false;
			}
		}
		else if (block.name.Contains("J"))
		{
			if (x + 1 < 0 || x + 1 >= boardSize || y + 1 < 0 || y + 1 >= boardSize)
			{
				return false;
			}
			if (x - 1 < 0 || x - 1 >= boardSize)
			{
				return false;
			}

			if (grid[x, y].GetComponent<CellState>().Colored || grid[x + 1, y].GetComponent<CellState>().Colored
				|| grid[x - 1, y].GetComponent<CellState>().Colored || grid[x - 1, y + 1].GetComponent<CellState>().Colored)
			{
				return false;
			}
		}
		else if (block.name.Contains("O"))
		{
			if (y + 1 < 0 || y + 1 >= boardSize)
			{
				return false;
			}
			if (x - 1 < 0 || x - 1 >= boardSize)
			{
				return false;
			}

			if (grid[x, y].GetComponent<CellState>().Colored || grid[x - 1, y].GetComponent<CellState>().Colored
				|| grid[x - 1, y + 1].GetComponent<CellState>().Colored || grid[x, y + 1].GetComponent<CellState>().Colored)
			{
				return false;
			}
		}
		else if (block.name.Contains("I"))
		{
			if (y + 1 < 0 || y + 1 >= boardSize)
			{
				return false;
			}
			if (y - 1 < 0 || y - 1 >= boardSize)
			{
				return false;
			}
			if (y - 2 < 0 || y - 2 >= boardSize)
			{
				return false;
			}

			if (grid[x, y].GetComponent<CellState>().Colored || grid[x, y + 1].GetComponent<CellState>().Colored
				|| grid[x, y - 1].GetComponent<CellState>().Colored || grid[x, y - 2].GetComponent<CellState>().Colored)
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// ����� ���忡 ����
	/// </summary>
	void PlaceBlock(GameObject block)
	{
		block.transform.SetParent(boardHolder.transform);

		Debug.Log(block.transform.localPosition);

		// ���� ����� ��ġ�� �˻�
		Vector3 blockPos = block.transform.localPosition;
		int x = Mathf.RoundToInt(blockPos.x / cellSize);
		int y = Mathf.RoundToInt(blockPos.y / cellSize);

		// �ش� ��ġ�� �� GameObject�� ����
		// ���� ��������Ʈ ������ Ȱ��ȭ
		Color darkGreen = new Color32(0, 100, 0, 255);

		if (block.name.Contains("T"))
		{
			grid[x, y].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x + 1, y].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x - 1, y].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x, y + 1].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x, y].GetComponent<CellState>().Colored = true;
			grid[x + 1, y].GetComponent<CellState>().Colored = true;
			grid[x - 1, y].GetComponent<CellState>().Colored = true;
			grid[x, y + 1].GetComponent<CellState>().Colored = true;
		}
		else if (block.name.Contains("Z"))
		{
			grid[x, y].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x - 1, y].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x + 1, y - 1].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x, y - 1].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x, y].GetComponent<CellState>().Colored = true;
			grid[x - 1, y].GetComponent<CellState>().Colored = true;
			grid[x + 1, y - 1].GetComponent<CellState>().Colored = true;
			grid[x, y - 1].GetComponent<CellState>().Colored = true;
		}
		else if (block.name.Contains("S"))
		{
			grid[x, y].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x + 1, y + 1].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x - 1, y].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x, y + 1].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x, y].GetComponent<CellState>().Colored = true;
			grid[x + 1, y + 1].GetComponent<CellState>().Colored = true;
			grid[x - 1, y].GetComponent<CellState>().Colored = true;
			grid[x, y + 1].GetComponent<CellState>().Colored = true;
		}
		else if (block.name.Contains("L"))
		{
			grid[x, y].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x + 1, y].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x - 1, y].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x + 1, y + 1].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x, y].GetComponent<CellState>().Colored = true;
			grid[x + 1, y].GetComponent<CellState>().Colored = true;
			grid[x - 1, y].GetComponent<CellState>().Colored = true;
			grid[x + 1, y + 1].GetComponent<CellState>().Colored = true;
		}
		else if (block.name.Contains("J"))
		{
			grid[x, y].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x + 1, y].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x - 1, y].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x - 1, y + 1].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x, y].GetComponent<CellState>().Colored = true;
			grid[x + 1, y].GetComponent<CellState>().Colored = true;
			grid[x - 1, y].GetComponent<CellState>().Colored = true;
			grid[x - 1, y + 1].GetComponent<CellState>().Colored = true;
		}
		else if (block.name.Contains("O"))
		{
			grid[x, y].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x - 1, y].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x - 1, y + 1].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x, y + 1].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x, y].GetComponent<CellState>().Colored = true;
			grid[x - 1, y].GetComponent<CellState>().Colored = true;
			grid[x - 1, y + 1].GetComponent<CellState>().Colored = true;
			grid[x, y + 1].GetComponent<CellState>().Colored = true;
		}
		else if (block.name.Contains("I"))
		{
			grid[x, y].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x, y + 1].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x, y - 1].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x, y - 2].GetComponent<SpriteRenderer>().color = darkGreen;
			grid[x, y].GetComponent<CellState>().Colored = true;
			grid[x, y + 1].GetComponent<CellState>().Colored = true;
			grid[x, y - 1].GetComponent<CellState>().Colored = true;
			grid[x, y - 2].GetComponent<CellState>().Colored = true;
		}

		Destroy(block);
	}

	/// <summary>
	/// �ϼ��� ����/���� ���� �ִ��� Ȯ���ϰ� ����
	/// </summary>
	void CheckForLines()
	{
		List<int> completedRows = new List<int>();
		List<int> completedCols = new List<int>();

		// ������ Ȯ��
		for (int y = 0; y < boardSize; y++)
		{
			bool isRowFull = true;
			for (int x = 0; x < boardSize; x++)
			{
				if (grid[x, y].GetComponent<CellState>().Colored == false)
				{
					isRowFull = false;
					break;
				}
			}
			if (isRowFull) completedRows.Add(y);
		}

		// ������ Ȯ��
		for (int x = 0; x < boardSize; x++)
		{
			bool isColFull = true;
			for (int y = 0; y < boardSize; y++)
			{
				if (grid[x, y].GetComponent<CellState>().Colored == false)
				{
					isColFull = false;
					break;
				}
			}
			if (isColFull) completedCols.Add(x);
		}

		// �� ���� �� ���� �߰�
		foreach (int y in completedRows)
		{
			for (int x = 0; x < boardSize; x++)
			{
				grid[x, y].GetComponent<CellState>().Colored = false;
				grid[x, y].GetComponent<SpriteRenderer>().color = Color.white;
			}
			score += 10;
		}

		foreach (int x in completedCols)
		{
			for (int y = 0; y < boardSize; y++)
			{
				grid[x, y].GetComponent<CellState>().Colored = false;
				grid[x, y].GetComponent<SpriteRenderer>().color = Color.white;
			}
			score += 10;
		}

		if (completedRows.Count > 0 || completedCols.Count > 0)
		{
			scoreManager.AddScore(10);
			Debug.Log($"�� ����! ���� ����: {score}");
		}
	}

	/// <summary>
	/// ���� ���忡 �ش� ����� ���� �� �ִ��� �˻�
	/// </summary>
	bool CanPlaceBlockAnywhere(GameObject blockPrefab)
	{
		// ����� ������ �������� �ʰ�, ��ġ�� �������� üũ
		for (int y = 0; y < boardSize; y++)
		{
			for (int x = 0; x < boardSize; x++)
			{
				if (CanFitAtPosition(blockPrefab, x, y))
					return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Ư�� ��ǥ(x,y)�� blockPrefab ����� �� �� �ִ��� Ȯ��
	/// </summary>
	bool CanFitAtPosition(GameObject block, int x, int y)
	{
		Vector3 blockPos = block.transform.localPosition;

		// ���� ������ ����ų� �̹� �ٸ� ����� �ִ� ���
		if (x < 0 || x >= boardSize || y < 0 || y >= boardSize)
		{
			return false;
		}

		if (block.name.Contains("T"))
		{
			if (x + 1 < 0 || x + 1 >= boardSize || y + 1 < 0 || y + 1 >= boardSize)
			{
				return false;
			}
			if (x - 1 < 0 || x - 1 >= boardSize)
			{
				return false;
			}

			if (grid[x, y].GetComponent<CellState>().Colored || grid[x + 1, y].GetComponent<CellState>().Colored
				|| grid[x - 1, y].GetComponent<CellState>().Colored || grid[x, y + 1].GetComponent<CellState>().Colored)
			{
				return false;
			}
		}
		else if (block.name.Contains("Z"))
		{
			if (x + 1 < 0 || x + 1 >= boardSize || y + 1 < 0 || y + 1 >= boardSize)
			{
				return false;
			}
			if (x - 1 < 0 || x - 1 >= boardSize || y - 1 < 0 || y - 1 >= boardSize)
			{
				return false;
			}

			if (grid[x, y].GetComponent<CellState>().Colored || grid[x - 1, y].GetComponent<CellState>().Colored
				|| grid[x + 1, y - 1].GetComponent<CellState>().Colored || grid[x, y - 1].GetComponent<CellState>().Colored)
			{
				return false;
			}
		}
		else if (block.name.Contains("S"))
		{
			if (x + 1 < 0 || x + 1 >= boardSize || y + 1 < 0 || y + 1 >= boardSize)
			{
				return false;
			}
			if (x - 1 < 0 || x - 1 >= boardSize)
			{
				return false;
			}

			if (grid[x, y].GetComponent<CellState>().Colored || grid[x + 1, y + 1].GetComponent<CellState>().Colored
				|| grid[x - 1, y].GetComponent<CellState>().Colored || grid[x, y + 1].GetComponent<CellState>().Colored)
			{
				return false;
			}
		}
		else if (block.name.Contains("L"))
		{
			if (x + 1 < 0 || x + 1 >= boardSize || y + 1 < 0 || y + 1 >= boardSize)
			{
				return false;
			}
			if (x - 1 < 0 || x - 1 >= boardSize)
			{
				return false;
			}

			if (grid[x, y].GetComponent<CellState>().Colored || grid[x + 1, y].GetComponent<CellState>().Colored
				|| grid[x - 1, y].GetComponent<CellState>().Colored || grid[x + 1, y + 1].GetComponent<CellState>().Colored)
			{
				return false;
			}
		}
		else if (block.name.Contains("J"))
		{
			if (x + 1 < 0 || x + 1 >= boardSize || y + 1 < 0 || y + 1 >= boardSize)
			{
				return false;
			}
			if (x - 1 < 0 || x - 1 >= boardSize)
			{
				return false;
			}

			if (grid[x, y].GetComponent<CellState>().Colored || grid[x + 1, y].GetComponent<CellState>().Colored
				|| grid[x - 1, y].GetComponent<CellState>().Colored || grid[x - 1, y + 1].GetComponent<CellState>().Colored)
			{
				return false;
			}
		}
		else if (block.name.Contains("O"))
		{
			if (y + 1 < 0 || y + 1 >= boardSize)
			{
				return false;
			}
			if (x - 1 < 0 || x - 1 >= boardSize)
			{
				return false;
			}

			if (grid[x, y].GetComponent<CellState>().Colored || grid[x - 1, y].GetComponent<CellState>().Colored
				|| grid[x - 1, y + 1].GetComponent<CellState>().Colored || grid[x, y + 1].GetComponent<CellState>().Colored)
			{
				return false;
			}
		}
		else if (block.name.Contains("I"))
		{
			if (y + 1 < 0 || y + 1 >= boardSize)
			{
				return false;
			}
			if (y - 1 < 0 || y - 1 >= boardSize)
			{
				return false;
			}
			if (y - 2 < 0 || y - 2 >= boardSize)
			{
				return false;
			}

			if (grid[x, y].GetComponent<CellState>().Colored || grid[x, y + 1].GetComponent<CellState>().Colored
				|| grid[x, y - 1].GetComponent<CellState>().Colored || grid[x, y - 2].GetComponent<CellState>().Colored)
			{
				return false;
			}
		}

		// ���� ����ϸ� ����
		return true;
	}


	/// <summary>
	/// ���� ���� ó��
	/// </summary>
	void GameOver()
	{
		if (comportManager.hp == 0)
		{
			hpText.text = "YOUR HP IS 0";
		}
		panel.SetActive(true);
	}

	public void OnClickRetry()
	{
		// GameOver ������ �̵�
		SceneManager.LoadScene("StartScene");
	}

	public void OnClickExit()
	{
		// ���α׷� ����
		Application.Quit();
	}
}
