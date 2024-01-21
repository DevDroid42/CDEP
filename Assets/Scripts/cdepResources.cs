using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace cdep
{
    public class cdepResources : MonoBehaviour
    {
        public static Capture[] InitializeOdsTextures(string file_name, Vector3[] positions, int count)
        {
            Capture[] caps = new Capture[count];
            for (int i = 0; i < count; i++)
            {
                caps[i] = new Capture();
                // Load from file path and save as texture - color
                string textureImagePath = file_name + "_" + (i + 1) + ".png";
                byte[] bytes = File.ReadAllBytes(textureImagePath);
                Texture2D loadTexture = new Texture2D(1, 1); //mock size 1x1
                loadTexture.LoadImage(bytes);
                caps[i].image = loadTexture;

                // Load from file path to texture asset - depth
                string depthImagePath = file_name + "_" + (i + 1) + ".depth";

                byte[] depthBytes = File.ReadAllBytes(depthImagePath);
                // Ensure the byte array length is a multiple of 4 (size of a float)
                if (depthBytes.Length % 4 != 0)
                {
                    throw new FormatException("Byte array length must be a multiple of 4");
                }

                // Initialize float array
                float[] floatArray = new float[depthBytes.Length / 4];

                // Convert bytes to floats
                for (int j = 0; j < depthBytes.Length; j += 4)
                {
                    floatArray[j / 4] = BitConverter.ToSingle(depthBytes, j);
                }

                Color[] colors = new Color[loadTexture.width * loadTexture.height];
                for (int j = 0; j < floatArray.Length; j++)
                {
                    float val = floatArray[j];
                    colors[floatArray.Length - j - 1] = new Color(val, val, val);
                }

                Texture2D depthLoadTexture = new Texture2D(loadTexture.width, loadTexture.height, TextureFormat.RFloat, false); //mock size 1x1
                depthLoadTexture.SetPixels(colors);
                depthLoadTexture.Apply();
                caps[i].depth = depthLoadTexture;
                caps[i].position = new Vector3(positions[i].x, -positions[i].y, positions[i].z);
            }
            return caps;
        }
    }
    public class Capture
    {
        public Texture2D image;
        public Texture2D depth;
        public Vector3 position;
        public MeshGeneration meshGenScript;
    }
}
