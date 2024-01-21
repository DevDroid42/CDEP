using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using cdep;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

public class CDEPShaderDispatch : MonoBehaviour
{
    public RenderTexture rtColor, rtDepth;
    public ComputeShader clearShader;
    private int clearShaderKernelID;
    public ComputeShader cdepShader;
    private int cdepKernelID;
    public ComputeShader textureGenShader;
    private int textureGenKernelID;
    public int threadGroupSize = 8;
    public int ImagesToLoad = 8;
    public int ImagesToRender = 8;
    public String depthName;
    public Vector3[] positions;
    public Vector3 camPos;
    public Vector2 resolution;

    private ComputeBuffer intermediateStorage;
    private int x, y;
    private List<Capture> captures;

    void Start()
    {
        x = (int)resolution.x;
        y = (int)resolution.y * 2;
        rtColor = new RenderTexture(x, y, 24);
        intermediateStorage = new ComputeBuffer(x * y, sizeof(uint));
        rtColor.enableRandomWrite = true;
        rtColor.Create();

        rtDepth = new RenderTexture(x, y, 24);
        intermediateStorage = new ComputeBuffer(x * y, sizeof(uint));
        rtDepth.format = RenderTextureFormat.RFloat;
        rtDepth.enableRandomWrite = true;
        rtDepth.Create();

        gameObject.GetComponent<Renderer>().material.mainTexture = rtColor;


        // Find the kernel IDs
        clearShaderKernelID = clearShader.FindKernel("CLEAR");
        cdepKernelID = cdepShader.FindKernel("CDEP");
        textureGenKernelID = textureGenShader.FindKernel("RENDERTEXTURE");


        clearShader.SetBuffer(clearShaderKernelID, "out_rgbd", intermediateStorage);
        clearShader.SetInts("dims", x, y);

        textureGenShader.SetBuffer(textureGenKernelID, "_Rgbd", intermediateStorage);
        textureGenShader.SetTexture(textureGenKernelID, "_OutRgba", rtColor);
        textureGenShader.SetTexture(textureGenKernelID, "_OutDepth", rtDepth);
        textureGenShader.SetInts("dims", x, y);
        textureGenShader.SetFloat("z_max", 1);

        cdepShader.SetFloat("camera_ipd", 1f);
        cdepShader.SetFloat("camera_focal_dist", 1f);
        cdepShader.SetFloat("z_max", 10f);
        cdepShader.SetFloat("depth_hint", 1f);

        captures = cdepResources.InitializeOdsTextures(Application.streamingAssetsPath + "/" + depthName, positions, ImagesToLoad).ToList();

        cdepShader.SetBuffer(cdepKernelID, "out_rgbd", intermediateStorage);

        //Render the buffer to the render texture    
        textureGenShader.Dispatch(textureGenKernelID, x / threadGroupSize, y / threadGroupSize, 1);

    }

    public void Update()
    {
        clearShader.Dispatch(clearShaderKernelID, x / threadGroupSize, y / threadGroupSize, 1);
        //so unity cam correctly maps to new space
        Vector3 cdepCameraPosition = new Vector3(camPos.z, camPos.y, camPos.x);
        captures = captures.OrderBy(x => Vector3.Distance(x.position, cdepCameraPosition)).ToList();
        for (int i = 0; i < Math.Min(ImagesToLoad, captures.Count); i++)
        {
            cdepShader.SetVector("camera_position", cdepCameraPosition - captures[i].position);
            cdepShader.SetTexture(cdepKernelID, "image", captures[i].image);
            cdepShader.SetTexture(cdepKernelID, "depths", captures[i].depth);

            // Dispatch the shader
            cdepShader.Dispatch(cdepKernelID, x / threadGroupSize, y / threadGroupSize, 1);

            //Render the buffer to the render texture
            textureGenShader.Dispatch(textureGenKernelID, x / threadGroupSize, y / threadGroupSize, 1);
        }
    }

    void OnDestroy()
    {
        // Release the compute buffer
        intermediateStorage.Release();
    }
}