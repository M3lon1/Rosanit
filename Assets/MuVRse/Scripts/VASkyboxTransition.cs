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
using System.Collections;

namespace VRSYS.MuVRse.Scripts{
    
    public class VASkyboxTransition : MonoBehaviour, ITransitionable
    {
        #region Member Variables
        
        [SerializeField] private GameObject _skybox;
        
        private Coroutine _skyboxTransitionCoroutine;
        
        private bool _isTransitioning;
        
        private static readonly int AlphaMultiplier = Shader.PropertyToID("_AlphaMultiplier");
        
        private VAConfiguration _vaConfig;

        private float _duration;

        #endregion

        #region Unity Callbacks
        
        void Start()
        {
            GetConfigSettings();
        }
        
        #endregion
        
        #region Public Methods
        
        private IEnumerator SkyboxTransitionCoroutine(float startAlpha, float targetAlpha)
        {
            _isTransitioning = true;
            float elapsedTime = 0f;
            Material material = _skybox.GetComponent<MeshRenderer>().material;

            while (elapsedTime < _duration)
            {
                elapsedTime += Time.deltaTime;
                float easedT = Mathf.Pow(elapsedTime / _duration, 2);
                float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, easedT);
                material.SetFloat(AlphaMultiplier, currentAlpha);
                yield return null;
            }

            _isTransitioning = false;
        }
        
        public void TransitionIn()
        {
            StartSkyboxTransition(1);
        }

        public void TransitionOut()
        {
            StartSkyboxTransition(0);
        }
        
        #endregion
        
        #region Private Methods
        private void GetConfigSettings()
        {
            _vaConfig = GetComponent<VAConfiguration>();
            _duration = _vaConfig.configuration.transitionDurationSkybox;
        }

        private void StartSkyboxTransition(float targetAlpha)
        {
            float startAlpha = _skybox.GetComponent<MeshRenderer>().material.GetFloat(AlphaMultiplier);
            if (_isTransitioning) StopCoroutine(_skyboxTransitionCoroutine);
            _skyboxTransitionCoroutine = StartCoroutine(SkyboxTransitionCoroutine(startAlpha, targetAlpha));
        }
        
        #endregion  
    }
}