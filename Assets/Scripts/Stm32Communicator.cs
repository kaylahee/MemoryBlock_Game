using UnityEngine;
using System.IO.Ports;
using System.Threading;

public class Stm32Communicator : MonoBehaviour
{
	// === ȯ�� ���� ���� ===
	[Tooltip("PC�� ����� STM32�� COM ��Ʈ ��ȣ (��: COM3, COM4)")]
	public string portName = "COM5";

	[Tooltip("STM32�� PC�� ����ϴ� �ӵ� (STM32 �߿��� ������ ��ġ�ؾ� ��)")]
	public int baudRate = 115200;

	// === ���� ���� ===
	private SerialPort serialPort;
	private Thread readThread;
	private bool isRunning = false;
	private string receivedData = "";

	public BlockPuzzleGame bpg;
	public int hp = 3;

	// === Unity �����ֱ� ===

	void Start()
	{
		ConnectToStm32();
	}

	void OnApplicationQuit()
	{
		DisconnectFromStm32();
	}

	// === ��� ���� �Լ� ===

	/// <summary>
	/// STM32���� �ø��� ����� �����մϴ�.
	/// </summary>
	void ConnectToStm32()
	{
		try
		{
			serialPort = new SerialPort(portName, baudRate);
			serialPort.Open();
			serialPort.ReadTimeout = 100; // �б� Ÿ�Ӿƿ� ����

			isRunning = true;
			Debug.Log($"<color=green>�ø��� ��Ʈ {portName} ���� ����!</color>");

			// �����͸� ��׶��忡�� �б� ���� ������ ����
			readThread = new Thread(ReadSerialData);
			readThread.Start();
		}
		catch (System.Exception ex)
		{
			Debug.LogError($"<color=red>�ø��� ��Ʈ {portName} ���� ����: {ex.Message}</color>");
		}
	}

	/// <summary>
	/// STM32���� �ø��� ����� �����մϴ�.
	/// </summary>
	void DisconnectFromStm32()
	{
		isRunning = false;

		// ������ ����
		if (readThread != null && readThread.IsAlive)
		{
			readThread.Join();
		}

		// ��Ʈ �ݱ�
		if (serialPort != null && serialPort.IsOpen)
		{
			serialPort.Close();
			Debug.Log("<color=orange>�ø��� ��Ʈ ����.</color>");
		}
	}

	/// <summary>
	/// ��׶��忡�� �ø��� �����͸� �д� ������ �Լ�.
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
					receivedData = receivedData.Trim(); // \r\n ����
					Debug.Log($"<color=blue>STM32�κ��� ������ ����: {receivedData}</color>");

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
				// �����Ͱ� ���� �� �߻��ϴ� ���ܴ� ����
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"<color=red>������ �б� ����: {ex.Message}</color>");
			}
			Thread.Sleep(10); // ������ CPU ����� �����ϱ� ���� ������
		}
	}

	/// <summary>
	/// STM32�� �����͸� �����ϴ�.
	/// </summary>
	/// <param name="message">���� ���ڿ�</param>
	public void SendToStm32(string message)
	{
		if (serialPort != null && serialPort.IsOpen)
		{
			serialPort.WriteLine(message);
			Debug.Log($"<color=purple>STM32�� ������ ����: {message}</color>");
		}
		else
		{
			Debug.LogWarning("�ø��� ��Ʈ�� �������� �ʾ� �����͸� ���� �� �����ϴ�.");
		}
	}

	// === ���� �Լ� ===

	void Update()
	{
		// 'Space' Ű�� ���� ������ STM32�� "Hello" �޽����� �����ϴ�.
		if (Input.GetKeyDown(KeyCode.Space))
		{
			SendToStm32("Hello\n");
		}
	}
}