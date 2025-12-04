using FishNet.Object;
using UnityEngine;
using System.Collections;

public class ProjectileSpawner : NetworkBehaviour
{
    [SerializeField] private NetworkObject projectilePrefab;

    /// <summary>
    /// Call this from server or via ServerRpc to spawn a projectile.
    /// </summary>
    [ServerRpc]
    public void SpawnProjectileServer(Vector3 position, Vector3 forward)
    {
        if (!IsServerInitialized) return;

        // Instantiate projectile
        NetworkObject proj = Instantiate(projectilePrefab, position, Quaternion.LookRotation(forward));

        // Spawn it on all clients (server authority)
        Spawn(proj);

        // Start despawn timer
        StartCoroutine(DespawnAfterDelay(proj, 5f));
    }

    private IEnumerator DespawnAfterDelay(NetworkObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (obj != null && obj.IsSpawned)
            Despawn(obj); // Serverseitige Löschung für alle Clients
    }
}
