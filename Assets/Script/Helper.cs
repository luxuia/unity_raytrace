using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Helper {

    public static bool Refract(Vector3 v, Vector3 n, float nint, out Vector3 outRefracted) {
        float dt = Vector3.Dot(v, n);
        float discr = 1.0f - nint * nint * (1 - dt * dt);
        if (discr > 0) {
            outRefracted = nint * (v - n * dt) - n * Mathf.Sqrt(discr);
            return true;
        }
        outRefracted = Vector3.zero;
        return false;
    }
    
    public static float Schlick(float cosine, float ri) {
        float r0 = (1 - ri) / (1 + ri);
        r0 = r0 * r0;
        return r0 + (1 - r0) * Mathf.Pow(1 - cosine, 5);
    }

}

public struct TraceObj {
    public Collider collider;
    public Vector3 pos;
    public MaterialWrap mat;
    public int id;
}

public struct CameraWrap {
    public Vector3 pos;
    public Vector3 forward, up, right;

    public Vector3 LowLeftCorner;
    Vector3 horizontal, vertical;

    float lens_radius;

    public Ray GetRay(float s, float t) {
        Vector2 rd = lens_radius*Random.insideUnitCircle;
        Vector3 offset = up * rd.y + right * rd.x;
        return new Ray(pos + offset, 
            (LowLeftCorner + s * horizontal + t * vertical - pos - offset).normalized);
    }

    public CameraWrap(Camera camera, float lens_radius) {
        this.lens_radius = lens_radius;
        var trans = camera.transform;
        pos = trans.position;
        forward = trans.forward;
        up = trans.up;
        right = trans.right;

        float fov = camera.fieldOfView;
        float aspect = camera.aspect;
        float dis = camera.nearClipPlane;

        float halfHeight = Mathf.Tan(fov * Mathf.Deg2Rad / 2) * dis;
        float halfWidth = aspect * halfHeight;

        LowLeftCorner = pos - halfWidth * right - halfHeight * up + dis * forward;
        horizontal = 2 * halfWidth * right;
        vertical = 2 * halfHeight * up;
    }
}

[System.Serializable]
public struct MaterialWrap {

    public enum Type { Lambert, Metal, Dielectric };
    public Type type;
    public Color albedo;
    public Color emissive;
    public float roughness;
    public float ri; // air = 1, grass=1.3~1.7, diamond=2.4

    public bool HasEmission => emissive.r > 0 || emissive.r > 0 || emissive.b > 0;
}