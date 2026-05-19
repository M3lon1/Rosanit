using TMPro;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using VRSYS.Core.Networking;


namespace VRSYS.MuVRse.Scripts
{
    
    public class VAMenuEntry : MonoBehaviour
    {
        #region Member Variables
        
        public VAConfigCreator config;
        
        [Tooltip("Will be set automaticaly when the entry is spawned")]
        [SerializeField] private FixedString32Bytes _areaName;

        [SerializeField] private TextMeshProUGUI _areaNameText;
        [Header("Button References")]
        [SerializeField] private GameObject _placeButton;
        [SerializeField] private GameObject _drawButton;
        [SerializeField] private GameObject _spawnContentButton;
        [SerializeField] private GameObject _deleteButton;
        
        private bool _isSpawned = false;
        private ulong _areaId = 0;

        #endregion
        
        #region Unity Callbacks
        
        void Start()
        {
            _areaName = new FixedString32Bytes(config.areaName);
        }
        
        void Update()
        {
            IsAreaSpawned();
            ButtonStateHandler();
        }

        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Handles which button is active and interactable
        /// </summary>
        private void ButtonStateHandler()
        {
            if (VAPlacement.LocalInstance.activeAreaId.Value == 0)
            {
                if (_isSpawned)
                {
                    SetButtonsActivationState("delete");
                }
                else
                {
                    SetButtonsActivationState("place");
                }
            }
            else if (VAPlacement.LocalInstance.activeAreaId.Value != 0)
            {
                VirtualArea activeArea = Utils.GetNetworkObject(VAPlacement.LocalInstance.activeAreaId.Value)
                    .GetComponent<VirtualArea>();
                
                if (activeArea.areaName.Value == _areaName.ToString())
                {
                    if (activeArea.state.Value == VirtualArea.VirtualAreaState.Spawned)
                    {
                        SetButtonsActivationState("draw");
                        if (CustonEnvironmentRaycast.Singleton.GetObjectPositions().Count > 3 && NetworkUser.LocalInstance.userRole.Value.Name.Contains("Admin"))
                        {
                            _drawButton.GetComponent<Button>().interactable = true;
                        }
                        else
                        {
                            _drawButton.GetComponent<Button>().interactable = false;
                        }
                    }
                    else if (activeArea.state.Value == VirtualArea.VirtualAreaState.SpawnedWithBoundary)
                    {
                        SetButtonsActivationState("spawn");
                    }
                    else if (activeArea.state.Value == VirtualArea.VirtualAreaState.SpawnedWithBoundaryAndWorld)
                    {
                        SetButtonsActivationState("delete");
                    }
                }
                else
                {
                    SetButtonsActivationState("inactive");
                }
            }
        }

        /// <summary>
        /// Check if the corresponding area is spawned
        /// </summary>
        private void IsAreaSpawned()
        {
            bool found = false;
            foreach (ulong areaId in VAGlobalInfo.LocalInstance.virtualAreaIds)
            {
                if (Utils.GetNetworkObject(areaId))
                {
                    if (Utils.GetNetworkObject(areaId).GetComponent<VirtualArea>().areaName.Value == _areaName.ToString())
                    {
                        found = true;
                        _areaId = areaId;
                    }
                }
            }
            _isSpawned = found;
        }
        
        /// <summary>
        /// Helper function for activating and deactivating the correct buttons
        /// </summary>
        /// <param name="state">place, draw, spawn, inactive, delete</param>
        private void SetButtonsActivationState(string state)
        {
            switch (state)
            {
                case "place":
                    _placeButton.SetActive(true);
                    if (NetworkUser.LocalInstance?.userRole?.Value?.Name?.Contains("Admin") == true)
                    {
                        _placeButton.GetComponent<Button>().interactable = true;
                    }
                    _drawButton.SetActive(false);
                    _spawnContentButton.SetActive(false);
                    _deleteButton.SetActive(false);
                    break;
                case "draw":
                    _placeButton.SetActive(false);
                    _drawButton.SetActive(true);
                    _spawnContentButton.SetActive(false);
                    _deleteButton.SetActive(true);
                    if (NetworkUser.LocalInstance?.userRole?.Value?.Name?.Contains("Admin") == true)
                    {
                        _deleteButton.GetComponent<Button>().interactable = true;
                    }
                    break;
                case "spawn":
                    _placeButton.SetActive(false);
                    _drawButton.SetActive(false);
                    _spawnContentButton.SetActive(true);
                    _deleteButton.SetActive(true);
                    
                    if (NetworkUser.LocalInstance?.userRole?.Value?.Name?.Contains("Admin") == true)
                    {
                        _spawnContentButton.GetComponent<Button>().interactable = true;
                        _deleteButton.GetComponent<Button>().interactable = true;
                    }
                    break;
                case "inactive":
                    _placeButton.SetActive(false);
                    _drawButton.SetActive(false);
                    _spawnContentButton.SetActive(false);
                    _deleteButton.SetActive(false);
                    break;
                case "delete":
                    _placeButton.SetActive(false);
                    _drawButton.SetActive(false);
                    _spawnContentButton.SetActive(false);
                    _deleteButton.SetActive(true);
                    
                    if (NetworkUser.LocalInstance?.userRole?.Value?.Name?.Contains("Admin") == true)
                    {
                        _deleteButton.GetComponent<Button>().interactable = true;
                    }
                    break;
            }
        }
        
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Set UI text 
        /// </summary>
        /// <param name="areaName"></param>
        public void SetAreaName(string areaName)
        {
            _areaNameText.text = areaName;
        }

        /// <summary>
        /// This function spawns a new default area, sets the configuration for this area and initializes it.
        /// </summary>
        public void PlaceArea()
        {
            VAPlacement.LocalInstance.RequestAreaSpawnRpc(new FixedString32Bytes(_areaNameText.text));
        }
        
        /// <summary>
        /// This function removes the spawned world content and despawns the area
        /// </summary>
        public void RemoveArea()
        {
            if (CustonEnvironmentRaycast.Singleton)
            {
                CustonEnvironmentRaycast.Singleton.ResetRaycastPoints();
            }
            VAPlacement.LocalInstance.RequestDespawnAreaRpc(new FixedString32Bytes(_areaNameText.text));
        }

        /// <summary>
        /// This function spawns the area content
        /// </summary>
        public void SpawnWorldContent()
        {
            VAPlacement.LocalInstance.RequestSpawnAreaContentRpc(_areaName);
        }

        /// <summary>
        /// This function draws the boundary
        /// it collects the points from the local custom environment raycast script
        /// </summary>
        public void DrawBoundary()
        {
            if (CustonEnvironmentRaycast.Singleton.GetObjectPositions().Count > 2)
            {
                if (_areaId != 0)
                {
                    VAPlacement.LocalInstance.RequestCreateBoundaryRpc(_areaId, CustonEnvironmentRaycast.Singleton.GetObjectPositions().ToArray());
                }
            }
            CustonEnvironmentRaycast.Singleton.ResetRaycastPoints();
        }
        
        
        #endregion

    }
}
