using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;


namespace VRSYS.MuVRse.Scripts
{
    public class VAGlobalInfo : NetworkBehaviour
    {
        #region Member Variables
        
        public static VAGlobalInfo LocalInstance { get; private set; }
        
        public List<PlayerAreaManager> players = new List<PlayerAreaManager>();
        public List<VirtualArea> virtualAreas = new List<VirtualArea>();
        public NetworkList<ulong> virtualAreaContentIds;
        public NetworkList<ulong> virtualAreaIds;
        public UnityEvent playerChangedArea;
        
        #endregion
        
        #region Unity Callbacks
        
        private void Awake()
        {
            if (LocalInstance == null)
            {
                LocalInstance = this;
            }
            else Debug.Log("Two VAGlobalInfo Scripts are present in the scene. Make sure only one is present");
            virtualAreaIds = new NetworkList<ulong>();
            virtualAreaContentIds = new NetworkList<ulong>();

        }
        
        #endregion
        
        #region Public Methods
        
        public void RegisterUser(PlayerAreaManager player)
        {
            players.Add(player);
        }

        public void RegisterVirtualArea(VirtualArea area, ulong areaId)
        {
            virtualAreas.Add(area);
            virtualAreaIds.Add(areaId);
        }

        public void UnregisterVirtualArea(VirtualArea area, ulong areaId)
        {
            virtualAreas.Remove(area);
            virtualAreaIds.Remove(areaId);
        } 
        
        #endregion
    }
}
