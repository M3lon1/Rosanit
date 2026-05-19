using UnityEngine;
using VRSYS.MuVRse.Scripts;

public class GhostPreview : MonoBehaviour
{
    [SerializeField] private GameObject _ghostHead;
    [SerializeField] private GameObject _ghostBody;
    

    void Start()
    {
        
    }
    
    void Update()
    {
        
    }

    #region Public Methods

    public void Activate()
    {
        SwitchGhostVisibility(true);
    }

    public void Deactivate()
    {
        SwitchGhostVisibility(false);
    }

    public bool IsActive()
    {
        if (_ghostHead.layer == 0)
        {
            return true;
        }

        return false;
    }
    #endregion
    
    #region Private Metods
    private void SwitchGhostVisibility(bool isActive)
    {
        int layer = isActive ? 0 : 3;
        Utils.SetLayerRecursively(_ghostHead, layer);
        Utils.SetLayerRecursively(_ghostBody, layer);

    }
    #endregion
}
