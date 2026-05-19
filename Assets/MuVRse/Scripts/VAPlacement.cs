// VRSYS plugin of Virtual Reality and Visualization Group (Bauhaus-University Weimar)
//  _    ______  _______  _______
// | |  / / __ \/ ___/\ \/ / ___/
// | | / / /_/ /\__ \  \  /\__ \ 
// | |/ / _, _/___/ /  / /___/ / 
// |___/_/ |_|/____/  /_//____/  
//
//  __                            __                       __   __   __    ___ .  . ___
// |__)  /\  |  | |__|  /\  |  | /__`    |  | |\ | | \  / |__  |__) /__` |  |   /\   |  
// |__) /~~\ \__/ |  | /~~\ \__/ .__/    \__/ | \| |  \/  |___ |  \ .__/ |  |  /~~\  |  
//
//       ___               __                                                           
// |  | |__  |  |\/|  /\  |__)                                                          
// |/\| |___ |  |  | /~~\ |  \                                                                                                                                                                                     
//
// Copyright (c) 2023 Virtual Reality and Visualization Group
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//-----------------------------------------------------------------
//   Authors:        Sebastian Heckner
//   Date:           2025
//-----------------------------------------------------------------

using Unity.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using VRSYS.Core.Networking;

namespace VRSYS.MuVRse.Scripts
{
    [RequireComponent(typeof(NetworkObject))]
    public class VAPlacement : NetworkBehaviour
    {
        #region Member Variables
        
        public static VAPlacement LocalInstance { get; private set; }
        
        [Tooltip("If this flag is set the server spawns the world in placement mode")]
        [SerializeField] private bool _spawnServerInPlacementMode = true;
        
        [Tooltip("Reference to the button that is used for triggering the placement mode")]
        [SerializeField] private InputActionReference _togglePlacementMode;
        
        [Tooltip("List of areas that can be spawned")]
        public VAList virtualAreaList;
        
        [Tooltip("Empty Virtual Area Prefab")]
        public NetworkObject emptyAreaPrefab;
        
        [HideInInspector] public NetworkVariable<bool> isPlacementModeActive = new NetworkVariable<bool>(true);
        [HideInInspector] public NetworkVariable<ulong> activeAreaId = new NetworkVariable<ulong>(0);
        
        #endregion

        #region Callback Functions
        

        private void Awake()
        {
            
            if (LocalInstance == null)
            {
                LocalInstance = this;
                Debug.Log($"LocalInstance set on {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"Multiple VAPlacement instances! Destroying {gameObject.name}");
                Destroy(gameObject);
            }

            if (_togglePlacementMode != null)
            {
                _togglePlacementMode.action.performed += TogglePlacementMode;
            }
            
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if(_togglePlacementMode != null)
            {
                _togglePlacementMode.action.performed -= TogglePlacementMode;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                if (!_spawnServerInPlacementMode)
                {
                    RequestPlacementModeChangeRpc(false);
                }
            }
            
        }

        #endregion

        #region Public Methods
        
        /// <summary>
        /// This function is just for testing purposes to activate and deactivate the placement mode via editor
        /// </summary>
        [ContextMenu("TogglePlacementMode")]
        public void TogglePlacementModeContextMenu()
        {
            if (NetworkUser.LocalInstance.userRole.Value.Name.Contains("Admin"))
            {
                RequestPlacementModeChangeRpc(!isPlacementModeActive.Value);
            }
        }
        
        /// <summary>
        /// toggle value of _isPlacementModeActive 
        /// </summary>
        /// <param name="context"></param>
        private void TogglePlacementMode(InputAction.CallbackContext context)
        {
            if (NetworkUser.LocalInstance.userRole.Value.Name.Contains("Admin"))
            {
                Debug.Log("Request placement mode change");
                RequestPlacementModeChangeRpc(!isPlacementModeActive.Value);
            }
        }
        
        
        #endregion

        #region Private Methods

        private void SpawnArea(string areaName)
        {
            if (IsAreaSpawned(areaName)) return;
            if (emptyAreaPrefab == null)
            {
                Debug.LogError("emptyAreaPrefab is not assigned!");
                return;
            }
            NetworkObject area = NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(emptyAreaPrefab);
            RequestActiveAreaChangeRpc(area.NetworkObjectId);
            AssignConfigurationToAreaRpc(area.NetworkObjectId, new FixedString32Bytes(areaName));
        }
        
        private bool IsAreaSpawned(string areaName)
        {
            foreach (VirtualArea spawnedArea in VAGlobalInfo.LocalInstance.virtualAreas)
            {
                if (areaName == spawnedArea.areaName.Value)
                {
                    return true;
                }
            }
            return false;
        }
        
        private void DespawnArea(string areaName)
        {
            foreach (VirtualArea spawnedArea in VAGlobalInfo.LocalInstance.virtualAreas)
            {
                if (areaName == spawnedArea.areaName.Value)
                {
                    if (spawnedArea.GetComponent<VirtualArea>()._worldContent)
                    {
                        spawnedArea.GetComponent<VirtualArea>()._worldContent.gameObject.GetComponent<NetworkObject>().Despawn();
                    }
                    spawnedArea.GetComponent<NetworkObject>().Despawn();
                    
                    if(activeAreaId.Value == spawnedArea.NetworkObjectId) RequestActiveAreaChangeRpc(0);
                }
            }
        }

        private VirtualArea GetVirtualArea(FixedString32Bytes areaName)
        {
            foreach (VirtualArea area in VAGlobalInfo.LocalInstance.virtualAreas)
            {
                if (area.areaName.Value == areaName.ToString())
                {
                    return area;
                }
            }

            return null;
        }
        
        #endregion

        #region RPC´s

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void RequestPlacementModeChangeRpc(bool state)
        {
            isPlacementModeActive.Value = state;
            Debug.Log($"Server changed placement mode. Is placement mode active: {state}");
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void RequestActiveAreaChangeRpc(ulong areaId)
        {
            activeAreaId.Value = areaId;
            Debug.Log($"Server changed active area to {areaId}");
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void RequestAreaSpawnRpc(FixedString32Bytes areaName)
        {
            SpawnArea(areaName.ToString());
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void RequestSpawnAreaContentRpc(FixedString32Bytes areaName)
        {
            VirtualArea area = GetVirtualArea(areaName);
            if (area == null) return;
            area.SpawnWorldObjects();
        }
        
        [Rpc(SendTo.Everyone)]
        private void AssignConfigurationToAreaRpc(ulong areaId, FixedString32Bytes areaName)
        {
            foreach (VAConfigCreator config in virtualAreaList.list)
            {
                if (config.areaName == areaName)
                {
                    GetNetworkObject(areaId).gameObject.GetComponent<VAConfiguration>().configuration = config;
                }
            }
        }
        
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void RequestDespawnAreaRpc(FixedString32Bytes areaName)
        {
            DespawnArea(areaName.ToString());
        }
        
        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
        public void RequestCreateBoundaryRpc(ulong areaId, Vector3[] points)
        {
            List<Vector3> p  = new List<Vector3>(points);
            VirtualArea area = Utils.GetNetworkObject(areaId).GetComponent<VirtualArea>();
            // area.SetBoundaryPoints(p);
            area.CreateBoundary(p);
            
        }
        
        
        #endregion
        
    }
    
}