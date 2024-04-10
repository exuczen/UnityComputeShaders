using UnityEngine;
using System.Collections.Generic;

namespace PBDFluid
{
    public class FreeCam : MonoBehaviour
    {
        public float m_speed = 50.0f;

        public float sensitivityX = 15;
        public float sensitivityY = 15;

        public float minimumX = -360;
        public float maximumX = 360;

        public float minimumY = -89F;
        public float maximumY = 89;

        private float rotationY = 0F;

        void Start()
        {
            rotationY = transform.localEulerAngles.x;
            transform.localEulerAngles = new Vector3(rotationY, transform.localEulerAngles.y, 0);
        }

        void Update()
        {
            float speed = m_speed;

            if (Input.GetKey(KeyCode.Space)) speed *= 10.0f;

            Vector3 move = new(0, 0, 0);

            float deltaTime = Time.deltaTime;

            //move left
            if (Input.GetKey(KeyCode.A))
                move = deltaTime * speed * new Vector3(-1, 0, 0);

            //move right
            if (Input.GetKey(KeyCode.D))
                move = deltaTime * speed * new Vector3(1, 0, 0);

            //move forward
            if (Input.GetKey(KeyCode.W))
                move = deltaTime * speed * new Vector3(0, 0, 1);

            //move back
            if (Input.GetKey(KeyCode.S))
                move = deltaTime * speed * new Vector3(0, 0, -1);

            //move up
            if (Input.GetKey(KeyCode.Q))
                move = deltaTime * speed * new Vector3(0, -1, 0);

            //move down
            if (Input.GetKey(KeyCode.E))
                move = deltaTime * speed * new Vector3(0, 1, 0);


            transform.Translate(move);

            if (Input.GetMouseButton(1))
            {
                float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX;

                rotationY -= Input.GetAxis("Mouse Y") * sensitivityY;
                rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

                transform.localEulerAngles = new Vector3(rotationY, rotationX, 0);
            }
        }
    }
}
