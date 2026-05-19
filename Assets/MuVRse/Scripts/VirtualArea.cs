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

using System;
using System.Collections.Generic;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace VRSYS.MuVRse.Scripts
{
    [RequireComponent(typeof(VABoundary), typeof(VAWorldTransition), typeof(VAConfiguration))]
    [RequireComponent(typeof(VASkyboxTransition))]
    [RequireComponent(typeof(NetworkObject), typeof(NetworkTransform))]
    public class VirtualArea : NetworkBehaviour
    {
        #region Member Variables

        [Header("Layer Settings")]
        [SerializeField] private int _ignoreLayer;
        [SerializeField] private int _layer;

        [Header("Object References")]
        [SerializeField] private GameObject _skybox;
        [SerializeField] private GameObject _transition;
        [SerializeField] private Material _gridMaterial;
        [SerializeField] public GameObject _worldContent;
        
        [Header("Private Fields")]
        private float _transitionDurationSkybox;
        private float _transitionDurationWorld;
        private float _transitionDurationPlayer;
        private ulong _areaId;
        private VAWorldTransition _vaWorldTransition;
        private VASkyboxTransition _vaSkyboxTransition;
        private VABoundary _vaBoundary;
        private float _previewAreaDistance;

        [Header("Public Fields")]
        public NetworkVariable<VirtualAreaState> state;
        // public string areaName;
        public NetworkVariable<FixedString32Bytes> areaName;
        public NetworkList<Vector3> points;

        #endregion

        #region Enums

        public enum VirtualAreaState
        {
            None,
            Spawned,
            SpawnedWithBoundary,
            SpawnedWithWorld,
            SpawnedWithBoundaryAndWorld,
        }

        #endregion

        #region Unity Callback Functions

        private void Awake()
        {
            _vaBoundary = GetComponent<VABoundary>();
            points = new NetworkList<Vector3>();

        }

        public override void OnNetworkSpawn()
        {
            _areaId = GetComponent<NetworkObject>().NetworkObjectId;
            if(IsServer) state.Value = VirtualAreaState.Spawned;
            
            Debug.Log("Initialize Virtual Area OnNetworkSpawn");
            Initialize();

            if (VAPlacement.LocalInstance)
            {
                StartCoroutine(DelayedRegister());
            }
            else
            {
                Debug.Log("VirtualAreaManager is missing");
            }
        }

        private void Start()
        {
            InitializeLateJoin();
        }

        private void Update()
        {
            if (state.Value == VirtualAreaState.SpawnedWithBoundaryAndWorld)
            {
                CheckPlayerEnterAndExit();
                CheckGhostAvatarVisibility();
                UpdateBoundaryGridTransparency();
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            UnregisterWithManager();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set all layers to be ignored by the camera
        /// </summary>
        public void SetLayerToIgnore()
        {
            Utils.SetLayerRecursively(gameObject, _ignoreLayer);
            if (_worldContent)
            {
                Utils.SetLayerRecursively(_worldContent, _ignoreLayer);
            }
        }
        
        /// <summary>
        /// Set all layers to be seen by the camera
        /// </summary>
        public void SetLayerToShow()
        {
            Utils.SetLayerRecursively(gameObject, _layer);
            if (_worldContent)
            {
                Utils.SetLayerRecursively(_worldContent, _layer);
            }
        }

        /// <summary>
        /// Initializes the virtual area.
        /// Sets the world content and gets all configuration settings.
        /// Creates boundary 
        /// </summary>
        public void Initialize()
        {
            StartCoroutine(SetConfiguration());
            StartCoroutine(SetLocalPlayerAreaManager());
        }

        public void InitializeLateJoin()
        {
            Debug.Log("Init Late join");
            if (state.Value == VirtualAreaState.SpawnedWithBoundary ||
                state.Value == VirtualAreaState.SpawnedWithBoundaryAndWorld)
            {
                Debug.Log($"Virtual area state late join {state.Value}");
                if (!_vaBoundary.IsBoundaryCreated())
                {
                    List<Vector3> pointList = new List<Vector3>();
                    foreach (var item in points)
                    {
                        pointList.Add(item);
                    }

                    CreateBoundary(pointList);
                }
                if (state.Value == VirtualAreaState.SpawnedWithBoundaryAndWorld)
                {
                    foreach (ulong contentId in VAGlobalInfo.LocalInstance.virtualAreaContentIds)
                    {
                        if (Utils.GetNetworkObject(contentId).gameObject.name == GetComponent<VAConfiguration>().configuration.worldContent.gameObject.name+"(Clone)")
                        {
                            _worldContent = Utils.GetNetworkObject(contentId).gameObject;
                        }
                    }
                }
                gameObject.GetComponent<VAWorldTransition>().InitializeWorld(_worldContent, _vaBoundary.GetBoundarySize(), _vaBoundary.GetCentroid());
            }
        }

        /// <summary>
        /// Activate the virtual area
        /// Call transition functions and activate on local player
        /// </summary>
        public void Activate()
        {
            PlayerAreaManager.LocalInstance.ActivateArea(_areaId, _transitionDurationPlayer);
            _vaSkyboxTransition.TransitionIn();
            _vaWorldTransition.TransitionIn();
        }

        /// <summary>
        /// Deactivate the virtual area
        /// Call transition functions and deactivate on local player
        /// </summary>
        public void Deactivate()
        {
            PlayerAreaManager.LocalInstance.DeactivateArea(0, _transitionDurationPlayer);
            _vaSkyboxTransition.TransitionOut();
            _vaWorldTransition.TransitionOut();
        }

        /// <summary>
        /// Creates the boundary.
        /// Set points, calculate new centroid, move world and skybox to new centroid.
        /// </summary>
        public void CreateBoundary(List<Vector3> p)
        {
            SetBoundaryPoints(p);
            Vector3 newCenter = _vaBoundary.CalculateCentroid();
            _skybox.transform.position = newCenter;
            _transition.transform.position = newCenter;
            _vaBoundary.CreateBoundaryGrid();
            if (IsServer)
            {
                if (state.Value == VirtualAreaState.SpawnedWithWorld)
                {
                    state.Value = VirtualAreaState.SpawnedWithBoundaryAndWorld;

                }
                else if (state.Value == VirtualAreaState.Spawned)
                {
                    state.Value = VirtualAreaState.SpawnedWithBoundary;
                }
            }
        }

        /// <summary>
        /// Set boundary points, close the loop on the line renderer and calculate its centroid.
        /// </summary>
        public void SetBoundaryPoints(List<Vector3> pointList)
        {
            _vaBoundary.ResetLineRendererPoints();
            _vaBoundary.SetLineRendererPoints(pointList);
            _vaBoundary.SetLineRendererLoop(true);

            foreach (Vector3 point in pointList)
            {
                AddPointRpc(point);
            }
        }

        /// <summary>
        /// Removes the current boundary. Set all lineRenderer points to 0.
        /// </summary>
        public void ResetBoundaryPoints()
        {
            _vaBoundary.ResetLineRendererPoints();
            _vaBoundary.DeleteGrid();
            if (IsServer)
            {
                if (state.Value == VirtualAreaState.SpawnedWithBoundaryAndWorld)
                {
                    state.Value = VirtualAreaState.SpawnedWithWorld;
                }
                else if (state.Value == VirtualAreaState.SpawnedWithBoundary)
                {
                    state.Value = VirtualAreaState.Spawned;
                    
                }
            }
        }

        public float CalculatePlayerDistanceToBoundary()
        {
            if (PlayerAreaManager.LocalInstance)
            {
                return _vaBoundary.CalculatePlayerDistanceToBoundary(PlayerAreaManager.LocalInstance.transform.position);
            }
            return -1;
        }

        [ContextMenu("Spawn World")]
        public void SpawnWorldObjects()
        {
            if (_worldContent != null) return;

            Debug.Log("Spawning World");
            VAConfiguration vaConfig = GetComponent<VAConfiguration>();

            if (vaConfig.configuration != null)
            {
                NetworkObject worldContent = vaConfig.configuration.worldContent;
                if (worldContent != null)
                {
                    Vector3 center = _vaBoundary.GetCentroid();
                    float areaSize = _vaBoundary.GetBoundarySize();
                    NetworkObject no = NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(
                        worldContent, NetworkManager.ServerClientId, position: center);
                    VAGlobalInfo.LocalInstance.virtualAreaContentIds.Add(no.NetworkObjectId);
                    _worldContent = no.gameObject;
                    gameObject.GetComponent<VAWorldTransition>().InitializeWorld(_worldContent, areaSize, center);
                    
                    if (IsServer)
                    {
                        if (state.Value == VirtualAreaState.Spawned)
                        {
                            state.Value = VirtualAreaState.SpawnedWithWorld;
                        }
                        else if (state.Value == VirtualAreaState.SpawnedWithBoundary)
                        {
                            state.Value = VirtualAreaState.SpawnedWithBoundaryAndWorld;
                        }
                        if (VAPlacement.LocalInstance.activeAreaId.Value != 0)
                        {
                            VAPlacement.LocalInstance.activeAreaId.Value = 0;
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("Cannot spawn world content. Missing configuration ?");
            }
        }

        public void DeleteWorldObjects()
        {
            if (_worldContent == null) return;

            _worldContent.GetComponent<NetworkObject>().Despawn();
            _worldContent = null;
            if (IsServer)
            {
                if (state.Value == VirtualAreaState.SpawnedWithWorld)
                {
                    state.Value = VirtualAreaState.Spawned;
                }
                else if (state.Value == VirtualAreaState.SpawnedWithBoundaryAndWorld)
                {
                    state.Value = VirtualAreaState.SpawnedWithBoundary;
                }
            }
        }

        #endregion

        #region Private Methods

        private void OnActiveAreaChanged(ulong prev, ulong current)
        {
            if (current == GetComponent<NetworkObject>().NetworkObjectId || current == 0)
            {
                SetLayerToShow();
            }
            else
            {
                SetLayerToIgnore();
            }
        }

        /// <summary>
        /// Updates boundary transparency depending on player distance.
        /// </summary>
        private void UpdateBoundaryGridTransparency()
        {
            Color color = _gridMaterial.color;

            if (CalculatePlayerDistanceToBoundary() > 1)
            {
                color.a = 0;
            }
            else
            {
                color.a = 1 - CalculatePlayerDistanceToBoundary();
            }

            _gridMaterial.color = color;
        }

        /// <summary>
        /// Check VirtualAreaConfig and get needed script references.
        /// </summary>
        private void GetConfigSettingsAndReferences()
        {
            VAConfiguration vaConfig = GetComponent<VAConfiguration>();

            if (vaConfig.configuration.useTransitionByTime)
            {
                try
                {
                    _vaWorldTransition = GetComponent<VAWorldTransition>();
                    _vaSkyboxTransition = GetComponent<VASkyboxTransition>();
                }
                catch
                {
                    Debug.Log("Missing transition script (Time)");
                }
            }

            if (IsServer) areaName.Value = vaConfig.configuration.areaName;
            // areaName = vaConfig.configuration.areaName;
            _previewAreaDistance = vaConfig.configuration.ghostPreviewSize;
        }

        /// <summary>
        /// Check if the player enters or exits an area.
        /// </summary>
        private void CheckPlayerEnterAndExit()
        {
            PlayerAreaManager localPlayer = PlayerAreaManager.LocalInstance;

            if (localPlayer)
            {
                if (_vaBoundary.IsPointInArea(localPlayer.GetPlayerPosition()) && localPlayer.activeAreaId.Value == 0)
                {
                    Debug.Log($"Virtual Area activates area {_areaId}");
                    Activate();
                }
                else if (!_vaBoundary.IsPointInArea(localPlayer.GetPlayerPosition()) && localPlayer.activeAreaId.Value != 0 && _areaId == localPlayer.activeAreaId.Value)
                {
                    Debug.Log($"Virtual Area deactivates area {_areaId}");
                    Deactivate();
                }
            }
        }

        private void CheckGhostAvatarVisibility()
        {
            
            PlayerAreaManager localPlayer = PlayerAreaManager.LocalInstance;
            
            foreach (var player in VAGlobalInfo.LocalInstance.players)
            {
                if (player.activeAreaId.Value == 0 && localPlayer.activeAreaId.Value != 0)
                {
                    // Debug.Log($"Players are not in the same area. Distance of remote player{_vaBoundary.CalculatePlayerDistanceToBoundary(player.transform.position)}");
                    if (_vaBoundary.CalculatePlayerDistanceToBoundary(player.transform.position) < _previewAreaDistance)
                    {
                        if (!player.GetComponent<GhostPreview>().IsActive())
                        {
                            player.GetComponent<GhostPreview>().Activate();
                        }
                    }
                    else
                    {
                        if (player.GetComponent<GhostPreview>().IsActive())
                        {
                            player.GetComponent<GhostPreview>().Deactivate();
                        }
                    }
                }
                else
                {
                    if (player.GetComponent<GhostPreview>().IsActive())
                    {
                        player.GetComponent<GhostPreview>().Deactivate();
                    }
                }
            }
        }

        /// <summary>
        /// Registers this area with the VirtualAreaManager.
        /// </summary>
        private void RegisterWithManager()
        {
            VAGlobalInfo.LocalInstance.RegisterVirtualArea(this, _areaId);
            Debug.Log($"Registered Virtual Area {_areaId}");
        }

        /// <summary>
        /// Unregisters this area from the VirtualAreaManager.
        /// </summary>
        private void UnregisterWithManager()
        {
            VAGlobalInfo.LocalInstance.UnregisterVirtualArea(this, _areaId);
            Debug.Log($"Unregistered Virtual Area {_areaId}");
        }

        #endregion

        #region Coroutines

        private IEnumerator DelayedRegister()
        {
            yield return new WaitForSeconds(1);
            RegisterWithManager();
        }

        private IEnumerator SetConfiguration()
        {
            Debug.Log("Setting VA Configuration");
            foreach (VAConfigCreator config in VAPlacement.LocalInstance.virtualAreaList.list)
            {
                Debug.Log($"Name compare: {config.areaName} = {areaName.Value}");
                if (config.areaName == areaName.Value)
                {
                    GetComponent<VAConfiguration>().configuration = config;
                }
            }
            yield return new WaitUntil(() => gameObject.GetComponent<VAConfiguration>().configuration != null);
            GetConfigSettingsAndReferences();
        }

        #endregion

        private IEnumerator SetLocalPlayerAreaManager()
        {
            yield return new WaitUntil(() => PlayerAreaManager.LocalInstance != null);
            PlayerAreaManager.LocalInstance.activeAreaId.OnValueChanged += OnActiveAreaChanged;
            
        }

        #region RPC Functions

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void RequestSpawnAreaContentRpc()
        {
            SpawnWorldObjects();
            VAPlacement.LocalInstance.RequestActiveAreaChangeRpc(0);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void AddPointRpc(Vector3 point)
        {
            points.Add(point);
        }

        #endregion
    }
}