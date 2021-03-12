using UnityEngine;

namespace Complete {
	
    public class CameraFollow : MonoBehaviour {
        
        private Camera m_Camera;                        // Camera reference              
        public float smooth = 0.5f;                     // Smoothness
		public float limitDist = 20.0f;                 // Distance limit
       
        private void FixedUpdate ()
        {
			if (m_Camera == null) {
				m_Camera = GetComponentInChildren<Camera>();
				return;
			}

            Follow();
        }


		private void Follow()
        {
			float currentDist = Vector3.Distance (transform.position, m_Camera.transform.position);

            if (currentDist > limitDist)
            {
                m_Camera.transform.position = Vector3.Lerp (m_Camera.transform.position, transform.position, Time.deltaTime * smooth);
            }
        }
	}
}