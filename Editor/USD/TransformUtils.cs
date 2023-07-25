using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Harpoon.USD
{
    public static class TransformUtils
    {
        public static bool IsOddNegativeScale(this Transform xform)
        {
            return IsOddNegativeScale(xform.localToWorldMatrix);
        }

        public static bool IsOddNegativeScale(this Matrix4x4 xform)
        {
            Vector3 x = xform.MultiplyVector(Vector3.right);
            Vector3 y = xform.MultiplyVector(Vector3.up);
            Vector3 z = xform.MultiplyVector(Vector3.forward);
            Vector3 w = Vector3.Cross(x, y);
            return Vector3.Dot(z, w) < 0; 
        }
    }
}