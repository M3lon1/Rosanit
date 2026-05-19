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
using UnityEngine;
using Meta.XR;
using Meta.XR.MRUtilityKit;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace VRSYS.MuVRse.Scripts
{
    

    public class CustonEnvironmentRaycast : MonoBehaviour
    {
        #region Member Variables
        
        public static CustonEnvironmentRaycast Singleton { get; private set; }
        
        [SerializeField] private InputActionReference _placePoint;
        
        [SerializeField] private Transform _raycastAnchor;
        [SerializeField] private GameObject _prefabToPlace;

        [SerializeField] private EnvironmentRaycastManager _raycastManager;

        [SerializeField] private LineRenderer _raycastVisualizationLine;
        [SerializeField] private Transform _raycastVisualizationNormal;
        [SerializeField][Range(0, 3)] private float _yLimit;
        [SerializeField] private List<GameObject> _placedObjects;
        
        private EnvironmentRaycastHitStatus _currentEnvHitStatus;
    
        #endregion
        
        #region Unity Callbacks
        
        void Start()
        {
            if (gameObject.GetComponentInParent<NetworkObject>().IsOwner)
            {
                Singleton = this;
            }
            _placePoint.action.performed += OnPlacePoint;
        }

        private void OnDestroy()
        {
            _placePoint.action.performed -= OnPlacePoint;
        }

        private void OnEnable()
        {
            _placePoint.action.Enable();
        }

        private void OnDisable()
        {
            _placePoint.action.Disable();
        }
        
        void Update()
        {
            if (VAPlacement.LocalInstance.isPlacementModeActive.Value)
            {
                VisualizeRaycast();
            }
        }

        #endregion
        
        #region  Private Functions
        
        /// <summary>
        /// Input callback function to remove the current boundary
        /// </summary>
        /// <param name="context"></param>
        private void OnRemoveBoundaryPoints(InputAction.CallbackContext context)
        {
            if (VAPlacement.LocalInstance.isPlacementModeActive.Value)
            {
                DeleteAllPlacedObjects();
            }
        }
        
        /// <summary>
        /// Input callback function to place a point on the floor 
        /// </summary>
        /// <param name="context"></param>
        private void OnPlacePoint(InputAction.CallbackContext context){
            if (VAPlacement.LocalInstance.isPlacementModeActive.Value)
            {
                var ray = GetRaycastRay();
                TryPlace(ray);
            }
        }

        /// <summary>
        /// Input function for setting the constructed points to the line renderer of the boundary
        /// </summary>
        /// <param name="context"></param>
        private void OnCreateBoundary(InputAction.CallbackContext context)
        {
            List<Vector3> positions = GetObjectPositions();
        }

        /// <summary>
        /// Try to place an object where we hit with a raycast
        /// </summary>
        /// <param name="ray"></param>
        private void TryPlace(Ray ray)
        {
            Debug.Log($"{_raycastManager.Raycast(ray, out var h)}");
            if (_raycastManager.Raycast(ray, out var hit))
            {
                // limit to xz plane
                if (hit.point.y < _yLimit && hit.point.y > -0.5)
                {
                    var objectToPlace = Instantiate(_prefabToPlace);
                    objectToPlace.transform.SetPositionAndRotation(
                        hit.point,
                        Quaternion.LookRotation(hit.normal, Vector3.up)
                    );
                    
                    // If no MRUK component is present in the scene, we add an OVRSpatialAnchor component
                    // to the instantiated prefab to anchor it in the physical space and prevent drift.
                    if (MRUK.Instance?.IsWorldLockActive != true)
                    {
                        objectToPlace.AddComponent<OVRSpatialAnchor>();
                    }
                
                    _placedObjects.Add(objectToPlace);
                }
            }
            else
            {
                Debug.Log("No Hit found");
            }
        }
        
        /// <summary>
        /// visualizes raycast. Raycast from given _raycastAnchor to environment. If a hit is detected the _raycastVisualizationLine (LineRenderer) is updated. point 0 is _raycastAnchor point 1 is environment hit
        /// </summary>
        private void VisualizeRaycast()
                {
                    var ray = GetRaycastRay();
                    bool hasHit = RaycastEnvironment(ray, out var hit) || hit.status == EnvironmentRaycastHitStatus.HitPointOccluded;
                    bool hasNormal = hit.normalConfidence > 0f;
                    _raycastVisualizationLine.enabled = hasHit;
                    _raycastVisualizationNormal.gameObject.SetActive(hasHit && hasNormal);
                    if (hasHit)
                    {
                        _raycastVisualizationLine.SetPosition(0, ray.origin);
                        _raycastVisualizationLine.SetPosition(1, hit.point);
        
                        if (hasNormal)
                        {
                            _raycastVisualizationNormal.SetPositionAndRotation(hit.point, Quaternion.LookRotation(hit.normal));
                        }

                        if (hit.point.y < 0.6 && hit.point.y > -0.6)
                        {
                            _raycastVisualizationLine.startColor = Color.green;
                            _raycastVisualizationLine.endColor = Color.green;
                        }
                        else
                        {
                            _raycastVisualizationLine.startColor = Color.red;
                            _raycastVisualizationLine.endColor = Color.red;
                        }
                    }
        
                }
                
        /// <summary>
        /// Get a ray from the _raycastAnchor
        /// </summary>
        /// <returns></returns>
        private Ray GetRaycastRay()
        {
            return new Ray(_raycastAnchor.position + _raycastAnchor.forward * 0.1f, _raycastAnchor.forward);
        }
        
        /// <summary>
        /// Raycast against the environment with meta sdk 
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="envHit"></param>
        /// <returns></returns>
        private bool RaycastEnvironment(Ray ray, out EnvironmentRaycastHit envHit)
        {
            if (Physics.Raycast(ray, out var physicsHit))
            {
                envHit = new EnvironmentRaycastHit
                {
                    status = EnvironmentRaycastHitStatus.Hit,
                    point = physicsHit.point,
                    normal = physicsHit.normal,
                    normalConfidence = 1f
                };
                return true;
            }
            bool envHitResult = _raycastManager.Raycast(ray, out envHit);
            _currentEnvHitStatus = envHit.status;
            return envHitResult;
        }
        
        #endregion

        #region Context Menu Functions
        
        /// <summary>
        /// Delete all spawned objects
        /// </summary>
        [ContextMenu("Delete all objects")]
        public void DeleteAllPlacedObjects()
        {

            for (int i = _placedObjects.Count - 1; i >= 0; i--)
            {
                var obj = _placedObjects[i];
                _placedObjects.RemoveAt(i);
                DestroyImmediate(obj);
            }
        }
        
        /// <summary>
        /// Just for testing purposes, remove when done
        /// </summary>
        [ContextMenu("Add Point")]
        public void AddObject()
        {
            var objectToPlace = Instantiate(_prefabToPlace);
            objectToPlace.transform.position = Random.onUnitSphere*5;
            _placedObjects.Add(objectToPlace);
        }

        [ContextMenu("Add base points 1")]
        public void AddBasePointsPositive()
        {
            Vector3 p1 = new Vector3(1, 0, 1);
            Vector3 p2 = new Vector3(1, 0, 3);
            Vector3 p3 = new Vector3(3, 0, 3);
            Vector3 p4 = new Vector3(3, 0, 1);
            
            var ob1 = Instantiate(_prefabToPlace);
            var ob2 = Instantiate(_prefabToPlace);
            var ob3 = Instantiate(_prefabToPlace);
            var ob4 = Instantiate(_prefabToPlace);

            ob1.transform.position = p1;
            ob2.transform.position = p2;
            ob3.transform.position = p3;
            ob4.transform.position = p4;
            
            _placedObjects.Add(ob1);
            _placedObjects.Add(ob2);
            _placedObjects.Add(ob3);
            _placedObjects.Add(ob4);

        }
        
        [ContextMenu("Add base points -1")]
        public void AddBasePointsNegative()
        {
            Vector3 p1 = new Vector3(-1, 0, -1);
            Vector3 p2 = new Vector3(-1, 0, -3);
            Vector3 p3 = new Vector3(-3, 0, -3);
            Vector3 p4 = new Vector3(-3, 0, -1);
            
            var ob1 = Instantiate(_prefabToPlace);
            var ob2 = Instantiate(_prefabToPlace);
            var ob3 = Instantiate(_prefabToPlace);
            var ob4 = Instantiate(_prefabToPlace);

            ob1.transform.position = p1;
            ob2.transform.position = p2;
            ob3.transform.position = p3;
            ob4.transform.position = p4;
            
            _placedObjects.Add(ob1);
            _placedObjects.Add(ob2);
            _placedObjects.Add(ob3);
            _placedObjects.Add(ob4);

        }
        
        /// <summary>
        /// Sets the position of all spawned objects as boundary points
        /// </summary>
        [ContextMenu("Set Boundary Points")]
        public void SetPointsToBoundary()
        {
            
            List<Vector3> points = new List<Vector3>();
            foreach (var obj in _placedObjects)
            {
                points.Add(obj.transform.position);
            }

            if (Utils.GetNetworkObject(VAPlacement.LocalInstance.activeAreaId.Value))
            {
                Utils.GetNetworkObject(VAPlacement.LocalInstance.activeAreaId.Value).gameObject.GetComponent<VirtualArea>().SetBoundaryPoints(points);
            }
        }

        [ContextMenu("Create Boundary")]
        public void CreateBoundary()
        {
            List<Vector3> positions = new List<Vector3>();
            positions.Add(Vector3.zero);
            positions.Add(new Vector3(0,0,1));
            positions.Add(new Vector3(1,0,1));
            positions.Add(new Vector3(1,0,0));
            if (Utils.GetNetworkObject(VAPlacement.LocalInstance.activeAreaId.Value))
            {
                Utils.GetNetworkObject(VAPlacement.LocalInstance.activeAreaId.Value).gameObject.GetComponent<VirtualArea>().CreateBoundary(positions);
            }
        }
        
        #endregion

        #region Public Functions

        /// <summary>
        /// Removes all placed points 
        /// </summary>
        public void ResetRaycastPoints()
        {
            DeleteAllPlacedObjects();
        }
        
        /// <summary>
        /// Get the positions of all spawned objects
        /// </summary>
        /// <returns> List </returns>
        public List<Vector3> GetObjectPositions()
        {
            List<Vector3> points = new List<Vector3>();
            foreach (var obj in _placedObjects)
            {
                points.Add(obj.transform.position);
            }

            return points;
        }
        
        #endregion
    }

}