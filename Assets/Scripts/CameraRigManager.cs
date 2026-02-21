using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace MyGame
{
    public class CameraRigManager : MonoBehaviour
    {
        public Vector3 offset = new Vector3(0f, 10f, -15f);
        public float followSpeed = 5f;
        public float rotationSpeed = 5f;
        public float followDistance = 30f; // how far behind the dice (adjustable in Inspector)
        public float followHeight = 20f;   // how high above the dice (adjustable in Inspector)
        private float initTimer = 5f;
        private bool isInitialized = false;

        void Start()
        {
            StartCoroutine(WaitAndInitialize());
        }

        private IEnumerator WaitAndInitialize()
        {
            yield return new WaitForSeconds(initTimer);
            isInitialized = true;
        }

        void LateUpdate()
        {
            if (!isInitialized) return;

            List<GameObject> DiceObjects = new List<GameObject>(GameObject.FindGameObjectsWithTag("dice"));
            if (DiceObjects.Count == 0) return;

            // Calculate the average position of all dice
            Vector3 averagePosition = Vector3.zero;
            foreach (GameObject die in DiceObjects)
            {
                averagePosition += die.transform.position;
            }
            averagePosition /= DiceObjects.Count;

            // restrict camera to offsets horizontal plane 
            averagePosition.y = 0f;


            // Get the current waypoint position
            Vector3 waypointPosition = WaypointManager.Instance.GetCurrentWaypointPosition();

            // Dynamic offset: behind the dice in the direction of travel, at a fixed height
            Vector3 travelDirection = (waypointPosition - averagePosition).normalized;
            Vector3 dynamicOffset = -travelDirection * followDistance + Vector3.up * followHeight;
            Vector3 desiredPosition = averagePosition + dynamicOffset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

            // Optionally, advance waypoint if close
            if(Vector3.Distance(averagePosition, waypointPosition) < 10f)
            {
                WaypointManager.Instance.MoveToNextWaypoint();
            }

            // Rotate the camera to face the direction of travel (from dice to waypoint)
            if (travelDirection.sqrMagnitude > 0.01f) // Avoid zero-length direction
            {
                Quaternion desiredRotation = Quaternion.LookRotation(travelDirection.normalized, Vector3.up);
                desiredRotation = Quaternion.Euler(0f, desiredRotation.eulerAngles.y, 0f); // Optional: lock X/Z rotation
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
}