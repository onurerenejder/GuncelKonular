using UnityEngine;
using ARFishApp.Core;
using ARFishApp.Interaction;

#if PUN_2_OR_NEWER
using Photon.Pun;
using Photon.Realtime;
#endif

namespace ARFishApp.Network
{
    /// <summary>
    /// Teacher -> student synchronization layer.
    /// Synchronizes module changes and basic hotspot interactions.
    /// </summary>
    public class NetworkStateManager :
#if PUN_2_OR_NEWER
        MonoBehaviourPunCallbacks
#else
        MonoBehaviour
#endif
    {
        [Header("Classroom Role")]
        public bool isTeacherMode = false;

        [Header("Sync Options")]
        public bool syncModuleState = true;
        public bool syncHotspotInteractions = true;

#if PUN_2_OR_NEWER
        private PhotonView photonView;
#endif

        private void Awake()
        {
#if PUN_2_OR_NEWER
            photonView = GetComponent<PhotonView>();
            if (photonView == null)
            {
                Debug.LogError("[Network Sync] PhotonView is required on NetworkStateManager for RPC synchronization.");
            }
#endif
        }

        private void Start()
        {
            if (SystemStateManager.Instance != null)
            {
                SystemStateManager.Instance.OnStateChanged += OnLocalStateChanged;
            }

            HotspotNode.OnAnyHotspotTapped += OnLocalHotspotTapped;
        }

        private void OnDestroy()
        {
            if (SystemStateManager.Instance != null)
            {
                SystemStateManager.Instance.OnStateChanged -= OnLocalStateChanged;
            }

            HotspotNode.OnAnyHotspotTapped -= OnLocalHotspotTapped;
        }

        private void OnLocalStateChanged(ModuleType newType)
        {
            if (!isTeacherMode || !syncModuleState) return;

#if PUN_2_OR_NEWER
            if (photonView == null) return;
            photonView.RPC(nameof(RpcSyncState), RpcTarget.Others, (int)newType);
            Debug.Log($"[Network Sync] Teacher pushed module state: {newType}");
#else
            Debug.LogWarning("[Network Sync] PUN_2_OR_NEWER is not defined. Module sync is inactive until Photon PUN is imported.");
#endif
        }

        private void OnLocalHotspotTapped(HotspotNode hotspot)
        {
            if (!isTeacherMode || !syncHotspotInteractions || hotspot == null) return;

#if PUN_2_OR_NEWER
            if (photonView == null) return;
            photonView.RPC(nameof(RpcSyncHotspotTap), RpcTarget.Others, hotspot.organName);
            Debug.Log($"[Network Sync] Teacher pushed hotspot interaction: {hotspot.organName}");
#else
            Debug.LogWarning("[Network Sync] PUN_2_OR_NEWER is not defined. Hotspot sync is inactive until Photon PUN is imported.");
#endif
        }

#if PUN_2_OR_NEWER
        [PunRPC]
#endif
        public void RpcSyncState(int moduleIndex)
        {
            if (isTeacherMode || !syncModuleState) return;

            ModuleType incomingState = (ModuleType)moduleIndex;
            if (SystemStateManager.Instance == null) return;
            SystemStateManager.Instance.ChangeState(incomingState);
            Debug.Log($"[Network Sync] Student received module state: {incomingState}");
        }

#if PUN_2_OR_NEWER
        [PunRPC]
#endif
        public void RpcSyncHotspotTap(string organName)
        {
            if (isTeacherMode || !syncHotspotInteractions || string.IsNullOrWhiteSpace(organName)) return;

            HotspotNode[] allHotspots = FindObjectsOfType<HotspotNode>();
            for (int i = 0; i < allHotspots.Length; i++)
            {
                HotspotNode node = allHotspots[i];
                if (node != null && node.organName == organName)
                {
                    node.ApplyRemoteTap();
                    Debug.Log($"[Network Sync] Student applied hotspot interaction: {organName}");
                    return;
                }
            }

            Debug.LogWarning($"[Network Sync] Hotspot not found on student scene: {organName}");
        }

#if PUN_2_OR_NEWER
        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (!isTeacherMode || !syncModuleState || photonView == null || SystemStateManager.Instance == null) return;

            // Late joiner immediately receives the active teaching module.
            photonView.RPC(nameof(RpcSyncState), newPlayer, (int)SystemStateManager.Instance.CurrentModule);
            Debug.Log($"[Network Sync] Late-join student synced to: {SystemStateManager.Instance.CurrentModule}");
        }
#endif
    }
}
