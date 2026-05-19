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
using System.Drawing;
using Unity.Netcode;
using UnityEngine;


namespace VRSYS.MuVRse.Scripts
{
    public class VABoundary : MonoBehaviour
    {
        #region Member Variables
        
        [SerializeField] private Vector3 _center;
        [SerializeField] private LineRenderer _lineRendererBase;
        
        [SerializeField][Range(0.01f, 1)] private float _gridSizeX = 0.3f;
        [SerializeField][Range(0.01f, 1)] private float _gridSizeY = 0.3f;
        [SerializeField] private float _height = 3;
        [SerializeField] private GameObject _grid;
        
        #endregion

        #region Public Methods
        
        public Vector3 GetCentroid()
        {
            return _center;
        }

        /// <summary>
        /// Calculates the center of the boundary
        /// </summary>
        /// <returns></returns>
        public Vector3 CalculateCentroid()
        {
            _center = Utils.CalculateCentroid(_lineRendererBase);
            return _center;
        }

        public void ResetCentroid()
        {
            _center = Vector3.zero;
        }


        public float GetBoundarySize()
        {
            Vector3[] basePoints = new Vector3[_lineRendererBase.positionCount];
            _lineRendererBase.GetPositions(basePoints);

            float minDistance = Single.PositiveInfinity;
            
            foreach(Vector3 position in basePoints)
            {
                float distance = Vector3.Distance(position, _center);
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }
            Debug.Log($"area size: {minDistance}");
            return minDistance;
        }
        
        /// <summary>
        /// Check if a point is inside closed area defined by the line renderer
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool IsPointInArea(Vector3 point)
        {
            return Utils.IsPointInsideClosedArea(_lineRendererBase, point);
        }

        /// <summary>
        /// Set Points of the line renderer and calculates its center
        /// </summary>
        /// <param name="positions"></param>
        public void SetLineRendererPoints(List<Vector3> positions)
        {
            Vector3[] points = positions.ToArray();
            _lineRendererBase.positionCount = points.Length;
            _lineRendererBase.SetPositions(points);
        }

        /// <summary>
        /// set position count of line renderer to 0
        /// </summary>
        public void ResetLineRendererPoints()
        {
            _lineRendererBase.positionCount = 0;
        }

        /// <summary>
        /// Set Loop to line renderer
        /// </summary>
        public void SetLineRendererLoop(bool value)
        {
            _lineRendererBase.loop = value;
        }

        public float CalculatePlayerDistanceToBoundary(Vector3 playerPosition)
        {
            if (_lineRendererBase.positionCount > 2)
            {
                return Utils.CalculateDistanceToClosedArea(_lineRendererBase, playerPosition);
            }

            return -1;
        }

        public bool IsBoundaryCreated()
        {
            if (_grid.transform.childCount < 0)
            {
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="height"></param>
        /// <param name="gridSize"></param>
        [ContextMenu("grid")]
        public void CreateBoundaryGrid()
        {
            
            // Vector3 gridSize = new Vector3(0.3f, 0.3f);
            int verticalLineCount = Mathf.RoundToInt(CalculateCircumference() / _gridSizeX);
            int horizontalLineCount = Mathf.RoundToInt(_height / _gridSizeY);
            int segments = _lineRendererBase.positionCount;
            
            // Vertical lines
            CreateVerticalLines();
            
            // Horizontal lines
            // copy base line renderer with an height offset
            for (int i = 0; i <= horizontalLineCount; i++)
            {
                float heightOffset = i * _gridSizeY; // Each line at different height
                CopyLineRendererWithNewPointHeight(heightOffset, _lineRendererBase);
            }

            Debug.Log($"Grid was generated for area {GetComponent<VirtualArea>().areaName} : {GetComponent<NetworkObject>().NetworkObjectId}");
        }

        [ContextMenu("delete grid")]
        public void DeleteGrid()
        {
            // Get all children
            int childCount = _grid.transform.childCount;

            // Delete in reverse order to avoid index shifting issues
            for (int i = childCount - 1; i >= 0; i--)
            {
                Transform child = _grid.transform.GetChild(i);

                // Use DestroyImmediate in editor mode
                DestroyImmediate(child.gameObject);
            }
        }

        #endregion
        
        #region Private Methods
        /// <summary>
        /// Calculates the circumference of the base line renderer.
        /// </summary>
        /// <returns></returns>
        private float CalculateCircumference()
        {
            float sum = 0;
            for (int i = 0; i < _lineRendererBase.positionCount - 1; i++)
            {
                sum += Vector3.Distance(_lineRendererBase.GetPosition(i), _lineRendererBase.GetPosition(i + 1));
            }
            sum += Vector3.Distance(_lineRendererBase.GetPosition(_lineRendererBase.positionCount-1), _lineRendererBase.GetPosition(0));
            return sum;
        }
        
        /// <summary>
        /// This function copys a line renderer.
        /// </summary>
        /// <param name="heightAddition">Gives how much is added to every points y value</param>
        /// <param name="lr"></param>
        /// <returns></returns>
        private void CopyLineRendererWithNewPointHeight(float heightAddition, LineRenderer lr)
        {
            GameObject newLineObject = new GameObject($"HorizontalLine_{heightAddition}");
            newLineObject.transform.parent = _grid.transform;
            LineRenderer newLine = newLineObject.AddComponent<LineRenderer>();
            
            Vector3[] basePoints = new Vector3[lr.positionCount];
            lr.GetPositions(basePoints);
            
            for(int i = 0; i < basePoints.Length; i++)
            {
                basePoints[i].y += heightAddition;

            }
            
            newLine.positionCount = lr.positionCount;
            newLine.SetPositions(basePoints);
            newLine.loop = lr.loop;
            newLine.material = _lineRendererBase.sharedMaterial;
            newLine.widthCurve = lr.widthCurve;
            
            return;
        }
        private void CreateVerticalLines()
        {
            Vector3[] basePoints = new Vector3[_lineRendererBase.positionCount];
            _lineRendererBase.GetPositions(basePoints);
    
            // For each segment of the base line
            for (int segmentIndex = 0; segmentIndex < basePoints.Length; segmentIndex++)
            {
                Vector3 startPoint = basePoints[segmentIndex];
                Vector3 endPoint = basePoints[(segmentIndex + 1) % basePoints.Length]; // Wrap around for closed loop
        
                float segmentLength = Vector3.Distance(startPoint, endPoint);
                int verticalLinesInSegment = Mathf.RoundToInt(segmentLength / _gridSizeX);
        
                // Place vertical lines along this segment
                for (int i = 0; i <= verticalLinesInSegment; i++)
                {
                    float t = (float)i / verticalLinesInSegment;
                    Vector3 positionOnSegment = Vector3.Lerp(startPoint, endPoint, t);
            
                    CreateVerticalLine(positionOnSegment, segmentIndex, i);
                }
            }
        }
        
        private void CreateVerticalLine(Vector3 basePosition, int segmentIndex, int lineIndex)
        {
            GameObject vertLineObject = new GameObject($"VerticalLine_Seg{segmentIndex}_Line{lineIndex}");
            vertLineObject.transform.parent = _grid.transform;
            LineRenderer vertLine = vertLineObject.AddComponent<LineRenderer>();
    
            vertLine.material = _lineRendererBase.sharedMaterial;
            vertLine.widthCurve = _lineRendererBase.widthCurve;
    
            vertLine.positionCount = 2;
            vertLine.SetPosition(0, basePosition); // Bottom
            vertLine.SetPosition(1, new Vector3(basePosition.x, basePosition.y + _height, basePosition.z)); // Top
        }
        
        #endregion
        
    }
}