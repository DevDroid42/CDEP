using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeShaderDispatch : MonoBehaviour
{
    public ComputeShader shader;
    public RenderTexture rt;
    public GameObject plane;
    public Texture2D[] textures;
    // Start is called before the first frame update
    void Start()
    {
        rt = new RenderTexture(1024,1024,24);
        rt.enableRandomWrite = true;
        rt.Create();
        plane.GetComponent<Renderer>().material.mainTexture = rt;  
        shader.SetTexture(0, "Result", rt);

        // Create a new Texture2DArray
        Texture2DArray textureArray = new Texture2DArray(2048, 1024, 8, TextureFormat.RGB24, false);

        // Fill the Texture2DArray with your textures
        for (int i = 0; i < textures.Length; i++)
        {
            // Here you would normally use your own textures,
            // but for this example we'll just use a white texture
            Texture2D tex = new Texture2D(256, 256, TextureFormat.ARGB32, false);
            tex.SetPixels32(new Color32[256 * 256]);
            tex.Apply();

            // Copy the texture data into the Texture2DArray
            Graphics.CopyTexture(tex, 0, 0, textureArray, i, 0);
        }

        // Set the Texture2DArray as a global texture in the shader
        shader.SetTexture(0, "rendTexArray", textureArray);

        shader.Dispatch(0, rt.width / 8, rt.height / 8, 1);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
