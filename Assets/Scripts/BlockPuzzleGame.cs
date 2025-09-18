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
	public float cellSize = 0.7f; // 그리드 셀 크기
	public int boardSize = 10; // 보드 크기 (10x10)

	// === Prefabs and References ===
	public GameObject cellPrefab; // 그리드 셀 프리팹
	public Transform boardHolder; // 보드 셀들을 담을 부모 오브젝트
	public GameObject[] blockPrefabs; // 블록 프리팹 배열

	public GameObject[] spawnBlockPosition;

	// === Private Variables ===
	private GameObject[,] grid; // 게임 보드 그리드
	private List<GameObject> currentBlocks = new List<GameObject>(); // 현재 생성된 블록 리스트

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

		// === Game Over 체크 ===
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
				// Coroutine으로 지연 호출
				StartCoroutine(DelayedGameOver(1.5f)); // 1.5초 후 GameOver
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

		GameOver(); // 게임 오버 처리 (씬 전환 등)
	}

	/// <summary>
	/// 게임 초기화: 보드 생성
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
		Debug.Log("게임 보드 생성 완료.");
	}

	public void ReSpawnBlocks()
	{
		currentBlocks.Clear();
		// 간단하게 프리팹 배열에서 블록을 가져와서 생성
		for (int i = 0; i < spawnBlockPosition.Length; i++) // 3개의 블록을 생성
		{
			Destroy(spawnBlockPosition[i].transform.GetChild(0).gameObject);
		}

		for (int i = 0; i < spawnBlockPosition.Length; i++) // 3개의 블록을 생성
		{
			if (blockPrefabs.Length > 0)
			{
				int randomIndex = Random.Range(0, blockPrefabs.Length);
				GameObject newBlock = Instantiate(blockPrefabs[randomIndex]);
				newBlock.transform.position = spawnBlockPosition[i].transform.position;
				newBlock.transform.SetParent(spawnBlockPosition[i].transform);
				currentBlocks.Add(newBlock);

				// 블록에 드래그 스크립트 추가
				if (newBlock.GetComponent<BlockDragger>() == null)
				{
					BlockDragger dragger = newBlock.AddComponent<BlockDragger>();
					dragger.gameManager = this;
				}
			}
		}
	}

	/// <summary>
	/// 블록 생성
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

		Debug.Log("블록 생성 완료.");
	}

	/// <summary>
	/// 마우스 입력 핸들링
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
	/// 블록을 선택했을 때 호출
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
	/// 블록 드롭 시 호출
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
			Debug.Log($"블록이 보드에 배치되었습니다. 현재 점수: {score}");
		}
		else
		{
			// 배치 실패 시 원래 위치로 되돌리기 / 원래 색으로 돌려놓기
			selectedBlock.transform.position = pre_position;
			selectedBlock.transform.SetParent(pre_parent);
			selectedBlock.transform.GetComponent<SpriteRenderer>().color = pre_color;

			Debug.Log("블록을 놓을 수 없는 위치입니다.");
		}

		selectedBlock = null;
	}

	/// <summary>
	/// 블록을 놓을 수 있는지 판단
	/// </summary>
	bool CanPlaceBlock(GameObject block)
	{
		block.transform.SetParent(boardHolder.transform);

		//Debug.Log(block.transform.localPosition);

		// 단일 블록의 위치만 검사
		Vector3 blockPos = block.transform.localPosition;
		int x = Mathf.RoundToInt(blockPos.x / cellSize);
		int y = Mathf.RoundToInt(blockPos.y / cellSize);

		// 보드 범위를 벗어나거나 이미 다른 블록이 있는 경우
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
	/// 블록을 보드에 고정
	/// </summary>
	void PlaceBlock(GameObject block)
	{
		block.transform.SetParent(boardHolder.transform);

		Debug.Log(block.transform.localPosition);

		// 단일 블록의 위치만 검사
		Vector3 blockPos = block.transform.localPosition;
		int x = Mathf.RoundToInt(blockPos.x / cellSize);
		int y = Mathf.RoundToInt(blockPos.y / cellSize);

		// 해당 위치의 셀 GameObject에 접근
		// 셀의 스프라이트 렌더러 활성화
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
	/// 완성된 가로/세로 줄이 있는지 확인하고 제거
	/// </summary>
	void CheckForLines()
	{
		List<int> completedRows = new List<int>();
		List<int> completedCols = new List<int>();

		// 가로줄 확인
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

		// 세로줄 확인
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

		// 줄 제거 및 점수 추가
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
			Debug.Log($"줄 제거! 현재 점수: {score}");
		}
	}

	/// <summary>
	/// 현재 보드에 해당 블록을 놓을 수 있는지 검사
	/// </summary>
	bool CanPlaceBlockAnywhere(GameObject blockPrefab)
	{
		// 블록을 실제로 생성하지 않고, 위치만 가상으로 체크
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
	/// 특정 좌표(x,y)에 blockPrefab 모양이 들어갈 수 있는지 확인
	/// </summary>
	bool CanFitAtPosition(GameObject block, int x, int y)
	{
		Vector3 blockPos = block.transform.localPosition;

		// 보드 범위를 벗어나거나 이미 다른 블록이 있는 경우
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

		// 전부 통과하면 가능
		return true;
	}


	/// <summary>
	/// 게임 오버 처리
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
		// GameOver 씬으로 이동
		SceneManager.LoadScene("StartScene");
	}

	public void OnClickExit()
	{
		// 프로그램 종료
		Application.Quit();
	}
}
