using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using System.Linq;
using static Helper;

public class Tracer {

    const int SAMPLE_PER_PIXEL = 200;
    const int MAX_DEPTH = 10;

    Dictionary<int, TraceObj> scene;

    bool Scatter(ref RaycastHit hit, ref Ray ray, ref TraceObj obj, out Color atten, out Ray scatter, out Color light, ref int rayCount) {
        var mat = obj.mat;
        light = Color.black;
        atten = default(Color);
        scatter = default(Ray);

        switch (mat.type)
        {
            case MaterialWrap.Type.Lambert:
                var hitpos = hit.point;
                var target = hitpos + hit.normal + Random.insideUnitSphere;
                scatter = new Ray(hitpos, (target - hitpos).normalized);
                atten = mat.albedo;
                return true;
            case MaterialWrap.Type.Metal:
                var reflect = Vector3.Reflect(ray.direction, hit.normal);
                
                scatter = new Ray(hit.point, (reflect
                    +mat.roughness*Random.insideUnitSphere).normalized); // 随机发射，模拟粗糙的表面
                atten = mat.albedo;

                return Vector3.Dot(reflect, hit.normal) > 0;
            case MaterialWrap.Type.Dielectric:
                atten = Color.white;
                float nint;
                Vector3 normal;
                float cosine;
                if (Vector3.Dot(ray.direction, hit.normal) > 0) {
                    normal = -hit.normal;
                    nint = mat.ri;
                    cosine = mat.ri * Vector3.Dot(ray.direction, hit.normal);
                } else {
                    normal = hit.normal;
                    nint = 1 / mat.ri;
                    cosine = -Vector3.Dot(ray.direction, hit.normal);
                }

                float reflProb;

                Vector3 refracted;
                if (Refract(ray.direction, normal, nint, out refracted )) {
                    reflProb = Schlick(cosine, mat.ri);
                } else {
                    reflProb = 1;
                }
                if (Random.Range(0, 1.0f) < reflProb) {
                    scatter = new Ray(hit.point, Vector3.Reflect(ray.direction, hit.normal));
                } else {
                    scatter = new Ray(hit.point, refracted.normalized);
                }

                break;
            default:
                return false;
        }
        return true;
    }

    Color Trace(ref Ray ray, int depth, ref int rayCount, List<Vector3> debugInfo) {
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit)) {
            var obj = scene[hit.collider.GetInstanceID()];

            if (debugInfo != null) {
                debugInfo.Add(hit.point);
            }

            Ray scatter;
            Color attenuation;
            Color light;
            var color = obj.mat.emissive; // 自发光颜色
            if (depth < MAX_DEPTH && Scatter(ref hit, ref ray, ref obj, out attenuation, out scatter, out light, ref rayCount)) {


                return color + light + attenuation * Trace(ref scatter, depth + 1, ref rayCount, debugInfo);
            } else {
                return color;
            }
        } else {
            if(debugInfo!=null)
                debugInfo.Add(ray.direction);

            // sky
            Vector3 unitDir = ray.direction;
            float t = 0.5f * (unitDir.y + 1.0f);
            return ((1.0f - t) * new Color(1.0f, 1.0f, 1.0f) + t * new Color(0.5f, 0.7f, 1.0f)) * 0.3f;
        }
    }
    
    public void PixelDebug(CameraWrap camera, float u, float v) {
        var infos = new List<Vector3>();
        
        Ray r = camera.GetRay(u, v);
        int rayCount = 0;
        infos.Add(r.origin);

        Trace(ref r, 0, ref rayCount, infos);

        for (int i = 0; i < infos.Count -2; ++i) {
            Debug.DrawLine(infos[i], infos[i + 1], Color.red, 10);
        }
        if (infos.Count >= 2) {
            Debug.DrawRay(infos[infos.Count - 2], infos[infos.Count - 1], Color.red, 10);
        }
    }

    int TraceRowJob(int y, int width, int height, NativeArray<Color> backbuffer, ref CameraWrap camera) {
        int rayCount = 0;

        for (int x = 0; x < width; ++x) {
            Color color = Color.black;
            for (int i =0; i < SAMPLE_PER_PIXEL; ++i) {
                float u = (x + Random.Range(0, 1.0f))/width;
                float v = (y + Random.Range(0, 1.0f))/height;

                Ray r = camera.GetRay(u, v);

                color += Trace(ref r, 0, ref rayCount, null);
            }
            color /= SAMPLE_PER_PIXEL;
            color = color.gamma;

            var old = backbuffer[y * width + x];
            backbuffer[y * width + x] = color;// (old+color)/2;
        }

        return rayCount;
    }

	public void DoTrace(int width, int height, CameraWrap camera, List<TraceObj> objs, NativeArray<Color> backBuffer, out int rayCount) {
        rayCount = 0;
        scene = objs.ToDictionary((obj) => obj.id);

        for ( int y = 0; y < height; ++y) {
            rayCount += TraceRowJob(y, width, height, backBuffer, ref camera); 
        }
    }
}
