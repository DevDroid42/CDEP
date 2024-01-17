using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public class CDEPShaderDispatch : MonoBehaviour
{
    public RenderTexture rt;
    public ComputeShader computeShader;
    private ComputeBuffer computeBuffer;
    public int maxImages = 8;
    public String depthName;
    public Texture2D[] images;
    public Texture2D[] depths;
    public Vector3[] positions;
    private int kernelID;
    
    void Start()
    {
        if (!(images.Length == depths.Length && depths.Length == positions.Length))
        {
            Debug.LogError("expected parrallel arrays but one length differed");
        }

        rt = new RenderTexture(1024, 1024, 24);
        //rt.format = RenderTextureFormat.ARGB32;
        rt.enableRandomWrite = true;
        rt.Create();
        gameObject.GetComponent<Renderer>().material.mainTexture = rt;

        // Find the kernel ID
        kernelID = computeShader.FindKernel("CDEP");
        for (int i = 0; i < maxImages; i++)
        {
            InitializeOdsTextures(Application.streamingAssetsPath + "/" + depthName, i);

            /*
            float3 camera_position;
            float camera_ipd;
            float camera_focal_dist;
            float z_max;
            float depth_hint;
            */

            computeShader.SetVector("camera_position", new Vector3(0, 0, 0));
            computeShader.SetFloat("camera_ipd", 0f);
            computeShader.SetFloat("camera_focal_dist", 0f);
            computeShader.SetFloat("z_max", 0f);
            computeShader.SetFloat("depth_hint", 0f);

            computeShader.SetTexture(kernelID, "image", images[i]);
            computeShader.SetTexture(kernelID, "depths", depths[i]);
            computeShader.SetTexture(kernelID, "out_rgbd", rt);

            // Dispatch the shader
            computeShader.Dispatch(kernelID, 1, 1, 1);
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
        computeBuffer.Release();
    }
}
