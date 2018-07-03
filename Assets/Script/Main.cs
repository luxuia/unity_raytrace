using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using System;
using System.Linq;

public class Main : MonoBehaviour {
    public RawImage img;
    public Text text;

    Stopwatch stopWatch = new Stopwatch();
    NativeArray<Color> backBuffer;
    Texture2D backBufferTex;

    Tracer tracer = new Tracer();

    List<TraceObj> traceObjs = new List<TraceObj>();

    CameraWrap cameraWrap;

    int width, height;
    public float lens_radius;

    public bool DoCapture = true;

    void Start() {
        width = (int)img.rectTransform.rect.width;
        height = (int)img.rectTransform.rect.height;

        backBuffer = new NativeArray<Color>(width * height, Allocator.Persistent);
        backBufferTex = new Texture2D(width, height, TextureFormat.RGBAFloat, false, false);
        img.texture = backBufferTex;

        var objs = GameObject.FindObjectsOfType<MaterialCom>().Where((obj)=>obj.GetComponent<Collider>()!=null);

        foreach (var obj in objs) {
            var collider = obj.GetComponent<Collider>();
            traceObjs.Add(new TraceObj() {
                collider = collider,
                pos = obj.transform.position,
                mat = obj.wrap,
                id = collider.GetInstanceID(),
            });
        }

        Physics.queriesHitBackfaces = true;

        accFrameID = 1;
        //DoUpdate();
    }

    int accFrameID;
    void DoUpdate() {
       
        if (true || DoCapture) {
            DoCapture = false;
            cameraWrap = new CameraWrap(Camera.main, lens_radius);

            int rayCount = 0;
            stopWatch.Start();
            tracer.DoTrace(width, height, cameraWrap, traceObjs, backBuffer, accFrameID, out rayCount);
            stopWatch.Stop();

            float timeDelta = (float)(double)stopWatch.ElapsedTicks/Stopwatch.Frequency;
            text.text = string.Format("FPS {0:F2}, RayCount {1:F2}", 1 / timeDelta, rayCount / timeDelta *10e-6);
            stopWatch.Reset();

            accFrameID++;
            unsafe
            {
                backBufferTex.LoadRawTextureData((IntPtr)backBuffer.GetUnsafeReadOnlyPtr(),
               backBuffer.Length * 16);
            };
            backBufferTex.Apply();
        }
    }

    Vector2 lastPos;
    void Update() {
        DoUpdate();

        if (Input.GetMouseButtonDown(0)) {
            var pos = Input.mousePosition;
            Vector2 uiPos;
            if (RectTransformUtility.RectangleContainsScreenPoint(img.rectTransform, pos)) {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(img.rectTransform,
                    pos, null, out uiPos);

                uiPos.x += width;
                lastPos = uiPos;

                tracer.PixelDebug(cameraWrap, uiPos.x / width, uiPos.y / height);
            }
        } else if (Input.GetKeyDown(KeyCode.R)) {

            tracer.PixelDebug(cameraWrap, lastPos.x / width, lastPos.y / height);
        }
    }

    void OnDestroy() {
        backBuffer.Dispose();
    }
}
