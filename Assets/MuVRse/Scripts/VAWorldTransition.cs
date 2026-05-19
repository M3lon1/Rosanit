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
using System.Linq;
using System.Collections;
using DG.Tweening;

namespace VRSYS.MuVRse.Scripts
{
    public class VAWorldTransition : MonoBehaviour, ITransitionable
    {
        #region Member Variables
        
        [Header("Object References")] 
        [SerializeField] private GameObject _worldContent;

        [Tooltip("Sphere used for blending in the world. Please create a sphere game object at the center of the virtual area and disble its mesh renderer. The default sphere can be found under" +
                 "Root/Transition/Sphere")]
        [SerializeField] private GameObject _sphere;
        
        private static readonly int AlphaMultiplier = Shader.PropertyToID("_AlphaMultiplier");
        private static readonly int SphereRadius = Shader.PropertyToID("_SphereRadius");
        private static readonly int SphereCenter = Shader.PropertyToID("_SphereCenter");

        private Coroutine _worldTransitionCoroutine;
        
        private bool _isTransitioning;
        private float _areaSize;
        private float _minAreaSize;
        private float _worldTransitionTime;
        private Vector3 _sphereCenter;
        private Material[] _cachedMaterials;
        private VAConfiguration _vaConfig;
    
        
        #endregion
        
        #region Unity Callback Functions 
        
        void Start()
        {
            GetConfigSettings();
        }

        #endregion
        
        #region Public Methods

        public void InitializeWorld(GameObject world, float minAreaSize, Vector3 sphereCenter)
        {
            _worldContent = world;
            CacheRenderers();
            _minAreaSize = minAreaSize;
            _sphereCenter = sphereCenter;
            Debug.Log($"$min Area size {minAreaSize}");
            // Setze den Sphere Radius auf die größe der Area. 
            // Was ist denn die größe der area ? Der kleinste Abstand vom center zu einem äußeren punkt
            SetShaderPropertyForAllCachedMaterials(SphereRadius, f: _minAreaSize / 2);
            SetShaderPropertyForAllCachedMaterials(SphereCenter, v: sphereCenter);

        }

        /// <summary>
        /// This function starts scaling the sphere game object given by the inspector with DOTween scale function & the InOutExpo ease function.
        /// It also starts the coroutine for setting the _SphereRadius property of all cached materials and sets it to the scale / 2 of the sphere.
        /// </summary>
        /// <param name="enter"></param>
        public void StartTransitionWithDOTween(bool enter)
        {
            if(_isTransitioning) StopCoroutine(_worldTransitionCoroutine);
            if (enter)
            {
                _sphere.transform.DOScale(_areaSize, _worldTransitionTime).SetEase(Ease.InOutExpo);
            }
            else
            {
                _sphere.transform.DOScale(_minAreaSize, _worldTransitionTime).SetEase(Ease.InOutExpo);
            }

            _worldTransitionCoroutine = StartCoroutine(SetSphereRadiusCoroutine(_worldTransitionTime));
        }
        
        /// <summary>
        /// Thist function starts the transition of the world objects.
        /// When enter is true we increase the _SphereRadius property of each shader instance on each object inside the world object
        /// When enter is false we decrease to 0
        /// 
        /// ToDo
        /// Currently we only have linear transition. Built something to ease in / out 
        /// 
        /// </summary>
        /// <param name="enter"></param>
        public void StartTransitioningWithoutDOTween(bool enter)
        {
            if(_isTransitioning) StopCoroutine(_worldTransitionCoroutine);
            if (enter)
            {
                
                _worldTransitionCoroutine = StartCoroutine(TranisitionCoroutineWithoutDOTween(_worldTransitionTime, _areaSize));
            }
            else
            {
                _worldTransitionCoroutine = StartCoroutine(TranisitionCoroutineWithoutDOTween(_worldTransitionTime, 0));
            }
        }
        
        public void TransitionIn()
        {
            StartTransitionWithDOTween(true);
        }

        public void TransitionOut()
        {
            StartTransitionWithDOTween(false);
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// This function gets all needed configuration values
        /// </summary>
        private void GetConfigSettings()
        {
            _vaConfig = GetComponent<VAConfiguration>();
            _areaSize = _vaConfig.configuration.areaSize;
            _worldTransitionTime = _vaConfig.configuration.transitionDurationWorld;
        }

        /// <summary>
        /// This function sets the _SphereRadius property of all cached materials to the local scale / 2 of the sphere given by the inspector
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        private IEnumerator SetSphereRadiusCoroutine(float duration)
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                SetShaderPropertyForAllCachedMaterials(SphereRadius, _sphere.transform.localScale.x / 2);
                yield return null;
            }
        }
        
        /// <summary>
        /// This function is the coroutine for setting the _SphereRadius property of all cached materials
        /// So far linear
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="targetRadius"></param>
        /// <returns></returns>
        private IEnumerator TranisitionCoroutineWithoutDOTween(float duration, float targetRadius)
        {
            Debug.Log("Start transition coroutine");
            _isTransitioning = true;
            float startRadius = _cachedMaterials[0].GetFloat(SphereRadius);
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                float currentRadius = Mathf.Lerp(startRadius, targetRadius, t);
                SetShaderPropertyForAllCachedMaterials(SphereRadius, currentRadius);
                yield return null;
            }
            
            _isTransitioning = false;
        } 
        
        /// <summary>
        /// This function sets the value of a given shader property for all cached materials
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        private void SetShaderPropertyForAllCachedMaterials(int id, float? f = null, Vector3? v = null)
        {
            if (_cachedMaterials == null) return;
            foreach (var m in _cachedMaterials)
            {
                if (f.HasValue)
                {
                    m.SetFloat(id, f.Value);
                }
                if (v.HasValue)
                {
                    m.SetVector(id, v.Value);
                }
            }
        }
        
        
        /// <summary>
        /// This function collects all mesh renderers and skinned mesh renderers from each child of the world game object
        /// </summary>
        private void CacheRenderers()
        {
            var worldRenderers = _worldContent.GetComponentsInChildren<MeshRenderer>();
            var skinnedRendereres  = _worldContent.GetComponentsInChildren<SkinnedMeshRenderer>();
        
            // Cache materials that have the property
            _cachedMaterials = worldRenderers
                .SelectMany(r => r.materials)
                .Concat(skinnedRendereres.SelectMany(r => r.materials))
                .Where(m => m.HasProperty(SphereRadius))
                .ToArray();
        }
        
        #endregion


    }
}