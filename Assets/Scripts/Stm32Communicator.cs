using UnityEngine;
using System.IO.Ports;
using System.Threading;

public class Stm32Communicator : MonoBehaviour
{
	// === 환경 설정 변수 ===
	[Tooltip("PC에 연결된 STM32의 COM 포트 번호 (예: COM3, COM4)")]
	public string portName = "COM5";

	[Tooltip("STM32와 PC가 통신하는 속도 (STM32 펌웨어 설정과 일치해야 함)")]
	public int baudRate = 115200;

	// === 내부 변수 ===
	private SerialPort serialPort;
	private Thread readThread;
	private bool isRunning = false;
	private string receivedData = "";

	public BlockPuzzleGame bpg;
	public int hp = 3;

	// === Unity 생명주기 ===

	void Start()
	{
		ConnectToStm32();
	}

	void OnApplicationQuit()
	{
		DisconnectFromStm32();
	}

	// === 통신 관련 함수 ===

	/// <summary>
	/// STM32와의 시리얼 통신을 시작합니다.
	/// </summary>
	void ConnectToStm32()
	{
		try
		{
			serialPort = new SerialPort(portName, baudRate);
			serialPort.Open();
			serialPort.ReadTimeout = 100; // 읽기 타임아웃 설정

			isRunning = true;
			Debug.Log($"<color=green>시리얼 포트 {portName} 열기 성공!</color>");

			// 데이터를 백그라운드에서 읽기 위한 스레드 시작
			readThread = new Thread(ReadSerialData);
			readThread.Start();
		}
		catch (System.Exception ex)
		{
			Debug.LogError($"<color=red>시리얼 포트 {portName} 연결 실패: {ex.Message}</color>");
		}
	}

	/// <summary>
	/// STM32와의 시리얼 통신을 종료합니다.
	/// </summary>
	void DisconnectFromStm32()
	{
		isRunning = false;

		// 스레드 종료
		if (readThread != null && readThread.IsAlive)
		{
			readThread.Join();
		}

		// 포트 닫기
		if (serialPort != null && serialPort.IsOpen)
		{
			serialPort.Close();
			Debug.Log("<color=orange>시리얼 포트 닫힘.</color>");
		}
	}

	/// <summary>
	/// 백그라운드에서 시리얼 데이터를 읽는 스레드 함수.
	/// </summary>
	void ReadSerialData()
	{
		while (isRunning)
		{
			try
			{
				string receivedData = serialPort.ReadExisting();
				if (!string.IsNullOrEmpty(receivedData))
				{
					receivedData = receivedData.Trim(); // \r\n 제거
					Debug.Log($"<color=blue>STM32로부터 데이터 수신: {receivedData}</color>");

					if (receivedData == "SUCCESS")
					{
						bpg.canGet = true;
						Debug.Log(bpg.canGet);
					}
					
					if (receivedData == "FAIL")
					{
						bpg.canGet = false;
						hp--;
						Debug.Log(bpg.canGet);
					}
				}
			}
			catch (System.TimeoutException)
			{
				// 데이터가 없을 때 발생하는 예외는 무시
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"<color=red>데이터 읽기 오류: {ex.Message}</color>");
			}
			Thread.Sleep(10); // 과도한 CPU 사용을 방지하기 위한 딜레이
		}
	}

	/// <summary>
	/// STM32로 데이터를 보냅니다.
	/// </summary>
	/// <param name="message">보낼 문자열</param>
	public void SendToStm32(string message)
	{
		if (serialPort != null && serialPort.IsOpen)
		{
			serialPort.WriteLine(message);
			Debug.Log($"<color=purple>STM32로 데이터 전송: {message}</color>");
		}
		else
		{
			Debug.LogWarning("시리얼 포트가 열려있지 않아 데이터를 보낼 수 없습니다.");
		}
	}

	// === 예시 함수 ===

	void Update()
	{
		// 'Space' 키를 누를 때마다 STM32로 "Hello" 메시지를 보냅니다.
		if (Input.GetKeyDown(KeyCode.Space))
		{
			SendToStm32("Hello\n");
		}
	}
}