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

using System.Collections;
using Unity.Netcode;
using UnityEngine;


namespace VRSYS.MuVRse.Scripts
{
    public class PlayerAreaManager : NetworkBehaviour
    {
        #region Member Variables

        public static PlayerAreaManager LocalInstance;

        [Tooltip("The network id of the area this player belongs to")]
        public NetworkVariable<ulong> activeAreaId;
        
        [Tooltip("The center eye anchor component of the ovr camera rig")]
        [SerializeField] private GameObject _centerEyeAnchor;
        
        
        [Header("Avatar references")]

        [Tooltip("The _hmd of the avatar")]
        [SerializeField] private GameObject _hmd;

        [Tooltip("The _head of the avatar")]
        [SerializeField] private GameObject _head;

        [Tooltip("The _body of the avatar")]
        [SerializeField] private GameObject _body;

        [Tooltip("The left hand of the avatar")]
        [SerializeField] private GameObject _leftHand;

        [Tooltip("The right hand of the avatar")]
        [SerializeField] private GameObject _rightHand;
        
        
        
        private OVRPassthroughLayer _ovrPassthroughLayer;
        private bool _ovrTransitioning;
        private Coroutine _ovrCoroutine;
        
        
        #endregion

        #region Callback Functions

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                LocalInstance = this;
                _ovrPassthroughLayer = FindFirstObjectByType<OVRPassthroughLayer>();
                InvokePlayerChangedAreaRpc();
            }
            
        }

        void Start()
        {
            try
            {
                VAGlobalInfo.LocalInstance.RegisterUser(this);
            }
            catch
            {
                Debug.Log("Could not register Player. Virtual Area Manager missing");
            }
            
            activeAreaId.OnValueChanged += OnActiveAreaIdChanged;
            VAGlobalInfo.LocalInstance.playerChangedArea.AddListener(CheckAvatarState);
        }


        #endregion
        
        
        #region Public Methods

        public Vector3 GetPlayerPosition()
        {
            return _centerEyeAnchor.transform.position;
        }
        
        /// <summary>
        /// Handels all player functions to activate an area
        /// </summary>
        /// <param name="areaId"></param>
        /// <param name="transitionTime"></param>
        public void ActivateArea(ulong areaId, float transitionTime)
        {
            // Setze area als aktiv & informiere alle anderen Player (RPC)
            Debug.Log($"Player activates Area {areaId}");
            RequestAreaIdChangeRpc(areaId);
            
            StartPassthroughTransition(0, transitionTime);
            // Aktiviere die Avatare aller anderen Spieler die in der Area sind | Done
            // Setze areas interaction layer und remove alle anderen
            // Rendere nur die Objekte die in der Area sind
            // Starte Passthrough transition
        }
        
        /// <summary>
        /// Handels all player functions to deactivate an area
        /// </summary>
        /// <param name="areaId"></param>
        /// <param name="transitionTime"></param>
        public void DeactivateArea(ulong areaId, float transitionTime)
        {
            Debug.Log($"Player deactivates Area {areaId}");
            RequestAreaIdChangeRpc(areaId);
            StartPassthroughTransition(1, transitionTime);

        }
        
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Callback function when a player changes areas
        /// Is responsible for activating & deactivating the correct avatars for local and remote players based on the area id
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        private void OnActiveAreaIdChanged(ulong oldValue, ulong newValue)
        {
            InvokePlayerChangedAreaRpc();
        }

        private void CheckAvatarState()
        {
            ulong localPlayerAreaId = LocalInstance.activeAreaId.Value;
            
            if (LocalInstance == this)
            {
                if (activeAreaId.Value == 0) SwitchAvatarComponentVisibility(false, false, false, false, false);
                else SwitchAvatarComponentVisibility(false, false, false, true, true);
                return;
            }
            if (activeAreaId.Value == localPlayerAreaId && activeAreaId.Value != 0) // Remote player
            {
                Debug.Log($"active area id {activeAreaId.Value}, local player area id {localPlayerAreaId}");
                SwitchAvatarComponentVisibility(true, true, true, true, true);
            }
            else if (activeAreaId.Value != localPlayerAreaId || (activeAreaId.Value == localPlayerAreaId && activeAreaId.Value == 0))
            {
                Debug.Log($"active area id {activeAreaId.Value}, local player area id {localPlayerAreaId}");
                SwitchAvatarComponentVisibility(false, false, false, false, false);
            }
        }

        /// <summary>
        /// Changes the layers of the avatar components. 0 is default and visible, 3 is cameraIgnore and is ignored by the player camera
        /// </summary>
        /// <param name="showHead"></param>
        /// <param name="showBody"></param>
        /// <param name="showHmd"></param>
        /// <param name="showLeftHand"></param>
        /// <param name="showRightHand"></param>
        private void SwitchAvatarComponentVisibility(bool showHead, bool showBody, bool showHmd, bool showLeftHand, bool showRightHand)
        {
            if (_head != null)
            {
                int l = showHead ? 0 : 3;
                Utils.SetLayerRecursively(_head, l);
                // _head.layer = showHead ? 0 : 3;
            }
            if (_body != null)
            {
                int l = showBody ? 0 : 3;
                Utils.SetLayerRecursively(_body, l);
                // _body.layer = showBody ? 0 : 3;
            }
            if (_hmd != null)
            {
                int l = showHmd ? 0 : 3;
                Utils.SetLayerRecursively(_hmd, l);
                // _hmd.layer = showHmd ? 0 : 3;
            }
            if (_leftHand != null)
            {
                int l = showLeftHand ? 0 : 3;
                Utils.SetLayerRecursively(_leftHand, l);
                // _leftHand.layer = showLeftHand ? 0 : 3;
            }
            if (_rightHand != null)
            {
                int l = showRightHand ? 0 : 3;
                Utils.SetLayerRecursively(_rightHand, l);
                // _rightHand.layer = showRightHand ? 0 : 3;
            }
        }
        
        /// <summary>
        /// Starts the coroutine for passthrough transitioning
        /// </summary>
        /// <param name="targetAlpha"></param>
        /// <param name="duration"></param>
        private void StartPassthroughTransition(float targetAlpha, float duration)
        {
            // StartOVRTransition von altem Skript
            float startAlpha = _ovrPassthroughLayer.textureOpacity;
            if (_ovrTransitioning) StopCoroutine(_ovrCoroutine);
            _ovrCoroutine = StartCoroutine(PassthroughTransitionCoroutine(startAlpha, targetAlpha, duration));
        }

        /// <summary>
        /// Coroutine function for transitioning the passthrough layer
        /// </summary>
        /// <param name="startAlpha"></param>
        /// <param name="targetAlpha"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        private IEnumerator PassthroughTransitionCoroutine(float startAlpha, float targetAlpha, float duration)
        {
            _ovrTransitioning = true;
            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
                _ovrPassthroughLayer.textureOpacity = currentAlpha;
                yield return null;
            }

            _ovrTransitioning = false;
        }
        
        #endregion

        #region RPCs

        /// <summary>
        /// Requests the change of the players activeAreaId network variable from the server
        /// </summary>
        /// <param name="newValue"></param>
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void RequestAreaIdChangeRpc(ulong newValue)
        {
            activeAreaId.Value = newValue;
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
        public void InvokePlayerChangedAreaRpc()
        {
            VAGlobalInfo.LocalInstance.playerChangedArea.Invoke();
        }
        
        #endregion
    }
    
}
