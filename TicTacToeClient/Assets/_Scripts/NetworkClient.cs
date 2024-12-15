using UnityEngine;
using UnityEngine.UI;
using Unity.Collections;
using Unity.Networking.Transport;
using System.Text;

public class NetworkClient : MonoBehaviour
{
    NetworkDriver networkDriver;
    NetworkConnection networkConnection;
    NetworkPipeline reliableAndInOrderPipeline;
    NetworkPipeline nonReliableNotInOrderedPipeline;

    const ushort NetworkPort = 9001;
    const string IPAddress = "127.0.0.1"; // Change to your server IP if needed

    public UnityEngine.UI.Text gameStateText; // Reference to a UI Text element for displaying game state
    private Button[,] gridButtons = new Button[3, 3]; // 3x3 grid of buttons

    private int playerNumber = 0; // Player number assigned by the server
    private int currentPlayer = 0; // Tracks whose turn it is

    void Start()
    {
        networkDriver = NetworkDriver.Create();
        reliableAndInOrderPipeline = networkDriver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));
        nonReliableNotInOrderedPipeline = networkDriver.CreatePipeline(typeof(FragmentationPipelineStage));

        networkConnection = default(NetworkConnection);
        NetworkEndpoint endpoint = NetworkEndpoint.Parse(IPAddress, NetworkPort, NetworkFamily.Ipv4);
        networkConnection = networkDriver.Connect(endpoint);

        InitializeGrid(); // Automatically find and assign buttons
    }

    void OnDestroy()
    {
        networkConnection.Disconnect(networkDriver);
        networkConnection = default(NetworkConnection);
        networkDriver.Dispose();
    }

    void Update()
    {
        networkDriver.ScheduleUpdate().Complete();

        if (!networkConnection.IsCreated)
        {
            UnityEngine.Debug.Log("Client is unable to connect to server");
            return;
        }

        DataStreamReader streamReader;
        NetworkPipeline pipelineUsedToSendEvent;
        NetworkEvent.Type networkEventType;

        while (PopNetworkEventAndCheckForData(out networkEventType, out streamReader, out pipelineUsedToSendEvent))
        {
            switch (networkEventType)
            {
                case NetworkEvent.Type.Connect:
                    UnityEngine.Debug.Log("Connected to the server");
                    break;

                case NetworkEvent.Type.Data:
                    int sizeOfDataBuffer = streamReader.ReadInt();
                    NativeArray<byte> buffer = new NativeArray<byte>(sizeOfDataBuffer, Allocator.Persistent);
                    streamReader.ReadBytes(buffer);
                    string msg = Encoding.Unicode.GetString(buffer.ToArray());
                    ProcessReceivedMsg(msg);
                    buffer.Dispose(); // Prevent memory leaks
                    break;

                case NetworkEvent.Type.Disconnect:
                    UnityEngine.Debug.Log("Disconnected from server");
                    networkConnection = default(NetworkConnection);
                    break;
            }
        }
    }

    private bool PopNetworkEventAndCheckForData(out NetworkEvent.Type networkEventType, out DataStreamReader streamReader, out NetworkPipeline pipelineUsedToSendEvent)
    {
        networkEventType = networkConnection.PopEvent(networkDriver, out streamReader, out pipelineUsedToSendEvent);

        if (networkEventType == NetworkEvent.Type.Empty)
            return false;

        return true;
    }

    private void ProcessReceivedMsg(string msg)
    {
        UnityEngine.Debug.Log("Message from server: " + msg);

        if (msg.StartsWith("PLAYER"))
        {
            playerNumber = int.Parse(msg.Split('|')[1]);
            UnityEngine.Debug.Log($"Assigned as Player {playerNumber}");
        }
        else if (msg.StartsWith("WIN"))
        {
            string winner = msg.Split('|')[1];
            gameStateText.text = $"Player {winner} wins!";
        }
        else if (msg == "DRAW")
        {
            gameStateText.text = "It's a draw!";
        }
        else
        {
            UpdateGridFromServer(msg); // Parse and update game state
        }
    }

    public void SendMoveToServer(int x, int y)
    {
        if (playerNumber == 0)
        {
            UnityEngine.Debug.Log("Player number not assigned yet. Cannot send move.");
            return;
        }

        if (playerNumber != currentPlayer)
        {
            UnityEngine.Debug.Log($"It's not your turn! Current player: {currentPlayer}, Your player number: {playerNumber}");
            return;
        }

        string msg = $"MOVE|{playerNumber}|{x}|{y}";
        byte[] msgAsByteArray = Encoding.Unicode.GetBytes(msg);
        NativeArray<byte> buffer = new NativeArray<byte>(msgAsByteArray, Allocator.Persistent);

        DataStreamWriter streamWriter;
        networkDriver.BeginSend(reliableAndInOrderPipeline, networkConnection, out streamWriter);
        streamWriter.WriteInt(buffer.Length);
        streamWriter.WriteBytes(buffer);
        networkDriver.EndSend(streamWriter);

        buffer.Dispose();
    }

    private void InitializeGrid()
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                string buttonName = $"Button_{i}_{j}";
                GameObject buttonObject = GameObject.Find(buttonName);

                if (buttonObject != null)
                {
                    gridButtons[i, j] = buttonObject.GetComponent<Button>();
                    int x = i, y = j; // Capture local variables for the closure
                    gridButtons[i, j].onClick.AddListener(() => OnCellClicked(x, y));
                }
                else
                {
                    UnityEngine.Debug.LogError($"Button {buttonName} not found in the scene!");
                }
            }
        }
    }

    private void OnCellClicked(int x, int y)
    {
        SendMoveToServer(x, y);
    }

    private void UpdateGridFromServer(string gameState)
    {
        try
        {
            string[] parts = gameState.Split('|');
            if (parts.Length != 3)
            {
                UnityEngine.Debug.LogError("Malformed game state: " + gameState);
                return;
            }

            string boardData = parts[0];
            currentPlayer = int.Parse(parts[1]); // Update currentPlayer from the server
            bool gameActive = bool.Parse(parts[2]);

            UnityEngine.Debug.Log($"Current player: {currentPlayer}, Game active: {gameActive}");

            string[] rows = boardData.Split(';');
            for (int i = 0; i < 3; i++)
            {
                string[] cells = rows[i].Split(',');
                for (int j = 0; j < 3; j++)
                {
                    int value = int.Parse(cells[j]);
                    gridButtons[i, j].GetComponentInChildren<UnityEngine.UI.Text>().text = value == 1 ? "X" : value == 2 ? "O" : "";
                    gridButtons[i, j].interactable = (value == 0) && gameActive;
                }
            }

            gameStateText.text = gameActive ? $"Player {currentPlayer}'s turn" : "Game Over!";
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError("Error in UpdateGridFromServer: " + ex.Message);
        }
    }
}
