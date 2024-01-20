using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

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
    public int maxImages = 8;
    public String depthName;
    public Texture2D[] images;
    public Texture2D[] depths;
    public Vector3[] positions;
    public Vector3 camPos;
    public Vector2 resolution;
    private ComputeBuffer intermediateStorage;
    private int x, y;

    void Start()
    {
        if (!(images.Length == depths.Length && depths.Length == positions.Length))
        {
            Debug.LogError("expected parrallel arrays but one length differed");
        }
        x = (int)resolution.x;
        y = (int)resolution.y * 2;
        rtColor = new RenderTexture(x, y, 24);
        intermediateStorage = new ComputeBuffer(x * y, sizeof(uint));
        //rt.format = RenderTextureFormat.ARGB32;
        rtColor.enableRandomWrite = true;
        rtColor.Create();

        rtDepth = new RenderTexture(x, y, 24);
        intermediateStorage = new ComputeBuffer(x * y, sizeof(uint));
        rtDepth.format = RenderTextureFormat.RFloat;
        rtDepth.enableRandomWrite = true;
        rtDepth.Create();

        gameObject.GetComponent<Renderer>().material.mainTexture = rtColor;


        // Find the kernel ID
        clearShaderKernelID = clearShader.FindKernel("CLEAR");
        cdepKernelID = cdepShader.FindKernel("CDEP");
        textureGenKernelID = textureGenShader.FindKernel("RENDERTEXTURE");

        //clear the intermediate data for a new frame
        clearShader.SetBuffer(clearShaderKernelID, "out_rgbd", intermediateStorage);
        clearShader.SetFloats("dims", x, y);
        clearShader.Dispatch(clearShaderKernelID, x / threadGroupSize, y / threadGroupSize, 1);


        for (int i = 0; i < maxImages; i++)
        {
            InitializeOdsTextures(Application.streamingAssetsPath + "/" + depthName, i);

            /* CDEP properties
            float3 camera_position;
            float camera_ipd;
            float camera_focal_dist;
            float z_max;
            float depth_hint;

            Texture2D<float4> image : register(t0);
            Texture2D<float4> depths : register(t1);
            RWStructuredBuffer<uint> out_rgbd : register(u0);
            */

            cdepShader.SetVector("camera_position", new Vector3(1, 1, 1));
            cdepShader.SetFloat("camera_ipd", 1f);
            cdepShader.SetFloat("camera_focal_dist", 1f);
            cdepShader.SetFloat("z_max", 10f);
            cdepShader.SetFloat("depth_hint", 1f);

            cdepShader.SetTexture(cdepKernelID, "image", images[i]);
            cdepShader.SetTexture(cdepKernelID, "depths", depths[i]);
            cdepShader.SetBuffer(cdepKernelID, "out_rgbd", intermediateStorage);

            // Dispatch the shader
            cdepShader.Dispatch(cdepKernelID, x / threadGroupSize, y / threadGroupSize, 1);

            //Render the buffer to the render texture
            textureGenShader.SetBuffer(textureGenKernelID, "_Rgbd", intermediateStorage);
            textureGenShader.SetTexture(textureGenKernelID, "_OutRgba", rtColor);
            textureGenShader.SetTexture(textureGenKernelID, "_OutDepth", rtDepth);
            textureGenShader.SetInts("dims", x, y);
            textureGenShader.SetFloat("z_max", 1);
            textureGenShader.Dispatch(textureGenKernelID, x / threadGroupSize, y / threadGroupSize, 1);
        }
    }

    public void Update()
    {
        clearShader.SetInts("dims", x, y);
        clearShader.SetBuffer(clearShaderKernelID, "out_rgbd", intermediateStorage);
        clearShader.Dispatch(clearShaderKernelID, x / threadGroupSize, y / threadGroupSize, 1);
        for (int i = 0; i < maxImages; i++)
        {
            cdepShader.SetVector("camera_position", camPos);

            cdepShader.SetBuffer(cdepKernelID, "out_rgbd", intermediateStorage);

            // Dispatch the shader
            cdepShader.Dispatch(cdepKernelID, x / threadGroupSize, y / threadGroupSize, 1);

            //Render the buffer to the render texture
            textureGenShader.SetBuffer(textureGenKernelID, "_Rgbd", intermediateStorage);
            textureGenShader.SetTexture(textureGenKernelID, "_OutRgba", rtColor);
            textureGenShader.SetInts("dims", x, y);
            textureGenShader.Dispatch(textureGenKernelID, x / threadGroupSize, y / threadGroupSize, 1);
        }
    }

    void InitializeOdsTextures(string file_name, int index)
    {
        // Load from file path and save as texture - color
        string textureImagePath = file_name + "_" + (index + 1) + ".png";
        byte[] bytes = File.ReadAllBytes(textureImagePath);
        Texture2D loadTexture = new Texture2D(1, 1); //mock size 1x1
        loadTexture.LoadImage(bytes);
        images[index] = loadTexture;

        // Load from file path to texture asset - depth
        string depthImagePath = file_name + "_" + (index + 1) + ".depth";

        byte[] depthBytes = File.ReadAllBytes(depthImagePath);
        // Ensure the byte array length is a multiple of 4 (size of a float)
        if (depthBytes.Length % 4 != 0)
        {
            throw new ArgumentException("Byte array length must be a multiple of 4");
        }

        // Initialize float array
        float[] floatArray = new float[depthBytes.Length / 4];

        // Convert bytes to floats
        for (int i = 0; i < depthBytes.Length; i += 4)
        {
            floatArray[i / 4] = BitConverter.ToSingle(depthBytes, i);
        }

        Color[] colors = new Color[loadTexture.width * loadTexture.height];
        for (int i = 0; i < floatArray.Length; i++)
        {
            float val = floatArray[i];
            colors[floatArray.Length - i - 1] = new Color(val, val, val);
        }

        Texture2D depthLoadTexture = new Texture2D(loadTexture.width, loadTexture.height, TextureFormat.RFloat, false); //mock size 1x1
        depthLoadTexture.SetPixels(colors);
        depthLoadTexture.Apply();
        depths[index] = depthLoadTexture;
    }

    void OnDestroy()
    {
        // Release the compute buffer
        intermediateStorage.Release();
    }
}

