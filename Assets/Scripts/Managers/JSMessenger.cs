using System.Runtime.InteropServices;
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
    public DojoWorker dojoWorker;

    [DllImport("__Internal")]
    private static extern void WebGLReady();

    [DllImport("__Internal")]
    private static extern void OpenConnectionPage();

    [DllImport("__Internal")]
    private static extern void ClearSession();
    [DllImport("__Internal")]
    private static extern void ExecuteCreatePlayer();

    void Start ()
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

    public void RegisterAccount(string payload)
    {
        AccountData accountData = JsonConvert.DeserializeObject<AccountData>(payload);

        // We mock data because Controller handles the transaction execution, only the address is needed
        Debug.Log(accountData.rpc);
        JsonRpcClient provider = new(accountData.rpc);
        Account account = new(
            provider,
            new SigningKey("0x0"),
            new FieldElement(accountData.address)
        );

        dojoWorker.provider = provider;
        dojoWorker.account = account;

        ExecuteCreatePlayer();

        Debug.Log($"Registering player {accountData.username}  |  {accountData.address}");
    }

    /// <summary>
    ///  Called from JS when ExecuteCreatePlayer(); is returned
    /// </summary>
    public void PlayerCreated() {
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
