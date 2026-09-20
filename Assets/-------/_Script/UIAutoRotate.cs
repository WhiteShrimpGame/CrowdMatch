using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIAutoRotate : MonoBehaviour
{
[Tooltip("旋转速度（度/秒），正值顺时针，负值逆时针")]
    public float speed = 100f;

    void Update()
    {
        // 绕 Z 轴自转
        transform.Rotate(0f, 0f, speed * Time.deltaTime);
    }
}
