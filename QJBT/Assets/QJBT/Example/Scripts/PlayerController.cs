using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    float _speed = 2.5f;
    float _angularSpeed = 45.0f;
	// Update is called once per frame
	void Update () {
        if (Input.GetKey(KeyCode.UpArrow))
        {
            transform.position += transform.forward * Time.deltaTime * _speed;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            transform.position += transform.forward * Time.deltaTime * -_speed;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            transform.rotation *= Quaternion.Euler(0.0f, _angularSpeed * Time.deltaTime, 0.0f);
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            transform.rotation *= Quaternion.Euler(0.0f, -_angularSpeed * Time.deltaTime, 0.0f);
        }

    }
}
