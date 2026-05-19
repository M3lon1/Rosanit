using Unity.Netcode;
using UnityEngine;

namespace VRSYS.MuVRse.Scripts
{
    
    public static class Utils
    {
        
        /// <summary>
        /// Calculates the center of an area defined by a line renderer
        /// </summary>
        /// <param name="lineRenderer"></param>
        /// <returns></returns>
        public static Vector3 CalculateCentroid(LineRenderer lineRenderer)
        {
            if (lineRenderer == null || lineRenderer.positionCount < 3)
            {
                Debug.LogError("LineRenderer ist nicht gesetzt oder hat nicht genügend Punkte, um eine Fläche zu definieren.");
                return Vector3.zero;
            }

            Vector3[] positions = new Vector3[lineRenderer.positionCount];
            lineRenderer.GetPositions(positions);

            Vector3 centroid = Vector3.zero;
            float signedArea = 0.0f;

            for (int i = 0; i < positions.Length; i++)
            {
                Vector3 current = positions[i];
                Vector3 next = positions[(i + 1) % positions.Length];

                float a = current.x * next.z - next.x * current.z;
                signedArea += a;
                centroid += (current + next) * a;
            }

            signedArea *= 0.5f;
            centroid /= (6.0f * signedArea);

            return centroid;
        }
        
        /// <summary>
        /// Calculate if a point is inside a closed area defined by a line renderer
        /// </summary>
        /// <param name="lineRenderer"></param>
        /// <param name="targetPosition"></param>
        /// <returns></returns>
        public static bool IsPointInsideClosedArea(LineRenderer lineRenderer, Vector3 targetPosition)
        {
            int pointCount = lineRenderer.positionCount;
            if (pointCount < 3)
            {
                // Debug.LogError("Der LineRenderer hat nicht genügend Punkte, um eine geschlossene Fläche zu definieren.");
                return false;
            }

            Vector3[] positions = new Vector3[pointCount];
            lineRenderer.GetPositions(positions);

            int intersections = 0;

            // Iteriere durch alle Liniensegmente der geschlossenen Fläche
            for (int i = 0; i < pointCount; i++)
            {
                Vector3 startPoint = positions[i];
                Vector3 endPoint = positions[(i + 1) % pointCount]; // Schließe die Fläche, indem du zum ersten Punkt zurückkehrst

                // Prüfe, ob die Linie von targetPosition nach rechts das Liniensegment schneidet
                if (DoesRayIntersectLineSegment(targetPosition, startPoint, endPoint))
                {
                    intersections++;
                }
            }

            // Wenn die Anzahl der Schnittpunkte ungerade ist, befindet sich der Punkt innerhalb der Fläche
            return (intersections % 2) != 0;
        }
        
        /// <summary>
        /// Helper function for IsPointInsideClosedArea. 
        /// </summary>
        /// <param name="point"></param>
        /// <param name="lineStart"></param>
        /// <param name="lineEnd"></param>
        /// <returns></returns>
        public static bool DoesRayIntersectLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            // Prüfe, ob die z-Koordinate des Punktes zwischen den z-Koordinaten des Liniensegments liegt
            if ((point.z > Mathf.Min(lineStart.z, lineEnd.z) && point.z <= Mathf.Max(lineStart.z, lineEnd.z)) &&
                (point.x <= Mathf.Max(lineStart.x, lineEnd.x)))
            {
                float xIntersection = lineStart.x + (point.z - lineStart.z) * (lineEnd.x - lineStart.x) / (lineEnd.z - lineStart.z);

                // Prüfe, ob der Punkt links vom Liniensegment liegt
                if (point.x <= xIntersection)
                {
                    return true;
                }
            }

            return false;
        }
        
        /// <summary>
        /// Caluclate the distance form a position (Vector3) to a defíned closed area from a line renderer
        /// </summary>
        /// <param name="lineRenderer"> LineRenderer </param>
        /// <param name="targetPosition"> Vector3 </param>
        /// <returns></returns>
        public static float CalculateDistanceToClosedArea(LineRenderer lineRenderer, Vector3 targetPosition)
        {
            int pointCount = lineRenderer.positionCount;
            if (pointCount < 2)
            {
                Debug.LogError("Der LineRenderer hat nicht genügend Punkte, um eine Fläche zu definieren.");
                return float.MaxValue;
            }

            Vector3[] positions = new Vector3[pointCount];
            lineRenderer.GetPositions(positions);

            float minDistance = float.MaxValue;

            // Iteriere durch alle Liniensegmente der geschlossenen Fläche
            for (int i = 0; i < pointCount; i++)
            {
                Vector3 startPoint = positions[i];
                Vector3 endPoint = positions[(i + 1) % pointCount]; // Schließe die Fläche, indem du zum ersten Punkt zurückkehrst

                // Berechne die kürzeste Distanz vom Zielpunkt zum aktuellen Liniensegment
                float distance = DistancePointToLineSegment(targetPosition, startPoint, endPoint);
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }

            return minDistance;
        }
        
        /// <summary>
        /// Calculate the distance from a a point to a line defined by two points
        /// </summary>
        /// <param name="point"> Vector3 </param>
        /// <param name="lineStart"> Vector3 </param>
        /// <param name="lineEnd"> Vector3 </param>
        /// <returns></returns>
        public static float DistancePointToLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            Vector3 lineDirection = lineEnd - lineStart;
            float lineLength = lineDirection.magnitude;
            lineDirection.Normalize();

            float projection = Vector3.Dot(point - lineStart, lineDirection);
            projection = Mathf.Clamp(projection, 0, lineLength);

            Vector3 closestPoint = lineStart + projection * lineDirection;
            return Vector3.Distance(point, closestPoint);
        }

        public static GameObject GetNetworkObject(ulong networkId)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkId, out NetworkObject networkObject))
            {
                GameObject gameObject = networkObject.gameObject;
                return gameObject;
            }

            return null;
        }
        
        public static void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
        
    }

}