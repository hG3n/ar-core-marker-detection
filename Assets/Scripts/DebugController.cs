using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugController : MonoBehaviour
{

    public GameObject arCoreCamera;
    public int distanceToArCoreCamera;

    private int screenWidth_;
    private int screenHeight_;

    private Camera arCam_;
    private Camera dbCam_;

    private CurrentCamera CurrentCameraMode_ = CurrentCamera.ARCore;

    private enum CurrentCamera
    {
        ARCore = 0,
        Debug = 1
    }


    // Start is called before the first frame update
    void Start()
    {
        screenWidth_ = Screen.width;
        screenHeight_ = Screen.height;

        arCam_ = arCoreCamera.GetComponent<Camera>();
        dbCam_ = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var touch in Input.touches)
        {
            if (touch.phase == TouchPhase.Began)
            {
                if (touch.position.x > screenWidth_ / 2)
                {
                    _SwitchCamera();
                }
                else
                {
                    Debug.Log("Left touch received!");
                }
            }
        }

        if (CurrentCameraMode_ == CurrentCamera.Debug)
        {
            _UpdateDebugCameraPosition();
        }


    }


    private void _SwitchCamera()
    {
        if (arCam_.enabled)
        {
            arCam_.enabled = false;
            dbCam_.enabled = true;
            CurrentCameraMode_ = CurrentCamera.Debug;
        }
        else
        {
            arCam_.enabled = true;
            dbCam_.enabled = false;
            CurrentCameraMode_ = CurrentCamera.ARCore;
        }

    }

    private void _UpdateDebugCameraPosition()
    {
        Vector3 new_pos = distanceToArCoreCamera * (-arCam_.transform.forward) - arCam_.transform.position;

        dbCam_.transform.position = new_pos;
        dbCam_.transform.LookAt(arCam_.transform.position);
        dbCam_.transform.rotation = arCam_.transform.rotation;
    }

}
