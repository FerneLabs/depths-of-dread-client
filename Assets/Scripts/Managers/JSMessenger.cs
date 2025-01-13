using System.Runtime.InteropServices;
using Dojo;
using Dojo.Starknet;
using Newtonsoft.Json;
using UnityEngine;

public struct AccountData
{
    public string address;
    public string username;
    public string rpc;
}

public class JSMessenger : MonoBehaviour
{
    public WorldManager worldManager;
    public DojoWorker dojoWorker;

    [DllImport("__Internal")]
    private static extern void WebGLReady();

    [DllImport("__Internal")]
    private static extern void OpenConnectionPage();

    [DllImport("__Internal")]
    private static extern void ClearSession();
    [DllImport("__Internal")]
    private static extern void ExecuteCreatePlayer();

    void Start()
    {
        // Send signal to JS app that GameObject is ready for communication
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLReady();
#endif
    }

    public void OpenConnection()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        OpenConnectionPage();
#else
        dojoWorker.SimulateControllerConnection("test_username");
#endif
    }

    public void Disconnect()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        ClearSession();
#else
        dojoWorker.SimulateControllerDisconnection();
#endif
    }

    public async void RegisterAccount(string payload)
    {
        AccountData accountData = JsonConvert.DeserializeObject<AccountData>(payload);

        JsonRpcClient provider = new(accountData.rpc);
        Account account = new(
            provider,
            new SigningKey("0x0"), // mock cause we don't need it, controller takes care of it
            new FieldElement(accountData.address)
        );

        dojoWorker.provider = provider;
        dojoWorker.account = account;

        // Run player creation if not present in world
        string addressHash = EncodingService.GetPoseidonHash(dojoWorker.account.Address);
        if (worldManager.Entity(addressHash) == null)
        {
            ExecuteCreatePlayer();
        }

        await dojoWorker.SyncLocalEntities();
        Debug.Log($"Registering player {accountData.username}  |  {accountData.address}");
    }

    /// <summary>
    ///  Called from JS when ExecuteCreatePlayer(); is returned
    /// </summary>
    public void PlayerCreated()
    {
        dojoWorker.InitLocalCurrentFloor();
    }

    /// <summary>
    ///  Called from JS when session is cleared
    /// </summary>
    public void UnregisterAccount()
    {
        Debug.Log($"Unregistering player");
        // should work for both webgl and editor
        dojoWorker.SimulateControllerDisconnection();
    }
}
