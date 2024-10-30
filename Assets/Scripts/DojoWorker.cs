using System.Collections.Generic;
using UnityEngine;
using Dojo;
using Dojo.Starknet;
using dojo_bindings;
using System.Numerics;
using System.Text;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

public class DojoWorker : MonoBehaviour
{
    [SerializeField] WorldManager worldManager;
    [SerializeField] WorldManagerData dojoConfig;
    [SerializeField] DojoWorkerData dojoWorkerData;
    [SerializeField] GameObject playerSprite;
    public Actions actions;
    public JsonRpcClient provider;
    public Account masterAccount;

    // Start is called before the first frame update
    void Start()
    {
        provider = new JsonRpcClient(dojoConfig.rpcUrl);
        masterAccount = new Account(provider, new SigningKey(dojoWorkerData.masterPrivateKey), new FieldElement(dojoWorkerData.masterAddress));
        worldManager.synchronizationMaster.OnEventMessage.AddListener(HandleEvent);
        worldManager.synchronizationMaster.OnSynchronized.AddListener(HandleSync);
        worldManager.synchronizationMaster.OnEntitySpawned.AddListener(HandleSpawn);

        CreatePlayer("jojo");
    }

    public async void CreatePlayer(string username) {
        // Encode to avoid "Invalid decimal string" error when creating FieldElement
        byte[] bytes = Encoding.UTF8.GetBytes(username);
        BigInteger encodedUsername = new BigInteger(bytes, isUnsigned: true);

        await actions.create_player(masterAccount, new FieldElement(encodedUsername));
    }

    public async void CreateGame() {
        await actions.create_game(masterAccount);
    }

    public async void SendMove(int direction)
    {
        switch (direction)
        {
            case 0:
                await actions.move(masterAccount, new Direction.Up());
                break;
            case 1:
                await actions.move(masterAccount, new Direction.Right());
                break;
            case 2:
                await actions.move(masterAccount, new Direction.Left());
                break;
            case 3:
                await actions.move(masterAccount, new Direction.Down());
                break;
            default:
                break;
        }
    }

    void HandleEvent(ModelInstance modelInstance)
    {
        // Debug.Log(modelInstance);
    }

    void HandleSpawn(GameObject spawnedEntity)
    {
        Debug.Log($"Entity Spawned: {spawnedEntity}");
    }

    void HandleSync(List<GameObject> syncedObjects)
    {
        foreach (var item in syncedObjects)
        {
            Debug.Log($"Synced Objects: {item}");
        }
    }
}
