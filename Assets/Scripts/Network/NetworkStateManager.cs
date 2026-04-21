/* Note: Setup designed for Photon PUN 2 network integration. Uncomment after importing PUN.
using Photon.Pun;
*/
using UnityEngine;
using ARFishApp.Core;

namespace ARFishApp.Network
{
    /// <summary>
    /// Professional Multiplayer Setup using Photon PUN architectural style.
    /// </summary>
    public class NetworkStateManager : MonoBehaviour /* MonoBehaviourPunCallbacks */
    {
        public bool isTeacherMode = false;
        // private PhotonView photonView;

        private void Awake()
        {
            // photonView = GetComponent<PhotonView>();
        }

        private void Start()
        {
            if (SystemStateManager.Instance != null)
            {
                SystemStateManager.Instance.OnStateChanged += OnLocalStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (SystemStateManager.Instance != null)
            {
                SystemStateManager.Instance.OnStateChanged -= OnLocalStateChanged;
            }
        }

        private void OnLocalStateChanged(ModuleType newType)
        {
            if (!isTeacherMode) return; 

            Debug.Log($"[Network Sync] MASTER CLIENT: Sending High-Level RPC to force Student devices into: {newType}");
            
            // Activate when Photon PUN is imported:
            // photonView.RPC("RpcSyncState", RpcTarget.Others, (int)newType);
        }

        // [PunRPC]
        public void RpcSyncState(int moduleIndex)
        {
            if (isTeacherMode) return; 

            ModuleType incomingState = (ModuleType)moduleIndex;
            Debug.Log($"[Network Sync] CLIENT: Received Authorized Server RPC. Executing Module: {incomingState}");
            
            SystemStateManager.Instance.ChangeState(incomingState);
        }
        
        /* 
        // Sync late-joiners (e.g., student joins late, instantly sees what teacher is doing):
        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            if (isTeacherMode)
            {
                photonView.RPC("RpcSyncState", newPlayer, (int)SystemStateManager.Instance.CurrentModule);
            }
        }
        */
    }
}
