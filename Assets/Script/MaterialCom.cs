using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialCom : MonoBehaviour {
    public MaterialWrap wrap;

    void Start() {
        var render = GetComponent<Renderer>();
        if (render) {
            render.material.color = wrap.albedo;
        }
    }
}
