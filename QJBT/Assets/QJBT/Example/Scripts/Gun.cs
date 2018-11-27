using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public LineRenderer _lineRenderer;
    float _firaRate = 0.5f;
    float _lastFireTime = 0.0f;

    void Start()
    {
        _lineRenderer.enabled = false;
    }

    public void Shoot()
    {
        if(Time.time - _lastFireTime > _firaRate)
        {
            var line = new LineRenderer();
            _lastFireTime = Time.time;
            _lineRenderer.SetPosition(0, transform.position);
            _lineRenderer.SetPosition(1, transform.position + transform.forward * 1000.0f);
            StartCoroutine(ShowGunLine());
        }
    }

    IEnumerator ShowGunLine()
    {
        _lineRenderer.enabled = true;
        yield return new WaitForSeconds(0.02f);
        _lineRenderer.enabled = false;
    }
}
