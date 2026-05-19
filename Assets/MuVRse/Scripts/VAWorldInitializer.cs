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

using UnityEngine;

namespace VRSYS.MuVRse.Scripts
{
    public class VAWorldInitializer : MonoBehaviour
    {
        #region Member Variables
        
        [Tooltip("Assign target shader to all children of this object")]
        [SerializeField] private Transform _parentObject;
        [SerializeField] private Shader _targetShader;
        
        [Header("Options")]
        [SerializeField] private bool _changeOnStart = false;
        [SerializeField] private bool _includeInactive = true;
        
        #endregion

        #region Unity Callbacks
        
        void Start()
        {
            if (_changeOnStart && _targetShader != null)
            {
                ChangeShaders();
            }
        }
        
        #endregion
        
        #region Public Methods
        
        [ContextMenu("Change Shaders")]
        public void ChangeShaders()
        {
            if (_parentObject == null)
            {
                Debug.LogWarning("Parent object is not assigned!");
                return;
            }
            
            if (_targetShader == null)
            {
                Debug.LogWarning("Target shader is not assigned!");
                return;
            }
            
            // Get all renderers
            Renderer[] renderers = _includeInactive 
                ? _parentObject.GetComponentsInChildren<Renderer>(true)
                : _parentObject.GetComponentsInChildren<Renderer>();
            
            int materialsChanged = 0;
            
            foreach (Renderer r in renderers)
            {
                foreach (Material material in r.materials)
                {
                    material.shader = _targetShader;
                    materialsChanged++;
                }
            }
            
            Debug.Log($"Changed shader to '{_targetShader.name}' on {materialsChanged} materials across {renderers.Length} renderers.");
        }
        
        #endregion
    }
}
