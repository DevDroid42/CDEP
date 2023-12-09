using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CDEPMeshGeneration : MeshGeneration
{
    // Start is called before the first frame update
    public override void Setup()
    {
        base.Setup();
    }
     
    public void SetCamPos(Vector3 cameraPos)
    {
        renderer.material.SetVector("_camera_position", cameraPos);
    }

    public void SetCameraIndex(int idx)
    {
        renderer.material.SetFloat("_img_index", idx);
    }
}
