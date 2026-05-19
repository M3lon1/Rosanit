using UnityEngine;

public interface ITransitionable
{
    /// <summary>
    /// Blends the object in
    /// </summary>
    void TransitionIn();
    
    /// <summary>
    /// Blends the object out
    /// </summary>
    void TransitionOut();

    
    private void GetConfigSettings()
    {
        
    }
}
